
using LOSTBOOKS.Data;
using LOSTBOOKS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ScottPlot;
using System.Text.Json;

using QuestColors = QuestPDF.Helpers.Colors;

namespace LOSTBOOKS.Controllers
{
    public class SalesReportingController : Controller
    {
        private readonly LOSTBOOKSContext _context;

        public SalesReportingController(LOSTBOOKSContext context)
        {
            _context = context;
        }

        // =====================================================
        // INDEX
        // =====================================================

        public IActionResult Index(
            string? range,
            DateTime? fromDate,
            DateTime? toDate,
            string? category)
        {
            var sales = GetFilteredSales(range, fromDate, toDate, category);

            decimal totalSales = sales.Sum(x => x.SellingPrice * x.QuantitySold);
            int totalTransactions = sales.Count;

            var topItem = sales
                .GroupBy(x => x.ItemName)
                .Select(g => new
                {
                    ItemName = g.Key,
                    Quantity = g.Sum(x => x.QuantitySold)
                })
                .OrderByDescending(x => x.Quantity)
                .FirstOrDefault();

            var bestCategory = sales
                .GroupBy(x => x.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Total = g.Sum(x => x.SellingPrice * x.QuantitySold)
                })
                .OrderByDescending(x => x.Total)
                .FirstOrDefault();

            var salesTrend = sales
                .GroupBy(x => x.TransactionDate.Date)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Date = g.Key.ToString("MMM dd, yyyy"),
                    Total = g.Sum(x => x.SellingPrice * x.QuantitySold)
                })
                .ToList();

            var categoryPerformance = sales
                .GroupBy(x => x.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Total = g.Sum(x => x.SellingPrice * x.QuantitySold)
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            var topItems = sales
                .GroupBy(x => x.ItemName)
                .Select(g => new
                {
                    ItemName = g.Key,
                    Quantity = g.Sum(x => x.QuantitySold)
                })
                .OrderByDescending(x => x.Quantity)
                .Take(5)
                .ToList();

            ViewBag.TotalSales = totalSales;
            ViewBag.TotalTransactions = totalTransactions;
            ViewBag.TopItem = topItem?.ItemName ?? "-";
            ViewBag.BestCategory = bestCategory?.Category ?? "-";

            ViewBag.Range = range ?? "";
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.Category = string.IsNullOrWhiteSpace(category) ? "All" : category;

            ViewBag.CategoryText =
                string.IsNullOrWhiteSpace(category) ||
                category.Equals("All", StringComparison.OrdinalIgnoreCase)
                ? "All Categories"
                : category;

            ViewBag.PeriodText = GetPeriodText(range, fromDate, toDate);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null
            };

            ViewBag.SalesTrendJson =
                JsonSerializer.Serialize(salesTrend, options);

            ViewBag.CategoryPerformanceJson =
                JsonSerializer.Serialize(categoryPerformance, options);

            ViewBag.TopItemsJson =
                JsonSerializer.Serialize(topItems, options);

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
            var sales = GetFilteredSales(range, fromDate, toDate, category);

            if (sales.Count == 0)
                return Content("No sales records found.");

            decimal totalSales =
                sales.Sum(x => x.SellingPrice * x.QuantitySold);

            int totalTransactions = sales.Count;

            var topItem = sales
                .GroupBy(x => x.ItemName)
                .Select(g => new
                {
                    ItemName = g.Key,
                    Quantity = g.Sum(x => x.QuantitySold)
                })
                .OrderByDescending(x => x.Quantity)
                .FirstOrDefault();

            var bestCategory = sales
                .GroupBy(x => x.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Total = g.Sum(x => x.SellingPrice * x.QuantitySold)
                })
                .OrderByDescending(x => x.Total)
                .FirstOrDefault();

            var dailySummary = sales
                .GroupBy(x => x.TransactionDate.Date)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Date = g.Key,
                    Transactions = g.Count(),
                    Quantity = g.Sum(x => x.QuantitySold),
                    TotalSales = g.Sum(x => x.SellingPrice * x.QuantitySold)
                })
                .ToList();

            var categorySummary = sales
                .GroupBy(x => x.Category)
                .OrderByDescending(g =>
                    g.Sum(x => x.SellingPrice * x.QuantitySold))
                .Select(g => new
                {
                    Category = g.Key,
                    Transactions = g.Count(),
                    Quantity = g.Sum(x => x.QuantitySold),
                    TotalSales = g.Sum(x => x.SellingPrice * x.QuantitySold)
                })
                .ToList();

            string categoryText =
                string.IsNullOrWhiteSpace(category) ||
                category.Equals("All", StringComparison.OrdinalIgnoreCase)
                ? "All Categories"
                : category;

            string periodText =
                GetPeriodText(range, fromDate, toDate);

            string tempFolder =
                Path.Combine(Path.GetTempPath(), "LostBooksCharts");

            Directory.CreateDirectory(tempFolder);

            string lineChartPath =
                Path.Combine(tempFolder, $"line-{Guid.NewGuid()}.png");

            string pieChartPath =
                Path.Combine(tempFolder, $"pie-{Guid.NewGuid()}.png");

            string barChartPath =
                Path.Combine(tempFolder, $"bar-{Guid.NewGuid()}.png");

            try
            {
                CreateSalesTrendChart(sales, lineChartPath);
                CreateCategoryChart(sales, pieChartPath);
                CreateTopItemsChart(sales, barChartPath);

                byte[] lineBytes =
                    System.IO.File.ReadAllBytes(lineChartPath);

                byte[] pieBytes =
                    System.IO.File.ReadAllBytes(pieChartPath);

                byte[] barBytes =
                    System.IO.File.ReadAllBytes(barChartPath);

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(30);

                        page.Header().Column(col =>
                        {
                            col.Item().AlignCenter()
                                .Text("LOST BOOKS CEBU")
                                .Bold().FontSize(20);

                            col.Item().AlignCenter()
                                .Text("SALES REPORTING & ANALYSIS")
                                .Bold().FontSize(15);

                            col.Item().PaddingTop(5)
                                .AlignCenter()
                                .Text($"Report Period: {periodText}")
                                .FontSize(9);

                            col.Item().AlignCenter()
                                .Text($"Category: {categoryText}")
                                .FontSize(9);
                        });

                        page.Content().PaddingTop(15).Column(col =>
                        {
                            col.Spacing(8);

                            // SUMMARY

                            col.Item().Text("SUMMARY")
                                .Bold().FontSize(13);

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(3);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(3);
                                });

                                table.Cell().Element(LabelStyle).Text("Total Sales");
                                table.Cell().Element(ValueStyle).Text($"₱{totalSales:N2}");

                                table.Cell().Element(LabelStyle).Text("Transactions");
                                table.Cell().Element(ValueStyle).Text(totalTransactions);

                                table.Cell().Element(LabelStyle).Text("Top Selling Item");
                                table.Cell().Element(ValueStyle).Text(topItem?.ItemName ?? "-");

                                table.Cell().Element(LabelStyle).Text("Best Category");
                                table.Cell().Element(ValueStyle).Text(bestCategory?.Category ?? "-");
                            });

                            // DAILY SUMMARY

                            col.Item().PaddingTop(10)
                                .Text("DAILY SALES SUMMARY")
                                .Bold().FontSize(13);

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(2);
                                });

                                table.Header(h =>
                                {
                                    h.Cell().Element(HeaderStyle).Text("Date");
                                    h.Cell().Element(HeaderStyle).Text("Transactions");
                                    h.Cell().Element(HeaderStyle).Text("Quantity");
                                    h.Cell().Element(HeaderStyle).Text("Total Sales");
                                });

                                foreach (var item in dailySummary)
                                {
                                    table.Cell().Element(CellStyle)
                                        .Text(item.Date.ToString("MMM dd, yyyy"));

                                    table.Cell().Element(CellStyle)
                                        .Text(item.Transactions);

                                    table.Cell().Element(CellStyle)
                                        .Text(item.Quantity);

                                    table.Cell().Element(CellStyle)
                                        .Text($"₱{item.TotalSales:N2}");
                                }
                            });

                            // CATEGORY SUMMARY

                            col.Item().PaddingTop(10)
                                .Text("CATEGORY SUMMARY")
                                .Bold().FontSize(13);

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(3);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(2);
                                });

                                table.Header(h =>
                                {
                                    h.Cell().Element(HeaderStyle).Text("Category");
                                    h.Cell().Element(HeaderStyle).Text("Transactions");
                                    h.Cell().Element(HeaderStyle).Text("Quantity");
                                    h.Cell().Element(HeaderStyle).Text("Total Sales");
                                });

                                foreach (var item in categorySummary)
                                {
                                    table.Cell().Element(CellStyle).Text(item.Category);
                                    table.Cell().Element(CellStyle).Text(item.Transactions);
                                    table.Cell().Element(CellStyle).Text(item.Quantity);
                                    table.Cell().Element(CellStyle).Text($"₱{item.TotalSales:N2}");
                                }
                            });

                            // ANALYSIS

                            col.Item().PaddingTop(15)
                                .Text("ANALYSIS VISUALS")
                                .Bold().FontSize(14);

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().PaddingRight(8).Column(chart =>
                                {
                                    chart.Item().Text("SALES TREND")
                                        .Bold().FontSize(11);

                                    chart.Item().Height(220)
                                        .Image(lineBytes)
                                        .FitArea();
                                });

                                row.RelativeItem().PaddingLeft(8).Column(chart =>
                                {
                                    chart.Item().Text("CATEGORY PERFORMANCE")
                                        .Bold().FontSize(11);

                                    chart.Item().Height(220)
                                        .Image(pieBytes)
                                        .FitArea();
                                });
                            });

                            // BIG BAR GRAPH

                            col.Item().PaddingTop(15)
                                .Text("TOP SELLING ITEMS")
                                .Bold().FontSize(12);

                            col.Item()
                                .Height(330)
                                .Image(barBytes)
                                .FitArea();

                            // GRAND TOTAL

                            col.Item().PaddingTop(10)
                                .AlignRight()
                                .Text($"GRAND TOTAL SALES: ₱{totalSales:N2}")
                                .Bold().FontSize(14);
                        });

                        page.Footer().AlignCenter()
                            .Text($"Generated on: {DateTime.Now:MMMM dd, yyyy h:mm tt}")
                            .FontSize(8);
                    });
                });

                byte[] pdf = document.GeneratePdf();

                return File(pdf, "application/pdf");
            }
            finally
            {
                DeleteFile(lineChartPath);
                DeleteFile(pieChartPath);
                DeleteFile(barChartPath);
            }
        }

        // =====================================================
        // SALES TREND
        // =====================================================

        private static void CreateSalesTrendChart(
            List<History> sales,
            string path)
        {
            var grouped = sales
                .GroupBy(x => x.TransactionDate.Date)
                .OrderBy(x => x.Key)
                .ToList();

            double[] xs =
                Enumerable.Range(0, grouped.Count)
                .Select(x => (double)x)
                .ToArray();

            double[] ys =
                grouped.Select(g =>
                    (double)g.Sum(x =>
                        x.SellingPrice * x.QuantitySold))
                .ToArray();

            var plot = new Plot();

            if (ys.Length > 0)
            {
                var scatter = plot.Add.Scatter(xs, ys);
                scatter.LineWidth = 3;
                scatter.MarkerSize = 7;
            }

            plot.Title("Sales Trend");
            plot.XLabel("Date");
            plot.YLabel("Sales");

            plot.SavePng(path, 1000, 500);
        }

        // =====================================================
        // PIE
        // =====================================================

        private static void CreateCategoryChart(
            List<History> sales,
            string path)
        {
            var grouped = sales
                .GroupBy(x => x.Category)
                .Select(g => new
                {
                    Category = g.Key ?? "Unknown",
                    Total = g.Sum(x =>
                        x.SellingPrice * x.QuantitySold)
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            double[] values =
                grouped.Select(x => (double)x.Total).ToArray();

            var plot = new Plot();

            if (values.Length > 0)
                plot.Add.Pie(values);

            plot.Title("Category Performance");

            plot.SavePng(path, 700, 500);
        }

        // =====================================================
        // BIG BAR GRAPH
        // =====================================================

        private static void CreateTopItemsChart(
            List<History> sales,
            string path)
        {
            var grouped = sales
                .GroupBy(x => x.ItemName)
                .Select(g => new
                {
                    ItemName = g.Key ?? "Unknown",
                    Quantity = g.Sum(x => x.QuantitySold)
                })
                .OrderByDescending(x => x.Quantity)
                .Take(5)
                .ToList();

            double[] values =
                grouped.Select(x => (double)x.Quantity).ToArray();

            var plot = new Plot();

            if (values.Length > 0)
                plot.Add.Bars(values);

            plot.Title("Top Selling Items");
            plot.XLabel("Items");
            plot.YLabel("Quantity Sold");

            plot.SavePng(path, 1400, 650);
        }

        // =====================================================
        // FILTER
        // =====================================================

        private List<History> GetFilteredSales(
            string? range,
            DateTime? fromDate,
            DateTime? toDate,
            string? category)
        {
            var query = _context.Histories
                .AsNoTracking()
                .AsQueryable();

            if (range == "daily" && fromDate.HasValue)
            {
                DateTime start = fromDate.Value.Date;
                DateTime end = start.AddDays(1);

                query = query.Where(x =>
                    x.TransactionDate >= start &&
                    x.TransactionDate < end);
            }
            else if (range == "weekly" && fromDate.HasValue)
            {
                DateTime start = fromDate.Value.Date;
                DateTime end = start.AddDays(7);

                query = query.Where(x =>
                    x.TransactionDate >= start &&
                    x.TransactionDate < end);
            }
            else if (range == "monthly" && fromDate.HasValue)
            {
                DateTime start =
                    new DateTime(fromDate.Value.Year,
                                 fromDate.Value.Month, 1);

                DateTime end = start.AddMonths(1);

                query = query.Where(x =>
                    x.TransactionDate >= start &&
                    x.TransactionDate < end);
            }
            else if (range == "custom" &&
                     fromDate.HasValue &&
                     toDate.HasValue)
            {
                DateTime start = fromDate.Value.Date;
                DateTime end = toDate.Value.Date.AddDays(1);

                query = query.Where(x =>
                    x.TransactionDate >= start &&
                    x.TransactionDate < end);
            }

            if (!string.IsNullOrWhiteSpace(category) &&
                category != "All")
            {
                query = query.Where(x => x.Category == category);
            }

            return query
                .OrderByDescending(x => x.TransactionDate)
                .ToList();
        }

        // =====================================================
        // PERIOD
        // =====================================================

        private static string GetPeriodText(
            string? range,
            DateTime? fromDate,
            DateTime? toDate)
        {
            if (range == "daily" && fromDate.HasValue)
                return fromDate.Value.ToString("MMMM dd, yyyy");

            if (range == "weekly" && fromDate.HasValue)
            {
                var end = fromDate.Value.AddDays(6);
                return $"{fromDate.Value:MMMM dd} - {end:MMMM dd, yyyy}";
            }

            if (range == "monthly" && fromDate.HasValue)
                return fromDate.Value.ToString("MMMM yyyy");

            if (range == "custom" &&
                fromDate.HasValue &&
                toDate.HasValue)
                return $"{fromDate.Value:MMMM dd} - {toDate.Value:MMMM dd, yyyy}";

            return "All Dates";
        }

        // =====================================================
        // STYLES
        // =====================================================

        private static IContainer HeaderStyle(IContainer c)
        {
            return c.Background(QuestColors.Green.Darken2)
                .Padding(5)
                .DefaultTextStyle(x =>
                    x.FontColor(QuestColors.White)
                     .Bold()
                     .FontSize(8));
        }

        private static IContainer CellStyle(IContainer c)
        {
            return c.BorderBottom(1)
                .BorderColor(QuestColors.Grey.Lighten2)
                .Padding(4)
                .DefaultTextStyle(x => x.FontSize(7));
        }

        private static IContainer LabelStyle(IContainer c)
        {
            return c.Padding(4)
                .DefaultTextStyle(x =>
                    x.Bold().FontSize(9));
        }

        private static IContainer ValueStyle(IContainer c)
        {
            return c.Padding(4)
                .DefaultTextStyle(x => x.FontSize(9));
        }

        private static void DeleteFile(string path)
        {
            try
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
            catch { }
        }
    }
}