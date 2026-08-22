using LOSTBOOKS.Data;
using LOSTBOOKS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LOSTBOOKS.Controllers
{
    public class ConsignorSummaryController : Controller
    {
        private readonly LOSTBOOKSContext _context;

        public ConsignorSummaryController(LOSTBOOKSContext context)
        {
            _context = context;
        }


        // =====================================================
        // MAIN LIST - ALL CONSIGNORS
        // =====================================================

        public IActionResult Index()
        {
            var consignors = _context.Consignors
                .AsNoTracking()
                .OrderBy(c => c.ConsignorName)
                .ToList();

            var sales = _context.Histories
                .AsNoTracking()
                .Where(x => x.ConsignorID != null)
                .ToList();

            var rows = consignors
                .Select(c =>
                {
                    var consignorSales = sales
                        .Where(x => x.ConsignorID == c.ConsignorID)
                        .ToList();

                    decimal totalSales = consignorSales
                        .Sum(x => x.SellingPrice * x.QuantitySold);

                    decimal storeShare = consignorSales
                        .Sum(x =>
                            x.SellingPrice * x.QuantitySold *
                            ((x.StoreSharePercentage ?? 0) / 100m));

                    return new
                    {
                        c.ConsignorID,
                        c.ConsignorName,

                        ItemsSold = consignorSales
                            .Select(x => x.ItemID)
                            .Distinct()
                            .Count(),

                        QuantitySold = consignorSales
                            .Sum(x => x.QuantitySold),

                        TotalSales = totalSales,
                        StoreShare = storeShare,
                        ConsignorShare = totalSales - storeShare
                    };
                })
                .OrderByDescending(x => x.TotalSales)
                .ToList();

            ViewBag.Consignors = rows;

            return View();
        }


        // =====================================================
        // CONSIGNOR DETAIL REPORT
        // =====================================================

        public IActionResult Details(
            int id,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var consignor = _context.Consignors
                .AsNoTracking()
                .FirstOrDefault(c => c.ConsignorID == id);

            if (consignor == null)
            {
                return NotFound();
            }

            var (periodStart, periodEnd) =
                GetPeriodBounds(fromDate, toDate);

            var sales =
                GetConsignorSales(id, periodStart, periodEnd);

            var items = BuildItemBreakdown(sales);

            ViewBag.Consignor = consignor;
            ViewBag.Items = items;

            ViewBag.TotalItemsSold = items.Count;
            ViewBag.TotalQuantitySold = items.Sum(x => x.QuantitySold);
            ViewBag.TotalSales = items.Sum(x => x.TotalSales);
            ViewBag.TotalStoreShare = items.Sum(x => x.StoreShare);
            ViewBag.TotalConsignorShare = items.Sum(x => x.ConsignorShare);

            ViewBag.PeriodText =
                GetPeriodText(fromDate, toDate);


            // =====================================================
            // OPTIONAL ANALYSIS
            // =====================================================
            // A simple, factual period-over-period comparison for
            // this consignor - only shown when a specific date
            // range is selected (so a "previous period" of the
            // same length exists to compare against). No invented
            // reasons, no forecasting.
            // =====================================================

            bool hasPreviousPeriod =
                periodStart.HasValue && periodEnd.HasValue;

            ViewBag.HasPreviousPeriod = hasPreviousPeriod;

            if (hasPreviousPeriod)
            {
                TimeSpan duration =
                    periodEnd!.Value - periodStart!.Value;

                var previousSales = GetConsignorSales(
                    id,
                    periodStart.Value - duration,
                    periodStart.Value);

                var previousItems = BuildItemBreakdown(previousSales);

                decimal currentTotal = items.Sum(x => x.TotalSales);
                decimal previousTotal = previousItems.Sum(x => x.TotalSales);
                decimal changeAmount = currentTotal - previousTotal;

                decimal? changePercent =
                    previousTotal != 0
                        ? Math.Round((changeAmount / previousTotal) * 100, 2)
                        : (decimal?)null;

                ViewBag.PreviousTotalSales = previousTotal;
                ViewBag.ChangeAmount = changeAmount;
                ViewBag.ChangePercent = changePercent;

                ViewBag.HighestSellingItem = items
                    .OrderByDescending(x => x.QuantitySold)
                    .FirstOrDefault();

                var itemChanges = items
                    .Select(cur =>
                    {
                        var prev = previousItems
                            .FirstOrDefault(p => p.ItemID == cur.ItemID);

                        int prevQty = prev?.QuantitySold ?? 0;

                        decimal? changePct =
                            prevQty != 0
                                ? Math.Round(
                                    ((decimal)(cur.QuantitySold - prevQty) / prevQty) * 100,
                                    2)
                                : (decimal?)null;

                        return new
                        {
                            cur.ItemID,
                            cur.ItemName,
                            ChangePercent = changePct
                        };
                    })
                    .Where(x => x.ChangePercent.HasValue)
                    .ToList();

                ViewBag.LargestIncreaseItem = itemChanges
                    .Where(x => x.ChangePercent > 0)
                    .OrderByDescending(x => x.ChangePercent)
                    .FirstOrDefault();

                ViewBag.LargestDecreaseItem = itemChanges
                    .Where(x => x.ChangePercent < 0)
                    .OrderBy(x => x.ChangePercent)
                    .FirstOrDefault();
            }

            return View();
        }


        // =====================================================
        // DOWNLOADABLE CONSIGNOR REPORT (PDF)
        // =====================================================

        public IActionResult ViewPdf(
            int id,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var consignor = _context.Consignors
                .AsNoTracking()
                .FirstOrDefault(c => c.ConsignorID == id);

            if (consignor == null)
            {
                return NotFound();
            }

            var (periodStart, periodEnd) =
                GetPeriodBounds(fromDate, toDate);

            var sales =
                GetConsignorSales(id, periodStart, periodEnd);

            var items = BuildItemBreakdown(sales);

            int totalQuantitySold = items.Sum(x => x.QuantitySold);
            decimal totalSales = items.Sum(x => x.TotalSales);
            decimal totalStoreShare = items.Sum(x => x.StoreShare);
            decimal totalConsignorShare = items.Sum(x => x.ConsignorShare);

            string periodText =
                GetPeriodText(fromDate, toDate);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(35);


                    // =================================================
                    // HEADER
                    // =================================================

                    page.Header()
                        .Column(column =>
                        {
                            column.Item()
                                .AlignCenter()
                                .Text("LOST BOOKS CEBU")
                                .Bold()
                                .FontSize(20);

                            column.Item()
                                .PaddingTop(3)
                                .AlignCenter()
                                .Text("CONSIGNOR SALES REPORT")
                                .Bold()
                                .FontSize(15);
                        });


                    // =================================================
                    // CONTENT
                    // =================================================

                    page.Content()
                        .Column(column =>
                        {
                            column.Spacing(6);


                            // CONSIGNOR INFORMATION

                            column.Item()
                                .PaddingTop(12)
                                .Text("CONSIGNOR INFORMATION")
                                .Bold()
                                .FontSize(13);

                            column.Item().LineHorizontal(1);

                            column.Item()
                                .PaddingTop(4)
                                .Text($"Consignor ID: CON-{consignor.ConsignorID:D4}")
                                .FontSize(10);

                            column.Item()
                                .Text($"Consignor Name: {consignor.ConsignorName}")
                                .FontSize(10);

                            column.Item()
                                .Text($"Reporting Period: {periodText}")
                                .FontSize(10);


                            // SALES BREAKDOWN

                            column.Item()
                                .PaddingTop(14)
                                .Text("SALES BREAKDOWN")
                                .Bold()
                                .FontSize(13);

                            column.Item().LineHorizontal(1);

                            column.Item()
                                .PaddingTop(4)
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1.3f);
                                        columns.RelativeColumn(2.6f);
                                        columns.RelativeColumn(1.6f);
                                        columns.RelativeColumn(1.1f);
                                        columns.RelativeColumn(1.5f);
                                        columns.RelativeColumn(1.7f);
                                        columns.RelativeColumn(1.7f);
                                        columns.RelativeColumn(1.7f);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(HeaderStyle).Text("Item ID");
                                        header.Cell().Element(HeaderStyle).Text("Item Name");
                                        header.Cell().Element(HeaderStyle).Text("Category");
                                        header.Cell().Element(HeaderStyle).AlignRight().Text("Qty");
                                        header.Cell().Element(HeaderStyle).AlignRight().Text("Price");
                                        header.Cell().Element(HeaderStyle).AlignRight().Text("Total Sales");
                                        header.Cell().Element(HeaderStyle).AlignRight().Text("Store Share");
                                        header.Cell().Element(HeaderStyle).AlignRight().Text("Consignor Share");
                                    });

                                    foreach (var item in items)
                                    {
                                        table.Cell().Element(CellStyle).Text(item.ItemID);
                                        table.Cell().Element(CellStyle).Text(item.ItemName);
                                        table.Cell().Element(CellStyle).Text(item.Category);

                                        table.Cell().Element(CellStyle).AlignRight()
                                            .Text(item.QuantitySold.ToString());

                                        table.Cell().Element(CellStyle).AlignRight()
                                            .Text($"₱{item.UnitPrice:N2}");

                                        table.Cell().Element(CellStyle).AlignRight()
                                            .Text($"₱{item.TotalSales:N2}");

                                        table.Cell().Element(CellStyle).AlignRight()
                                            .Text($"₱{item.StoreShare:N2}");

                                        table.Cell().Element(CellStyle).AlignRight()
                                            .Text($"₱{item.ConsignorShare:N2}");
                                    }
                                });


                            // TOTALS

                            column.Item()
                                .PaddingTop(14)
                                .Text("TOTALS")
                                .Bold()
                                .FontSize(13);

                            column.Item().LineHorizontal(1);

                            column.Item()
                                .PaddingTop(4)
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(3);
                                    });

                                    table.Cell().Element(SummaryLabelStyle).Text("Total Quantity Sold:");
                                    table.Cell().Element(SummaryValueStyle).Text(totalQuantitySold.ToString());

                                    table.Cell().Element(SummaryLabelStyle).Text("Total Sales:");
                                    table.Cell().Element(SummaryValueStyle).Text($"₱{totalSales:N2}");

                                    table.Cell().Element(SummaryLabelStyle).Text("Total Store Share:");
                                    table.Cell().Element(SummaryValueStyle).Text($"₱{totalStoreShare:N2}");

                                    table.Cell().Element(SummaryLabelStyle).Text("Total Consignor Share:");
                                    table.Cell().Element(SummaryValueStyle).Text($"₱{totalConsignorShare:N2}");
                                });
                        });


                    // =================================================
                    // FOOTER
                    // =================================================

                    page.Footer()
                        .AlignRight()
                        .Text($"Generated on: {DateTime.Now:MMMM dd, yyyy h:mm tt}")
                        .FontSize(8);
                });
            });

            byte[] pdf = document.GeneratePdf();

            return File(
                pdf,
                "application/pdf",
                $"ConsignorReport_{consignor.ConsignorName}_{DateTime.Now:yyyyMMdd}.pdf");
        }


        // =====================================================
        // HELPERS
        // =====================================================

        private List<History> GetConsignorSales(
            int consignorId,
            DateTime? start,
            DateTime? end)
        {
            var query = _context.Histories
                .AsNoTracking()
                .Where(x => x.ConsignorID == consignorId);

            if (start.HasValue && end.HasValue)
            {
                query = query.Where(x =>
                    x.TransactionDate >= start.Value &&
                    x.TransactionDate < end.Value);
            }

            return query
                .OrderByDescending(x => x.TransactionDate)
                .ToList();
        }

        private static List<ConsignorItemRow> BuildItemBreakdown(
            List<History> sales)
        {
            return sales
                .GroupBy(x => new { x.ItemID, x.ItemName, x.Category })
                .Select(g =>
                {
                    int quantity = g.Sum(x => x.QuantitySold);

                    decimal totalSales =
                        g.Sum(x => x.SellingPrice * x.QuantitySold);

                    decimal storeShare =
                        g.Sum(x =>
                            x.SellingPrice * x.QuantitySold *
                            ((x.StoreSharePercentage ?? 0) / 100m));

                    return new ConsignorItemRow
                    {
                        ItemID = g.Key.ItemID,
                        ItemName = g.Key.ItemName,
                        Category = g.Key.Category,
                        QuantitySold = quantity,
                        UnitPrice = quantity != 0 ? totalSales / quantity : 0,
                        TotalSales = totalSales,
                        StoreShare = storeShare,
                        ConsignorShare = totalSales - storeShare
                    };
                })
                .OrderByDescending(x => x.TotalSales)
                .ToList();
        }

        private static (DateTime? Start, DateTime? End) GetPeriodBounds(
            DateTime? fromDate,
            DateTime? toDate)
        {
            if (fromDate.HasValue && toDate.HasValue)
            {
                DateTime start = fromDate.Value.Date;
                DateTime end = toDate.Value.Date.AddDays(1);
                return (start, end);
            }

            return (null, null);
        }

        private static string GetPeriodText(
            DateTime? fromDate,
            DateTime? toDate)
        {
            if (fromDate.HasValue && toDate.HasValue)
            {
                return $"{fromDate.Value:MMMM dd}–{toDate.Value:MMMM dd, yyyy}";
            }

            return "All Dates";
        }


        // =====================================================
        // PDF STYLES (same look as the other reports)
        // =====================================================

        static IContainer HeaderStyle(IContainer container)
        {
            return container
                .Background(Colors.Green.Darken2)
                .PaddingVertical(5)
                .PaddingHorizontal(4)
                .DefaultTextStyle(x =>
                    x.FontColor(Colors.White)
                     .Bold()
                     .FontSize(8));
        }

        static IContainer CellStyle(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(4)
                .PaddingHorizontal(3)
                .DefaultTextStyle(x => x.FontSize(8));
        }

        static IContainer SummaryLabelStyle(IContainer container)
        {
            return container
                .PaddingVertical(3)
                .PaddingHorizontal(2)
                .DefaultTextStyle(x => x.FontSize(9).Bold());
        }

        static IContainer SummaryValueStyle(IContainer container)
        {
            return container
                .PaddingVertical(3)
                .PaddingHorizontal(2)
                .DefaultTextStyle(x => x.FontSize(9));
        }
    }


    // =====================================================
    // PER-ITEM AGGREGATED BREAKDOWN ROW FOR A CONSIGNOR
    // =====================================================

    public class ConsignorItemRow
    {
        public string ItemID { get; set; } = "";
        public string ItemName { get; set; } = "";
        public string Category { get; set; } = "";
        public int QuantitySold { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalSales { get; set; }
        public decimal StoreShare { get; set; }
        public decimal ConsignorShare { get; set; }
    }
}