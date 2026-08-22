using LOSTBOOKS.Data;
using LOSTBOOKS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LOSTBOOKS.Controllers
{
    public class HistoryController : Controller
    {
        private readonly LOSTBOOKSContext _context;

        public HistoryController(LOSTBOOKSContext context)
        {
            _context = context;
        }

        // =====================================================
        // HISTORY
        // =====================================================

        public IActionResult Index(
            string? dateRange,
            DateTime? fromDate,
            DateTime? toDate,
            string? category)
        {
            var history = GetFilteredHistory(
                dateRange,
                fromDate,
                toDate,
                category);

            decimal totalSales = history.Sum(x =>
                x.SellingPrice * x.QuantitySold);

            ViewBag.TotalSales = totalSales;

            return View(history);
        }


        // =====================================================
        // VIEW PDF
        // =====================================================

        public IActionResult ViewPdf(
            string? dateRange,
            DateTime? fromDate,
            DateTime? toDate,
            string? category)
        {
            var history = GetFilteredHistory(
                dateRange,
                fromDate,
                toDate,
                category);

            if (history.Count == 0)
            {
                return Content(
                    "No transaction records found for the selected filters.");
            }

            decimal totalSales = history.Sum(x =>
                x.SellingPrice * x.QuantitySold);

            int totalTransactions = history.Count;

            string categoryText =
                string.IsNullOrWhiteSpace(category) ||
                category == "All"
                    ? "All Categories"
                    : category;

            string periodText =
                GetPeriodText(
                    dateRange,
                    fromDate,
                    toDate);


            // =================================================
            // CREATE PDF
            // =================================================

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // =================================================
                    // PAGE
                    // =================================================

                    page.Size(PageSizes.A4.Landscape());

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
                                .Text("TRANSACTION HISTORY")
                                .Bold()
                                .FontSize(15);

                            column.Item()
                                .PaddingTop(6)
                                .AlignCenter()
                                .Text(
                                    $"Report Period: {periodText}")
                                .FontSize(9);

                            column.Item()
                                .AlignCenter()
                                .Text(
                                    $"Category: {categoryText}")
                                .FontSize(9);
                        });


                    // =================================================
                    // CONTENT
                    // =================================================

                    page.Content()
                        .Column(column =>
                        {
                            column.Spacing(6);


                            // =================================================
                            // SUMMARY
                            // =================================================

                            column.Item()
                                .PaddingTop(12)
                                .Text("SUMMARY")
                                .Bold()
                                .FontSize(13);


                            column.Item()
                                .LineHorizontal(1);


                            column.Item()
                                .PaddingTop(4)
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(4);
                                    });


                                    // TOTAL SALES

                                    table.Cell()
                                        .Element(SummaryLabelStyle)
                                        .Text("Total Sales:");

                                    table.Cell()
                                        .Element(SummaryValueStyle)
                                        .Text(
                                            $"₱{totalSales:N2}");


                                    // TOTAL TRANSACTIONS

                                    table.Cell()
                                        .Element(SummaryLabelStyle)
                                        .Text("Total Transactions:");

                                    table.Cell()
                                        .Element(SummaryValueStyle)
                                        .Text(
                                            totalTransactions.ToString());
                                });


                            // =================================================
                            // TRANSACTION RECORDS
                            // =================================================

                            column.Item()
                                .PaddingTop(12)
                                .Text("TRANSACTION RECORDS")
                                .Bold()
                                .FontSize(13);


                            column.Item()
                                .LineHorizontal(1);


                            // =================================================
                            // TABLE
                            // =================================================

                            column.Item()
                                .PaddingTop(4)
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        //Transaction ID
                                        columns.RelativeColumn(1.0f);
                                        // Date & Time
                                        columns.RelativeColumn(1.8f);

                                        // Item ID
                                        columns.RelativeColumn(1.0f);

                                        // Item Name
                                        columns.RelativeColumn(2.3f);

                                        // Category
                                        columns.RelativeColumn(1.25f);

                                        // Qty
                                        columns.RelativeColumn(0.65f);

                                        // Price
                                        columns.RelativeColumn(1.15f);

                                        // Total
                                        columns.RelativeColumn(1.15f);

                                        // Payment
                                        columns.RelativeColumn(0.95f);
                                    });


                                    // =================================================
                                    // TABLE HEADER
                                    // =================================================

                                    table.Header(header =>
                                    {
                                        header.Cell()
                                            .Element(HeaderStyle)
                                            .Text("Transaction ID");

                                        header.Cell()
                                            .Element(HeaderStyle)
                                            .Text("Date & Time");

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
                                            .AlignCenter()
                                            .Text("Qty");

                                        header.Cell()
                                            .Element(HeaderStyle)
                                            .AlignRight()
                                            .Text("Price");

                                        header.Cell()
                                            .Element(HeaderStyle)
                                            .AlignRight()
                                            .Text("Total");

                                        header.Cell()
                                            .Element(HeaderStyle)
                                            .Text("Payment");
                                    });


                                    // =================================================
                                    // TABLE DATA
                                    // =================================================

                                    foreach (var item in history)
                                    {
                                        decimal itemTotal =
                                            item.SellingPrice *
                                            item.QuantitySold;

                                      //TransactionID

                                        table.Cell()
                                            .Element(CellStyle)
                                            .Text(
                                                  item.TransactionID);
                                        

                                        // DATE & TIME

                                        table.Cell()
                                            .Element(CellStyle)
                                            .Text(
                                                item.TransactionDate.ToString(
                                                    "MMM dd, yyyy hh:mm tt"));


                                        // ITEM ID

                                        table.Cell()
                                            .Element(CellStyle)
                                            .Text(
                                                item.ItemID ?? "-");


                                        // ITEM NAME

                                        table.Cell()
                                            .Element(CellStyle)
                                            .Text(
                                                item.ItemName ?? "-");


                                        // CATEGORY

                                        table.Cell()
                                            .Element(CellStyle)
                                            .Text(
                                                item.Category ?? "-");


                                        // QUANTITY

                                        table.Cell()
                                            .Element(CellStyle)
                                            .AlignCenter()
                                            .Text(
                                                item.QuantitySold.ToString());


                                        // PRICE

                                        table.Cell()
                                            .Element(CellStyle)
                                            .AlignRight()
                                            .Text(
                                                $"₱{item.SellingPrice:N2}");


                                        // TOTAL

                                        table.Cell()
                                            .Element(CellStyle)
                                            .AlignRight()
                                            .Text(
                                                $"₱{itemTotal:N2}");


                                        // PAYMENT

                                        table.Cell()
                                            .Element(CellStyle)
                                            .Text(
                                                item.PaymentType ?? "-");
                                    }
                                });


                            // =================================================
                            // GRAND TOTAL
                            // =================================================

                            column.Item()
                                .PaddingTop(12)
                                .AlignRight()
                                .Text(
                                    $"GRAND TOTAL SALES: ₱{totalSales:N2}")
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


            // =================================================
            // GENERATE PDF
            // =================================================

            byte[] pdf = document.GeneratePdf();


            // =================================================
            // OPEN PDF IN BROWSER
            // =================================================

            return File(
                pdf,
                "application/pdf");
        }


        // =====================================================
        // DOWNLOAD PDF
        // =====================================================

        public IActionResult DownloadPdf(
            string? dateRange,
            DateTime? fromDate,
            DateTime? toDate,
            string? category)
        {
            var history = GetFilteredHistory(
                dateRange,
                fromDate,
                toDate,
                category);

            if (history.Count == 0)
            {
                return Content(
                    "No transaction records found for the selected filters.");
            }

            decimal totalSales = history.Sum(x =>
                x.SellingPrice * x.QuantitySold);

            string categoryText =
                string.IsNullOrWhiteSpace(category) ||
                category == "All"
                    ? "All Categories"
                    : category;

            string periodText =
                GetPeriodText(
                    dateRange,
                    fromDate,
                    toDate);


            // =================================================
            // CREATE PDF
            // =================================================

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());

                    page.Margin(35);


                    // HEADER

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
                                .Text("TRANSACTION HISTORY")
                                .Bold()
                                .FontSize(15);

                            column.Item()
                                .PaddingTop(6)
                                .AlignCenter()
                                .Text(
                                    $"Report Period: {periodText}")
                                .FontSize(9);

                            column.Item()
                                .AlignCenter()
                                .Text(
                                    $"Category: {categoryText}")
                                .FontSize(9);
                        });


                    // CONTENT

                    page.Content()
                        .Column(column =>
                        {
                            column.Spacing(6);


                            // SUMMARY

                            column.Item()
                                .PaddingTop(12)
                                .Text("SUMMARY")
                                .Bold()
                                .FontSize(13);

                            column.Item()
                                .LineHorizontal(1);

                            column.Item()
                                .PaddingTop(4)
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(4);
                                    });

                                    table.Cell()
                                        .Element(SummaryLabelStyle)
                                        .Text("Total Sales:");

                                    table.Cell()
                                        .Element(SummaryValueStyle)
                                        .Text(
                                            $"₱{totalSales:N2}");

                                    table.Cell()
                                        .Element(SummaryLabelStyle)
                                        .Text("Total Transactions:");

                                    table.Cell()
                                        .Element(SummaryValueStyle)
                                        .Text(
                                            history.Count.ToString());
                                });


                            // TRANSACTION RECORDS

                            column.Item()
                                .PaddingTop(12)
                                .Text("TRANSACTION RECORDS")
                                .Bold()
                                .FontSize(13);

                            column.Item()
                                .LineHorizontal(1);


                            // TABLE

                            column.Item()
                                .PaddingTop(4)
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1.0f);
                                        columns.RelativeColumn(1.7f);
                                        columns.RelativeColumn(1.0f);
                                        columns.RelativeColumn(2.3f);
                                        columns.RelativeColumn(1.25f);
                                        columns.RelativeColumn(0.65f);
                                        columns.RelativeColumn(1.15f);
                                        columns.RelativeColumn(1.15f);
                                        columns.RelativeColumn(0.95f);
                                    });


                                    table.Header(header =>
                                    {
                                        header.Cell()
                                            .Element(HeaderStyle)
                                            .Text("Transaction ID");

                                        header.Cell()
                                            .Element(HeaderStyle)
                                            .Text("Date & Time");

                                        header.Cell()
                                            .Element(HeaderStyle)
                                            .Text("Item ID");

                                        header.Cell()
                                            .Element(HeaderStyle)
                                            .Text("Category");

                                        header.Cell()
                                            .Element(HeaderStyle)
                                            .AlignCenter()
                                            .Text("Qty");

                                        header.Cell()
                                            .Element(HeaderStyle)
                                            .AlignRight()
                                            .Text("Price");

                                        header.Cell()
                                            .Element(HeaderStyle)
                                            .AlignRight()
                                            .Text("Total");

                                        header.Cell()
                                            .Element(HeaderStyle)
                                            .Text("Payment");
                                    });


                                    foreach (var item in history)
                                    {
                                        decimal itemTotal =
                                            item.SellingPrice *
                                            item.QuantitySold;

                                        table.Cell()
                                            .Element(CellStyle)
                                            .Text(
                                                 item.TransactionID);

                                        table.Cell()
                                            .Element(CellStyle)
                                            .Text(
                                                item.TransactionDate.ToString(
                                                    "MMM dd, yyyy hh:mm tt"));

                                        table.Cell()
                                            .Element(CellStyle)
                                            .Text(
                                                item.ItemID ?? "-");

                                        table.Cell()
                                            .Element(CellStyle)
                                            .Text(
                                                item.ItemName ?? "-");

                                        table.Cell()
                                            .Element(CellStyle)
                                            .Text(
                                                item.Category ?? "-");

                                        table.Cell()
                                            .Element(CellStyle)
                                            .AlignCenter()
                                            .Text(
                                                item.QuantitySold.ToString());

                                        table.Cell()
                                            .Element(CellStyle)
                                            .AlignRight()
                                            .Text(
                                                $"₱{item.SellingPrice:N2}");

                                        table.Cell()
                                            .Element(CellStyle)
                                            .AlignRight()
                                            .Text(
                                                $"₱{itemTotal:N2}");

                                        table.Cell()
                                            .Element(CellStyle)
                                            .Text(
                                                item.PaymentType ?? "-");
                                    }
                                });


                            // GRAND TOTAL

                            column.Item()
                                .PaddingTop(12)
                                .AlignRight()
                                .Text(
                                    $"GRAND TOTAL SALES: ₱{totalSales:N2}")
                                .Bold()
                                .FontSize(13);
                        });


                    // FOOTER

                    page.Footer()
                        .AlignRight()
                        .Text(
                            $"Generated on: {DateTime.Now:MMMM dd, yyyy h:mm tt}")
                        .FontSize(8);
                });
            });


            byte[] pdf = document.GeneratePdf();

            return File(
                pdf,
                "application/pdf",
                $"History_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }


        // =====================================================
        // FILTER HISTORY
        // =====================================================

        private List<History> GetFilteredHistory(
            string? dateRange,
            DateTime? fromDate,
            DateTime? toDate,
            string? category)
        {
            var query = _context.Histories
                .AsNoTracking()
                .AsQueryable();


            // =================================================
            // DAILY
            // =================================================

            if (dateRange == "Daily" &&
                fromDate.HasValue)
            {
                DateTime start =
                    fromDate.Value.Date;

                DateTime end =
                    start.AddDays(1);

                query = query.Where(x =>
                    x.TransactionDate >= start &&
                    x.TransactionDate < end);
            }


            // =================================================
            // WEEKLY
            // =================================================

            else if (dateRange == "Weekly" &&
                     fromDate.HasValue)
            {
                DateTime start =
                    fromDate.Value.Date;

                DateTime end =
                    start.AddDays(7);

                query = query.Where(x =>
                    x.TransactionDate >= start &&
                    x.TransactionDate < end);
            }


            // =================================================
            // MONTHLY
            // =================================================

            else if (dateRange == "Monthly" &&
                     fromDate.HasValue)
            {
                DateTime start =
                    new DateTime(
                        fromDate.Value.Year,
                        fromDate.Value.Month,
                        1);

                DateTime end =
                    start.AddMonths(1);

                query = query.Where(x =>
                    x.TransactionDate >= start &&
                    x.TransactionDate < end);
            }


            // =================================================
            // CUSTOM
            // =================================================

            else if (dateRange == "Custom" &&
                     fromDate.HasValue &&
                     toDate.HasValue)
            {
                DateTime start =
                    fromDate.Value.Date;

                DateTime end =
                    toDate.Value.Date.AddDays(1);

                query = query.Where(x =>
                    x.TransactionDate >= start &&
                    x.TransactionDate < end);
            }


            // =================================================
            // CATEGORY
            // =================================================

            if (!string.IsNullOrWhiteSpace(category) &&
                category != "All")
            {
                query = query.Where(x =>
                    x.Category == category);
            }


            // =================================================
            // RESULT
            // =================================================

            return query
                .OrderByDescending(x => x.TransactionDate)
                .ToList();
        }


        // =====================================================
        // PERIOD TEXT
        // =====================================================

        private static string GetPeriodText(
            string? dateRange,
            DateTime? fromDate,
            DateTime? toDate)
        {
            if (dateRange == "Daily" &&
                fromDate.HasValue)
            {
                return fromDate.Value
                    .ToString("MMMM dd, yyyy");
            }


            if (dateRange == "Weekly" &&
                fromDate.HasValue)
            {
                DateTime end =
                    fromDate.Value.Date.AddDays(6);

                return
                    $"{fromDate.Value:MMMM dd} - {end:MMMM dd, yyyy}";
            }


            if (dateRange == "Monthly" &&
                fromDate.HasValue)
            {
                return fromDate.Value
                    .ToString("MMMM yyyy");
            }


            if (dateRange == "Custom" &&
                fromDate.HasValue &&
                toDate.HasValue)
            {
                return
                    $"{fromDate.Value:MMMM dd} - {toDate.Value:MMMM dd, yyyy}";
            }


            return "All Dates";
        }


        // =====================================================
        // PDF HEADER STYLE
        // =====================================================

        private static IContainer HeaderStyle(
            IContainer container)
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


        // =====================================================
        // PDF CELL STYLE
        // =====================================================

        private static IContainer CellStyle(
            IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(4)
                .PaddingHorizontal(3)
                .DefaultTextStyle(x =>
                    x.FontSize(7));
        }


        // =====================================================
        // PDF SUMMARY LABEL
        // =====================================================

        private static IContainer SummaryLabelStyle(
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
        // PDF SUMMARY VALUE
        // =====================================================

        private static IContainer SummaryValueStyle(
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