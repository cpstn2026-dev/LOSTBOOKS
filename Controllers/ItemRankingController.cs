using LOSTBOOKS.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LOSTBOOKS.Controllers
{
    public class ItemRankingController : Controller
    {
        private readonly LOSTBOOKSContext _context;

        public ItemRankingController(LOSTBOOKSContext context)
        {
            _context = context;
        }


        // =====================================================
        // ITEM RANKING
        // =====================================================

        public IActionResult Index(
            string? range,
            DateTime? fromDate,
            DateTime? toDate,
            string? category)
        {
            var sales = _context.Histories
                .AsNoTracking()
                .AsQueryable();


            // =====================================================
            // DATE FILTER
            // =====================================================

            if (range == "daily" && fromDate.HasValue)
            {
                DateTime start = fromDate.Value.Date;
                DateTime end = start.AddDays(1);

                sales = sales.Where(x =>
                    x.TransactionDate >= start &&
                    x.TransactionDate < end);
            }

            else if (range == "weekly" && fromDate.HasValue)
            {
                DateTime start = fromDate.Value.Date;
                DateTime end = start.AddDays(7);

                sales = sales.Where(x =>
                    x.TransactionDate >= start &&
                    x.TransactionDate < end);
            }

            else if (range == "monthly" && fromDate.HasValue)
            {
                DateTime start = new DateTime(
                    fromDate.Value.Year,
                    fromDate.Value.Month,
                    1);

                DateTime end = start.AddMonths(1);

                sales = sales.Where(x =>
                    x.TransactionDate >= start &&
                    x.TransactionDate < end);
            }

            else if (
                range == "custom" &&
                fromDate.HasValue &&
                toDate.HasValue)
            {
                DateTime start = fromDate.Value.Date;
                DateTime end = toDate.Value.Date.AddDays(1);

                sales = sales.Where(x =>
                    x.TransactionDate >= start &&
                    x.TransactionDate < end);
            }


            // =====================================================
            // CATEGORY FILTER
            // =====================================================

            if (!string.IsNullOrEmpty(category) &&
                category != "All")
            {
                sales = sales.Where(x =>
                    x.Category == category);
            }


            // =====================================================
            // RANK ITEMS
            // =====================================================

            var rankings = sales
                .GroupBy(x => new
                {
                    x.ItemID,
                    x.ItemName,
                    x.Category
                })
                .Select(x => new
                {
                    ItemID = x.Key.ItemID,
                    ItemName = x.Key.ItemName,
                    Category = x.Key.Category,


                    QuantitySold = x.Sum(y =>
                        y.QuantitySold),
                    TotalSales = x.Sum(y =>
                        y.SellingPrice * y.QuantitySold)
                })
                .OrderByDescending(x => x.QuantitySold)
                .ToList();


            // =====================================================
            // SEND TO VIEW
            // =====================================================

            ViewBag.Rankings = rankings;

            ViewBag.TotalItems = rankings.Count;

            ViewBag.TotalQuantity = rankings.Sum(x =>
                x.QuantitySold);

            ViewBag.TotalSales = rankings.Sum(x =>
                x.TotalSales);
            return View();
        }


        // =====================================================
        // VIEW PDF
        // =====================================================

        public IActionResult ViewPdf(
            string? range,
            DateTime? fromDate,
            DateTime? toDate,
            string? category)
        {
            var sales = _context.Histories
                .AsNoTracking()
                .AsQueryable();


            // =====================================================
            // DATE FILTER
            // =====================================================

            if (range == "daily" && fromDate.HasValue)
            {
                DateTime start = fromDate.Value.Date;
                DateTime end = start.AddDays(1);

                sales = sales.Where(x =>
                    x.TransactionDate >= start &&
                    x.TransactionDate < end);
            }

            else if (range == "weekly" && fromDate.HasValue)
            {
                DateTime start = fromDate.Value.Date;
                DateTime end = start.AddDays(7);

                sales = sales.Where(x =>
                    x.TransactionDate >= start &&
                    x.TransactionDate < end);
            }

            else if (range == "monthly" && fromDate.HasValue)
            {
                DateTime start = new DateTime(
                    fromDate.Value.Year,
                    fromDate.Value.Month,
                    1);

                DateTime end = start.AddMonths(1);

                sales = sales.Where(x =>
                    x.TransactionDate >= start &&
                    x.TransactionDate < end);
            }

            else if (
                range == "custom" &&
                fromDate.HasValue &&
                toDate.HasValue)
            {
                DateTime start = fromDate.Value.Date;
                DateTime end = toDate.Value.Date.AddDays(1);

                sales = sales.Where(x =>
                    x.TransactionDate >= start &&
                    x.TransactionDate < end);
            }


            // =====================================================
            // CATEGORY FILTER
            // =====================================================

            if (!string.IsNullOrEmpty(category) &&
                category != "All")
            {
                sales = sales.Where(x =>
                    x.Category == category);
            }


            // =====================================================
            // RANK ITEMS
            // =====================================================

            var rankings = sales
                .GroupBy(x => new
                {
                    x.ItemID,
                    x.ItemName,
                    x.Category
                })
                .Select(x => new
                {
                    ItemID = x.Key.ItemID,
                    ItemName = x.Key.ItemName,
                    Category = x.Key.Category,

                    QuantitySold = x.Sum(y =>
                        y.QuantitySold),
                      TotalSales = x.Sum(y =>
                        y.SellingPrice * y.QuantitySold)
                })
                .OrderByDescending(x => x.QuantitySold)
                .ToList();


            // =====================================================
            // NO DATA
            // =====================================================

            if (rankings.Count == 0)
            {
                return NotFound(
                    "No item ranking records found.");
            }


            // =====================================================
            // CATEGORY DISPLAY
            // =====================================================

            string categoryText =
                string.IsNullOrEmpty(category) ||
                category == "All"
                    ? "All Categories"
                    : category;


            // =====================================================
            // PERIOD DISPLAY
            // =====================================================

            string periodText =
                GetPeriodText(
                    range,
                    fromDate,
                    toDate);


            // =====================================================
            // SUMMARY
            // =====================================================

            int totalItems =
                rankings.Count;

            int totalQuantity =
                rankings.Sum(x =>
                    x.QuantitySold);

            decimal totalSales =
            rankings.Sum(x =>
                    x.TotalSales);


            // =====================================================
            // CREATE PDF
            // =====================================================

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // =================================================
                    // PAGE
                    // =================================================

                    page.Size(
                        PageSizes.A4.Landscape());

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
                                .Text("ITEM RANKING")
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


                            // =================================================
                            // REPORT PERIOD
                            // =================================================

                            column.Item()
                                .PaddingTop(12)
                                .Text(
                                    $"Report Period: {periodText}")
                                .FontSize(10);


                            // =================================================
                            // CATEGORY
                            // =================================================

                            column.Item()
                                .Text(
                                    $"Category: {categoryText}")
                                .FontSize(10);


                            // =================================================
                            // SUMMARY
                            // =================================================

                            column.Item()
                                .PaddingTop(10)
                                .Text("SUMMARY")
                                .Bold()
                                .FontSize(13);


                            column.Item()
                                .LineHorizontal(1);


                            // =================================================
                            // SUMMARY DETAILS
                            // =================================================

                            column.Item()
                                .PaddingTop(4)
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(4);
                                    });


                                    // TOTAL ITEMS

                                    table.Cell()
                                        .Element(SummaryLabelStyle)
                                        .Text("Total Items:");


                                    table.Cell()
                                        .Element(SummaryValueStyle)
                                        .Text(
                                            totalItems.ToString());


                                    // TOTAL QUANTITY SOLD

                                    table.Cell()
                                        .Element(SummaryLabelStyle)
                                        .Text(
                                            "Total Quantity Sold:");


                                    table.Cell()
                                        .Element(SummaryValueStyle)
                                        .Text(
                                            totalQuantity.ToString());

                                    table.Cell()
                                        .Element(SummaryLabelStyle)
                                        .Text(
                                            "Total Sales");

                                    table.Cell()
                                        .Element(SummaryValueStyle)
                                        .Text(
                                            $"₱{totalSales:N2}");

                                });


                            // =================================================
                            // ITEM RANKING
                            // =================================================

                            column.Item()
                                .PaddingTop(12)
                                .Text("ITEM RANKING")
                                .Bold()
                                .FontSize(13);


                            column.Item()
                                .LineHorizontal(1);


                            // =================================================
                            // RANKING TABLE
                            // =================================================

                            column.Item()
                                .PaddingTop(4)
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        // Rank
                                        columns.RelativeColumn(0.8f);

                                        // Item ID
                                        columns.RelativeColumn(1.5f);

                                        // Item Name
                                        columns.RelativeColumn(3f);

                                        // Category
                                        columns.RelativeColumn(2f);

                                        // Quantity Sold
                                        columns.RelativeColumn(1.5f);

                                        //Total Sales
                                        columns.RelativeColumn(1.7f);
                                    });


                                    // =================================================
                                    // TABLE HEADER
                                    // =================================================

                                    table.Header(header =>
                                    {
                                        header.Cell()
                                            .Element(HeaderStyle)
                                            .AlignCenter()
                                            .Text("Rank");


                                        header.Cell()
                                            .Element(HeaderStyle)
                                            .Text("Item ID");


                                        header.Cell()
                                            .Element(HeaderStyle)
                                            .Text("Item Name");


                                        header.Cell()
                                            .Element(HeaderStyle)
                                            .Text("Category");


                                        header.Cell()
                                            .Element(HeaderStyle)
                                            .AlignRight()
                                            .Text("Quantity Sold");

                                        header.Cell()
                                            .Element(HeaderStyle)
                                            .AlignRight()
                                            .Text("Total Sales");
                                    });


                                    // =================================================
                                    // TABLE DATA
                                    // =================================================

                                    int rank = 1;


                                    foreach (var item in rankings)
                                    {
                                        // RANK

                                        table.Cell()
                                            .Element(CellStyle)
                                            .AlignCenter()
                                            .Text(
                                                $"#{rank}");


                                        // ITEM ID

                                        table.Cell()
                                            .Element(CellStyle)
                                            .Text(
                                                item.ItemID);


                                        // ITEM NAME

                                        table.Cell()
                                            .Element(CellStyle)
                                            .Text(
                                                item.ItemName);


                                        // CATEGORY

                                        table.Cell()
                                            .Element(CellStyle)
                                            .Text(
                                                item.Category);


                                        // QUANTITY SOLD

                                        table.Cell()
                                            .Element(CellStyle)
                                            .AlignRight()
                                            .Text(
                                                item.QuantitySold
                                                    .ToString());

                                        table.Cell()
                                            .Element(CellStyle)
                                            .AlignRight()
                                            .Text(
                                                $"₱{item.TotalSales:N2}");

                                        rank++;
                                    }
                                });


                            // =================================================
                            // TOTAL
                            // =================================================

                            column.Item()
                                .PaddingTop(12)
                                .AlignRight()
                                .Text(
                                    $"TOTAL QUANTITY SOLD: {totalQuantity}    |    TOTAL SALES: ₱{totalSales:N2}")
                                .Bold()
                                .FontSize(13);
                        });


                    // =================================================
                    // FOOTER
                    // =================================================

                    page.Footer()
                        .AlignRight()
                        .Text(
                            $"Generated on: {DateTime.Now:MMMM dd, yyyy h:mm tt}")
                        .FontSize(8);
                });
            });


            // =====================================================
            // GENERATE PDF
            // =====================================================

            byte[] pdf =
                document.GeneratePdf();


            // =====================================================
            // OPEN PDF IN BROWSER
            // =====================================================

            return File(
                pdf,
                "application/pdf");
        }


        // =====================================================
        // PERIOD TEXT
        // =====================================================

        private static string GetPeriodText(
            string? range,
            DateTime? fromDate,
            DateTime? toDate)
        {
            // DAILY

            if (
                range == "daily" &&
                fromDate.HasValue)
            {
                return fromDate.Value
                    .ToString("MMMM dd, yyyy");
            }


            // WEEKLY

            if (
                range == "weekly" &&
                fromDate.HasValue)
            {
                DateTime end =
                    fromDate.Value.Date.AddDays(6);

                return
                    $"{fromDate.Value:MMMM dd}–{end:MMMM dd, yyyy}";
            }


            // MONTHLY

            if (
                range == "monthly" &&
                fromDate.HasValue)
            {
                return fromDate.Value
                    .ToString("MMMM yyyy");
            }


            // CUSTOM

            if (
                range == "custom" &&
                fromDate.HasValue &&
                toDate.HasValue)
            {
                return
                    $"{fromDate.Value:MMMM dd}–{toDate.Value:MMMM dd, yyyy}";
            }


            // ALL DATES

            return "All Dates";
        }


        // =====================================================
        // PDF HEADER STYLE
        // =====================================================

        static IContainer HeaderStyle(
            IContainer container)
        {
            return container
                .Background(
                    Colors.Green.Darken2)
                .PaddingVertical(5)
                .PaddingHorizontal(4)
                .DefaultTextStyle(x =>
                    x.FontColor(Colors.White)
                     .Bold()
                     .FontSize(8));
        }


        // =====================================================
        // PDF CELL STYLE
        // =====================================================

        static IContainer CellStyle(
            IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(
                    Colors.Grey.Lighten2)
                .PaddingVertical(4)
                .PaddingHorizontal(3)
                .DefaultTextStyle(x =>
                    x.FontSize(8));
        }


        // =====================================================
        // SUMMARY LABEL STYLE
        // =====================================================

        static IContainer SummaryLabelStyle(
            IContainer container)
        {
            return container
                .PaddingVertical(3)
                .PaddingHorizontal(2)
                .DefaultTextStyle(x =>
                    x.FontSize(9)
                     .Bold());
        }


        // =====================================================
        // SUMMARY VALUE STYLE
        // =====================================================

        static IContainer SummaryValueStyle(
            IContainer container)
        {
            return container
                .PaddingVertical(3)
                .PaddingHorizontal(2)
                .DefaultTextStyle(x =>
                    x.FontSize(9));
        }
    }
}