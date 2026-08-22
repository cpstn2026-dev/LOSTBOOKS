using LOSTBOOKS.Data;
using LOSTBOOKS.Models;
using LOSTBOOKS.ViewModels;
using LOSTBOOKS.ViewModels.SalesAnalysisViewModel;
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
            ViewBag.Category =
                string.IsNullOrWhiteSpace(category) ? "All" : category;

            ViewBag.CategoryText =
                string.IsNullOrWhiteSpace(category) ||
                category.Equals("All", StringComparison.OrdinalIgnoreCase)
                ? "All Categories"
                : category;

            ViewBag.PeriodText =
                GetPeriodText(range, fromDate, toDate);

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

            var (periodStart, periodEnd) =
                GetPeriodBounds(range, fromDate, toDate);

            bool hasPreviousPeriod =
                periodStart.HasValue &&
                periodEnd.HasValue;

            List<History> previousSales =
                new List<History>();

            if (hasPreviousPeriod)
            {
                TimeSpan duration =
                    periodEnd!.Value - periodStart!.Value;

                DateTime prevStart =
                    periodStart.Value - duration;

                DateTime prevEnd =
                    periodStart.Value;

                previousSales =
                    GetSalesForRange(
                        prevStart,
                        prevEnd,
                        category);
            }

            ViewBag.SalesGrowth =
                BuildGrowthAnalysis(
                    sales,
                    previousSales,
                    hasPreviousPeriod);

            ViewBag.CategoryAnalysis =
                BuildCategoryAnalysis(
                    sales,
                    previousSales,
                    hasPreviousPeriod);

            ViewBag.ItemAnalysis =
                BuildItemAnalysis(
                    sales,
                    previousSales,
                    hasPreviousPeriod);

            ViewBag.TrendAnalysis =
                BuildTrendAnalysis(sales);

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
            var sales =
                GetFilteredSales(
                    range,
                    fromDate,
                    toDate,
                    category);

            if (sales.Count == 0)
                return Content("No sales records found.");

            decimal totalSales =
                sales.Sum(x =>
                    x.SellingPrice * x.QuantitySold);

            int totalTransactions =
                sales.Count;

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
                    Total = g.Sum(x =>
                        x.SellingPrice * x.QuantitySold)
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
                    TotalSales = g.Sum(x =>
                        x.SellingPrice * x.QuantitySold)
                })
                .ToList();

            var categorySummary = sales
                .GroupBy(x => x.Category)
                .OrderByDescending(g =>
                    g.Sum(x =>
                        x.SellingPrice * x.QuantitySold))
                .Select(g => new
                {
                    Category = g.Key,
                    Transactions = g.Count(),
                    Quantity = g.Sum(x => x.QuantitySold),
                    TotalSales = g.Sum(x =>
                        x.SellingPrice * x.QuantitySold)
                })
                .ToList();

            string categoryText =
                string.IsNullOrWhiteSpace(category) ||
                category.Equals(
                    "All",
                    StringComparison.OrdinalIgnoreCase)
                ? "All Categories"
                : category;

            string periodText =
                GetPeriodText(
                    range,
                    fromDate,
                    toDate);


            // =====================================================
            // PDF SALES ANALYSIS
            // =====================================================

            var pdfGrowth =
                new SalesGrowthAnalysis();

            var pdfCategoryAnalysis =
                new CategoryAnalysisSummary();

            var pdfTrend =
                new TrendAnalysis();

            var pdfItemAnalysis =
                new ItemAnalysisSummary();

            bool pdfHasPreviousPeriod = false;


            // =====================================================
            // CURRENT PERIOD ANALYSIS
            // =====================================================

            if (sales.Count > 0)
            {
                pdfTrend.HasData = true;

                var dailyTotals = sales
                    .GroupBy(x => x.TransactionDate.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Total = g.Sum(x =>
                            x.SellingPrice * x.QuantitySold)
                    })
                    .OrderBy(x => x.Date)
                    .ToList();

                if (dailyTotals.Count > 0)
                {
                    var highestDay =
                        dailyTotals
                            .OrderByDescending(x => x.Total)
                            .First();

                    var lowestDay =
                        dailyTotals
                            .OrderBy(x => x.Total)
                            .First();

                    var beginningDay =
                        dailyTotals.First();

                    var endingDay =
                        dailyTotals.Last();

                    pdfTrend.HighestSalesDate =
                        highestDay.Date;

                    pdfTrend.HighestSalesDateTotal =
                        highestDay.Total;

                    pdfTrend.LowestSalesDate =
                        lowestDay.Date;

                    pdfTrend.LowestSalesDateTotal =
                        lowestDay.Total;

                    pdfTrend.BeginningTotal =
                        beginningDay.Total;

                    pdfTrend.EndingTotal =
                        endingDay.Total;

                    pdfTrend.ChangeAmount =
                        endingDay.Total -
                        beginningDay.Total;

                    if (beginningDay.Total != 0)
                    {
                        pdfTrend.ChangePercent =
                            (pdfTrend.ChangeAmount /
                             beginningDay.Total) * 100m;
                    }
                }
            }


            // =====================================================
            // PREVIOUS PERIOD
            // =====================================================

            List<History> previousSales =
                new List<History>();


            if (sales.Count > 0 &&
                !string.IsNullOrWhiteSpace(range) &&
                !range.Equals(
                    "all",
                    StringComparison.OrdinalIgnoreCase))
            {
                DateTime currentStart =
                    fromDate?.Date
                    ?? sales.Min(
                        x => x.TransactionDate).Date;

                DateTime currentEnd =
                    toDate?.Date
                    ?? sales.Max(
                        x => x.TransactionDate).Date;


                DateTime previousStart;
                DateTime previousEnd;


                if (range.Equals(
                    "daily",
                    StringComparison.OrdinalIgnoreCase))
                {
                    previousStart =
                        currentStart.AddDays(-1);

                    previousEnd =
                        previousStart;
                }
                else if (range.Equals(
                    "weekly",
                    StringComparison.OrdinalIgnoreCase))
                {
                    previousStart =
                        currentStart.AddDays(-7);

                    previousEnd =
                        currentEnd.AddDays(-7);
                }
                else if (range.Equals(
                    "monthly",
                    StringComparison.OrdinalIgnoreCase))
                {
                    previousStart =
                        currentStart.AddMonths(-1);

                    previousEnd =
                        currentEnd.AddMonths(-1);
                }
                else if (range.Equals(
                    "custom",
                    StringComparison.OrdinalIgnoreCase))
                {
                    int numberOfDays =
                        (currentEnd - currentStart).Days + 1;

                    previousEnd =
                        currentStart.AddDays(-1);

                    previousStart =
                        previousEnd.AddDays(
                            -(numberOfDays - 1));
                }
                else
                {
                    previousStart =
                        currentStart.AddDays(-1);

                    previousEnd =
                        previousStart;
                }


                previousSales =
                    GetFilteredSales(
                        range,
                        previousStart,
                        previousEnd,
                        category);
            }


            // =====================================================
            // SALES GROWTH
            // =====================================================

            decimal currentTotal =
                sales.Sum(x =>
                    x.SellingPrice * x.QuantitySold);

            decimal previousTotal =
                previousSales.Sum(x =>
                    x.SellingPrice * x.QuantitySold);


            if (previousSales.Count > 0)
            {
                pdfHasPreviousPeriod = true;

                pdfGrowth.HasPreviousPeriod = true;

                pdfGrowth.CurrentTotal =
                    currentTotal;

                pdfGrowth.PreviousTotal =
                    previousTotal;

                pdfGrowth.ChangeAmount =
                    currentTotal -
                    previousTotal;


                if (previousTotal != 0)
                {
                    pdfGrowth.ChangePercent =
                        (pdfGrowth.ChangeAmount /
                         previousTotal) * 100m;
                }


                if (pdfGrowth.ChangeAmount > 0)
                {
                    pdfGrowth.Direction =
                        "Increased";
                }
                else if (pdfGrowth.ChangeAmount < 0)
                {
                    pdfGrowth.Direction =
                        "Decreased";
                }
                else
                {
                    pdfGrowth.Direction =
                        "Unchanged";
                }
            }


            // =====================================================
            // CATEGORY ANALYSIS
            // =====================================================

            var currentCategoryTotals =
                sales
                    .GroupBy(x => x.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        Total = g.Sum(x =>
                            x.SellingPrice *
                            x.QuantitySold)
                    })
                    .ToList();


            var previousCategoryTotals =
                previousSales
                    .GroupBy(x => x.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        Total = g.Sum(x =>
                            x.SellingPrice *
                            x.QuantitySold)
                    })
                    .ToList();


            var allCategories =
                currentCategoryTotals
                    .Select(x => x.Category)
                    .Union(
                        previousCategoryTotals
                            .Select(x => x.Category))
                    .Distinct()
                    .ToList();


            foreach (var categoryName in allCategories)
            {
                decimal currentCategorySales =
                    currentCategoryTotals
                        .Where(x =>
                            x.Category == categoryName)
                        .Select(x => x.Total)
                        .FirstOrDefault();


                decimal previousCategorySales =
                    previousCategoryTotals
                        .Where(x =>
                            x.Category == categoryName)
                        .Select(x => x.Total)
                        .FirstOrDefault();


                decimal changeAmount =
                    currentCategorySales -
                    previousCategorySales;


                decimal? changePercent = null;


                if (previousCategorySales != 0)
                {
                    changePercent =
                        (changeAmount /
                         previousCategorySales) * 100m;
                }


                pdfCategoryAnalysis.Categories.Add(
                    new CategoryAnalysisRow
                    {
                        Category = categoryName,

                        CurrentSales =
                            currentCategorySales,

                        PreviousSales =
                            previousCategorySales,

                        ChangeAmount =
                            changeAmount,

                        ChangePercent =
                            changePercent
                    });
            }


            // =====================================================
            // HIGHEST PERFORMING CATEGORY
            // =====================================================

            var highestCategory =
                pdfCategoryAnalysis.Categories
                    .OrderByDescending(
                        x => x.CurrentSales)
                    .FirstOrDefault();


            if (highestCategory != null)
            {
                pdfCategoryAnalysis
                    .HighestPerformingCategory =
                    highestCategory.Category;
            }


            // =====================================================
            // CATEGORY INCREASE / DECREASE
            // =====================================================

            if (pdfHasPreviousPeriod)
            {
                var increases =
                    pdfCategoryAnalysis.Categories
                        .Where(x =>
                            x.ChangePercent.HasValue &&
                            x.ChangePercent.Value > 0)
                        .OrderByDescending(x =>
                            x.ChangePercent.Value)
                        .FirstOrDefault();


                if (increases != null)
                {
                    pdfCategoryAnalysis
                        .LargestIncreaseCategory =
                        increases.Category;

                    pdfCategoryAnalysis
                        .LargestIncreasePercent =
                        increases.ChangePercent;
                }


                var decreases =
                    pdfCategoryAnalysis.Categories
                        .Where(x =>
                            x.ChangePercent.HasValue &&
                            x.ChangePercent.Value < 0)
                        .OrderBy(x =>
                            x.ChangePercent.Value)
                        .FirstOrDefault();


                if (decreases != null)
                {
                    pdfCategoryAnalysis
                        .LargestDecreaseCategory =
                        decreases.Category;

                    pdfCategoryAnalysis
                        .LargestDecreasePercent =
                        decreases.ChangePercent;
                }


                pdfCategoryAnalysis.HasPreviousPeriod =
                    true;
            }


            // =====================================================
            // ITEM PERFORMANCE ANALYSIS FOR PDF
            // =====================================================

            pdfItemAnalysis =
                BuildItemAnalysis(
                    sales,
                    previousSales,
                    pdfHasPreviousPeriod);


            // =====================================================
            // BUILD ANALYSIS TEXT STRINGS (used beside each chart)
            // =====================================================

            string overallAnalysisText;

            if (!pdfHasPreviousPeriod)
            {
                overallAnalysisText =
                    $"The current report shows ₱{totalSales:N2} in total sales " +
                    $"from {totalTransactions} transaction(s).";
            }
            else
            {
                overallAnalysisText =
                    $"Sales {pdfGrowth.Direction.ToLower()} by " +
                    $"{Math.Abs(pdfGrowth.ChangePercent ?? 0):N2}% " +
                    $"(₱{pdfGrowth.ChangeAmount:N2}) compared with the previous " +
                    $"period (₱{pdfGrowth.PreviousTotal:N2} to ₱{pdfGrowth.CurrentTotal:N2}).";
            }

            string trendAnalysisText;

            if (!pdfTrend.HasData)
            {
                trendAnalysisText =
                    "No sales trend data available for the selected period.";
            }
            else
            {
                trendAnalysisText =
                    $"Sales reached their highest point on " +
                    $"{pdfTrend.HighestSalesDate:MMMM dd, yyyy} at " +
                    $"₱{pdfTrend.HighestSalesDateTotal:N2}, while the lowest " +
                    $"sales were recorded on {pdfTrend.LowestSalesDate:MMMM dd, yyyy} " +
                    $"at ₱{pdfTrend.LowestSalesDateTotal:N2}.";

                if (pdfTrend.ChangePercent.HasValue)
                {
                    string direction =
                        pdfTrend.ChangeAmount > 0
                            ? "increased"
                            : pdfTrend.ChangeAmount < 0
                                ? "decreased"
                                : "remained unchanged";

                    trendAnalysisText +=
                        $" Between the beginning and end of the selected period, " +
                        $"sales {direction} by {Math.Abs(pdfTrend.ChangePercent.Value):N2}%.";
                }
            }

            string categoryAnalysisText;

            if (pdfCategoryAnalysis.Categories.Count == 0)
            {
                categoryAnalysisText =
                    "No category data available for the selected period.";
            }
            else
            {
                categoryAnalysisText =
                    $"{pdfCategoryAnalysis.HighestPerformingCategory ?? "-"} " +
                    $"is currently the highest-performing category.";

                if (pdfCategoryAnalysis.HasPreviousPeriod &&
                    pdfCategoryAnalysis.LargestIncreaseCategory != null)
                {
                    categoryAnalysisText +=
                        $" {pdfCategoryAnalysis.LargestIncreaseCategory} recorded the " +
                        $"largest increase at {pdfCategoryAnalysis.LargestIncreasePercent:N2}%.";
                }

                if (pdfCategoryAnalysis.HasPreviousPeriod &&
                    pdfCategoryAnalysis.LargestDecreaseCategory != null)
                {
                    categoryAnalysisText +=
                        $" {pdfCategoryAnalysis.LargestDecreaseCategory} recorded the " +
                        $"largest decrease at " +
                        $"{Math.Abs(pdfCategoryAnalysis.LargestDecreasePercent ?? 0):N2}%.";
                }
            }

            string itemAnalysisText;

            if (pdfItemAnalysis.HighestQuantityItem == null)
            {
                itemAnalysisText =
                    "No item data available for the selected period.";
            }
            else
            {
                itemAnalysisText = "";

                if (pdfItemAnalysis.HighestSalesItem != null)
                {
                    itemAnalysisText +=
                        $"{pdfItemAnalysis.HighestSalesItem.ItemName} generated the " +
                        $"highest total sales at ₱{pdfItemAnalysis.HighestSalesItem.CurrentSales:N2}. ";
                }

                if (pdfItemAnalysis.HighestQuantityItem != null)
                {
                    itemAnalysisText +=
                        $"{pdfItemAnalysis.HighestQuantityItem.ItemName} had the highest " +
                        $"quantity sold at {pdfItemAnalysis.HighestQuantityItem.CurrentQuantity} unit(s).";
                }
            }


            // =====================================================
            // CREATE TEMP CHART FOLDER
            // =====================================================

            string tempFolder =
                Path.Combine(
                    Path.GetTempPath(),
                    "LostBooksCharts");

            Directory.CreateDirectory(tempFolder);

            string lineChartPath =
                Path.Combine(
                    tempFolder,
                    $"line-{Guid.NewGuid()}.png");

            string pieChartPath =
                Path.Combine(
                    tempFolder,
                    $"pie-{Guid.NewGuid()}.png");

            string barChartPath =
                Path.Combine(
                    tempFolder,
                    $"bar-{Guid.NewGuid()}.png");


            try
            {
                CreateSalesTrendChart(
                    sales,
                    lineChartPath);

                CreateCategoryChart(
                    sales,
                    pieChartPath);

                CreateTopItemsChart(
                    sales,
                    barChartPath);


                byte[] lineBytes =
                    System.IO.File.ReadAllBytes(
                        lineChartPath);

                byte[] pieBytes =
                    System.IO.File.ReadAllBytes(
                        pieChartPath);

                byte[] barBytes =
                    System.IO.File.ReadAllBytes(
                        barChartPath);


                var document =
                    Document.Create(container =>
                    {
                        // =====================================================
                        // PAGE 1 — REPORT (summary + tables + written analysis)
                        // =====================================================

                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);

                            page.Margin(35);


                            page.Header().Column(col =>
                            {
                                col.Item()
                                    .AlignCenter()
                                    .Text("LOST BOOKS CEBU")
                                    .Bold()
                                    .FontSize(20);

                                col.Item()
                                    .AlignCenter()
                                    .Text("SALES REPORT")
                                    .Bold()
                                    .FontSize(15);

                                col.Item()
                                    .PaddingTop(5)
                                    .AlignCenter()
                                    .Text(
                                        $"Report Period: {periodText}")
                                    .FontSize(9);

                                col.Item()
                                    .AlignCenter()
                                    .Text(
                                        $"Category: {categoryText}")
                                    .FontSize(9);
                            });


                            page.Content()
                                .PaddingTop(15)
                                .Column(col =>
                                {
                                    col.Spacing(10);


                                    // SUMMARY

                                    col.Item()
                                        .Text("SUMMARY")
                                        .Bold()
                                        .FontSize(13);


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

                                    col.Item()
                                        .PaddingTop(6)
                                        .Text("DAILY SALES SUMMARY")
                                        .Bold()
                                        .FontSize(13);


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
                                            table.Cell().Element(CellStyle).Text(item.Date.ToString("MMM dd, yyyy"));
                                            table.Cell().Element(CellStyle).Text(item.Transactions);
                                            table.Cell().Element(CellStyle).Text(item.Quantity);
                                            table.Cell().Element(CellStyle).Text($"₱{item.TotalSales:N2}");
                                        }
                                    });


                                    // CATEGORY SUMMARY

                                    col.Item()
                                        .PaddingTop(6)
                                        .Text("CATEGORY SUMMARY")
                                        .Bold()
                                        .FontSize(13);


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


                                    // WRITTEN SALES ANALYSIS (report narrative)

                                    col.Item()
                                        .PaddingTop(10)
                                        .Text("SALES ANALYSIS")
                                        .Bold()
                                        .FontSize(13);

                                    col.Item().LineHorizontal(1);

                                    col.Item()
                                        .PaddingTop(4)
                                        .Text(overallAnalysisText)
                                        .FontSize(9);

                                    col.Item()
                                        .Text(trendAnalysisText)
                                        .FontSize(9);

                                    col.Item()
                                        .Text(categoryAnalysisText)
                                        .FontSize(9);

                                    col.Item()
                                        .Text(itemAnalysisText)
                                        .FontSize(9);


                                    // GRAND TOTAL

                                    col.Item()
                                        .PaddingTop(10)
                                        .AlignRight()
                                        .Text($"GRAND TOTAL SALES: ₱{totalSales:N2}")
                                        .Bold()
                                        .FontSize(14);
                                });


                            page.Footer()
                                .AlignCenter()
                                .Text($"Generated on: {DateTime.Now:MMMM dd, yyyy h:mm tt}")
                                .FontSize(8);
                        });


                        // =====================================================
                        // PAGE 2 — ANALYSIS (single page, 3 columns,
                        // each chart with its analysis directly below it,
                        // matching the web page layout)
                        // =====================================================

                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4.Landscape());

                            page.Margin(25);


                            page.Header().Column(col =>
                            {
                                col.Item()
                                    .AlignCenter()
                                    .Text("LOST BOOKS CEBU")
                                    .Bold()
                                    .FontSize(20);

                                col.Item()
                                    .AlignCenter()
                                    .Text("SALES ANALYSIS")
                                    .Bold()
                                    .FontSize(15);

                                col.Item()
                                    .PaddingTop(5)
                                    .AlignCenter()
                                    .Text($"Report Period: {periodText}")
                                    .FontSize(9);

                                col.Item()
                                    .AlignCenter()
                                    .Text($"Category: {categoryText}")
                                    .FontSize(9);
                            });


                            page.Content()
                                .PaddingTop(12)
                                .Row(row =>
                                {
                                    row.Spacing(14);


                                    // SALES TREND — chart on top, analysis below it

                                    row.RelativeItem()
                                        .Border(1)
                                        .BorderColor(QuestColors.Grey.Lighten2)
                                        .Column(chart =>
                                        {
                                            chart.Item()
                                                .Background(QuestColors.Green.Darken2)
                                                .Padding(6)
                                                .Text("SALES TREND")
                                                .Bold()
                                                .FontColor(QuestColors.White)
                                                .FontSize(10);

                                            chart.Item()
                                                .Padding(6)
                                                .Height(220)
                                                .Image(lineBytes)
                                                .FitArea();

                                            chart.Item()
                                                .Background(QuestColors.Green.Lighten5)
                                                .BorderTop(2)
                                                .BorderColor(QuestColors.Green.Darken2)
                                                .Padding(8)
                                                .Column(text =>
                                                {
                                                    text.Item()
                                                        .Text("Analysis")
                                                        .Bold()
                                                        .FontColor(QuestColors.Green.Darken2)
                                                        .FontSize(9);

                                                    text.Item()
                                                        .PaddingTop(3)
                                                        .Text(trendAnalysisText)
                                                        .FontSize(7.5f);
                                                });
                                        });


                                    // CATEGORY PERFORMANCE — chart on top, analysis below it

                                    row.RelativeItem()
                                        .Border(1)
                                        .BorderColor(QuestColors.Grey.Lighten2)
                                        .Column(chart =>
                                        {
                                            chart.Item()
                                                .Background(QuestColors.Green.Darken2)
                                                .Padding(6)
                                                .Text("CATEGORY PERFORMANCE")
                                                .Bold()
                                                .FontColor(QuestColors.White)
                                                .FontSize(10);

                                            chart.Item()
                                                .Padding(6)
                                                .Height(220)
                                                .Image(pieBytes)
                                                .FitArea();

                                            chart.Item()
                                                .Background(QuestColors.Green.Lighten5)
                                                .BorderTop(2)
                                                .BorderColor(QuestColors.Green.Darken2)
                                                .Padding(8)
                                                .Column(text =>
                                                {
                                                    text.Item()
                                                        .Text("Analysis")
                                                        .Bold()
                                                        .FontColor(QuestColors.Green.Darken2)
                                                        .FontSize(9);

                                                    text.Item()
                                                        .PaddingTop(3)
                                                        .Text(categoryAnalysisText)
                                                        .FontSize(7.5f);
                                                });
                                        });


                                    // TOP SELLING ITEMS — chart on top, analysis below it

                                    row.RelativeItem()
                                        .Border(1)
                                        .BorderColor(QuestColors.Grey.Lighten2)
                                        .Column(chart =>
                                        {
                                            chart.Item()
                                                .Background(QuestColors.Green.Darken2)
                                                .Padding(6)
                                                .Text("TOP SELLING ITEMS")
                                                .Bold()
                                                .FontColor(QuestColors.White)
                                                .FontSize(10);

                                            chart.Item()
                                                .Padding(6)
                                                .Height(220)
                                                .Image(barBytes)
                                                .FitArea();

                                            chart.Item()
                                                .Background(QuestColors.Green.Lighten5)
                                                .BorderTop(2)
                                                .BorderColor(QuestColors.Green.Darken2)
                                                .Padding(8)
                                                .Column(text =>
                                                {
                                                    text.Item()
                                                        .Text("Analysis")
                                                        .Bold()
                                                        .FontColor(QuestColors.Green.Darken2)
                                                        .FontSize(9);

                                                    text.Item()
                                                        .PaddingTop(3)
                                                        .Text(itemAnalysisText)
                                                        .FontSize(7.5f);
                                                });
                                        });
                                });


                            page.Footer()
                                .AlignCenter()
                                .Text($"Generated on: {DateTime.Now:MMMM dd, yyyy h:mm tt}")
                                .FontSize(8);
                        });
                    });


                byte[] pdf =
                    document.GeneratePdf();

                return File(
                    pdf,
                    "application/pdf");
            }
            finally
            {
                DeleteFile(lineChartPath);
                DeleteFile(pieChartPath);
                DeleteFile(barChartPath);
            }
        }


        // =====================================================
        // SALES TREND CHART
        // =====================================================

        private static void CreateSalesTrendChart(
            List<History> sales,
            string path)
        {
            var grouped =
                sales
                    .GroupBy(x => x.TransactionDate.Date)
                    .OrderBy(x => x.Key)
                    .ToList();


            double[] xs =
                Enumerable
                    .Range(0, grouped.Count)
                    .Select(x => (double)x)
                    .ToArray();


            double[] ys =
                grouped
                    .Select(g =>
                        (double)g.Sum(x =>
                            x.SellingPrice *
                            x.QuantitySold))
                    .ToArray();

            string[] labels =
                grouped
                    .Select(g => g.Key.ToString("MMM dd"))
                    .ToArray();


            var plot =
                new Plot();


            if (ys.Length > 0)
            {
                var scatter =
                    plot.Add.Scatter(xs, ys);

                scatter.LineWidth = 3;
                scatter.MarkerSize = 7;
                scatter.Color = ScottPlot.Color.FromHex("#198754");

                plot.Axes.Bottom.SetTicks(xs, labels);
            }


            plot.Title("Sales Trend");
            plot.XLabel("Date");
            plot.YLabel("Sales (₱)");

            plot.SavePng(
                path,
                1000,
                500);
        }


        // =====================================================
        // CATEGORY PIE CHART
        // =====================================================

        private static void CreateCategoryChart(
            List<History> sales,
            string path)
        {
            var grouped =
                sales
                    .GroupBy(x => x.Category)
                    .Select(g => new
                    {
                        Category =
                            g.Key ?? "Unknown",

                        Total =
                            g.Sum(x =>
                                x.SellingPrice *
                                x.QuantitySold)
                    })
                    .OrderByDescending(x => x.Total)
                    .ToList();


            string[] palette =
            {
                "#198754", "#ffc107", "#0d6efd",
                "#dc3545", "#6f42c1", "#20c997"
            };


            var plot =
                new Plot();


            if (grouped.Count > 0)
            {
                var slices = new List<PieSlice>();

                for (int i = 0; i < grouped.Count; i++)
                {
                    slices.Add(new PieSlice
                    {
                        Value = (double)grouped[i].Total,
                        Label = grouped[i].Category,
                        FillColor = ScottPlot.Color.FromHex(
                            palette[i % palette.Length])
                    });
                }

                plot.Add.Pie(slices);
                plot.ShowLegend();
            }


            plot.Title(
                "Category Performance");


            plot.SavePng(
                path,
                700,
                500);
        }


        // =====================================================
        // TOP ITEMS BAR CHART
        // =====================================================

        private static void CreateTopItemsChart(
            List<History> sales,
            string path)
        {
            var grouped =
                sales
                    .GroupBy(x => x.ItemName)
                    .Select(g => new
                    {
                        ItemName =
                            g.Key ?? "Unknown",

                        Quantity =
                            g.Sum(x =>
                                x.QuantitySold)
                    })
                    .OrderByDescending(
                        x => x.Quantity)
                    .Take(5)
                    .ToList();


            double[] values =
                grouped
                    .Select(x =>
                        (double)x.Quantity)
                    .ToArray();

            double[] positions =
                Enumerable
                    .Range(0, grouped.Count)
                    .Select(x => (double)x)
                    .ToArray();

            string[] labels =
                grouped
                    .Select(x => x.ItemName)
                    .ToArray();


            var plot =
                new Plot();


            if (values.Length > 0)
            {
                var bars = plot.Add.Bars(positions, values);

                foreach (var bar in bars.Bars)
                {
                    bar.FillColor = ScottPlot.Color.FromHex("#198754");
                }

                plot.Axes.Bottom.SetTicks(positions, labels);
            }


            plot.Title(
                "Top Selling Items");

            plot.XLabel("Items");

            plot.YLabel(
                "Quantity Sold");


            plot.SavePng(
                path,
                1400,
                650);
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
            var query =
                _context.Histories
                    .AsNoTracking()
                    .AsQueryable();


            if (range == "daily" &&
                fromDate.HasValue)
            {
                DateTime start =
                    fromDate.Value.Date;

                DateTime end =
                    start.AddDays(1);


                query =
                    query.Where(x =>
                        x.TransactionDate >= start &&
                        x.TransactionDate < end);
            }
            else if (range == "weekly" &&
                     fromDate.HasValue)
            {
                DateTime start =
                    fromDate.Value.Date;

                DateTime end =
                    start.AddDays(7);


                query =
                    query.Where(x =>
                        x.TransactionDate >= start &&
                        x.TransactionDate < end);
            }
            else if (range == "monthly" &&
                     fromDate.HasValue)
            {
                DateTime start =
                    new DateTime(
                        fromDate.Value.Year,
                        fromDate.Value.Month,
                        1);

                DateTime end =
                    start.AddMonths(1);


                query =
                    query.Where(x =>
                        x.TransactionDate >= start &&
                        x.TransactionDate < end);
            }
            else if (range == "custom" &&
                     fromDate.HasValue &&
                     toDate.HasValue)
            {
                DateTime start =
                    fromDate.Value.Date;

                DateTime end =
                    toDate.Value.Date.AddDays(1);


                query =
                    query.Where(x =>
                        x.TransactionDate >= start &&
                        x.TransactionDate < end);
            }


            if (!string.IsNullOrWhiteSpace(category) &&
                category != "All")
            {
                query =
                    query.Where(x =>
                        x.Category == category);
            }


            return query
                .OrderByDescending(
                    x => x.TransactionDate)
                .ToList();
        }


        // =====================================================
        // PERIOD BOUNDS
        // =====================================================

        private static (
            DateTime? Start,
            DateTime? End) GetPeriodBounds(
                string? range,
                DateTime? fromDate,
                DateTime? toDate)
        {
            if (range == "daily" &&
                fromDate.HasValue)
            {
                DateTime start =
                    fromDate.Value.Date;

                return (
                    start,
                    start.AddDays(1));
            }


            if (range == "weekly" &&
                fromDate.HasValue)
            {
                DateTime start =
                    fromDate.Value.Date;

                return (
                    start,
                    start.AddDays(7));
            }


            if (range == "monthly" &&
                fromDate.HasValue)
            {
                DateTime start =
                    new DateTime(
                        fromDate.Value.Year,
                        fromDate.Value.Month,
                        1);

                return (
                    start,
                    start.AddMonths(1));
            }


            if (range == "custom" &&
                fromDate.HasValue &&
                toDate.HasValue)
            {
                DateTime start =
                    fromDate.Value.Date;

                DateTime end =
                    toDate.Value.Date.AddDays(1);

                return (
                    start,
                    end);
            }


            return (
                null,
                null);
        }


        // =====================================================
        // SALES FOR EXPLICIT DATE RANGE
        // =====================================================

        private List<History> GetSalesForRange(
            DateTime? start,
            DateTime? end,
            string? category)
        {
            var query =
                _context.Histories
                    .AsNoTracking()
                    .AsQueryable();


            if (start.HasValue &&
                end.HasValue)
            {
                query =
                    query.Where(x =>
                        x.TransactionDate >= start.Value &&
                        x.TransactionDate < end.Value);
            }


            if (!string.IsNullOrWhiteSpace(category) &&
                category != "All")
            {
                query =
                    query.Where(x =>
                        x.Category == category);
            }


            return query
                .OrderByDescending(
                    x => x.TransactionDate)
                .ToList();
        }


        // =====================================================
        // A. SALES CHANGE / GROWTH ANALYSIS
        // =====================================================

        private static SalesGrowthAnalysis BuildGrowthAnalysis(
            List<History> currentSales,
            List<History> previousSales,
            bool hasPreviousPeriod)
        {
            decimal currentTotal =
                currentSales.Sum(x =>
                    x.SellingPrice *
                    x.QuantitySold);


            decimal previousTotal =
                previousSales.Sum(x =>
                    x.SellingPrice *
                    x.QuantitySold);


            decimal changeAmount =
                currentTotal -
                previousTotal;


            decimal? changePercent =
                previousTotal != 0
                    ? Math.Round(
                        (changeAmount /
                         previousTotal) * 100,
                        2)
                    : (decimal?)null;


            string direction =
                changeAmount > 0
                    ? "Increased"
                    : changeAmount < 0
                        ? "Decreased"
                        : "Unchanged";


            return new SalesGrowthAnalysis
            {
                CurrentTotal =
                    currentTotal,

                PreviousTotal =
                    previousTotal,

                ChangeAmount =
                    changeAmount,

                ChangePercent =
                    changePercent,

                Direction =
                    direction,

                HasPreviousPeriod =
                    hasPreviousPeriod
            };
        }


        // =====================================================
        // B. CATEGORY PERFORMANCE ANALYSIS
        // =====================================================

        private static CategoryAnalysisSummary BuildCategoryAnalysis(
            List<History> currentSales,
            List<History> previousSales,
            bool hasPreviousPeriod)
        {
            var currentTotals =
                currentSales
                    .GroupBy(x => x.Category)
                    .Select(g => new
                    {
                        Category = g.Key,

                        Total =
                            g.Sum(x =>
                                x.SellingPrice *
                                x.QuantitySold)
                    })
                    .ToList();


            var previousTotals =
                previousSales
                    .GroupBy(x => x.Category)
                    .Select(g => new
                    {
                        Category = g.Key,

                        Total =
                            g.Sum(x =>
                                x.SellingPrice *
                                x.QuantitySold)
                    })
                    .ToList();


            var categoryNames =
                currentTotals
                    .Select(x => x.Category)
                    .Union(
                        previousTotals
                            .Select(x => x.Category))
                    .Where(c =>
                        !string.IsNullOrWhiteSpace(c))
                    .Distinct()
                    .ToList();


            var rows =
                new List<CategoryAnalysisRow>();


            foreach (var cat in categoryNames)
            {
                decimal current =
                    currentTotals
                        .FirstOrDefault(
                            x => x.Category == cat)
                        ?.Total ?? 0m;


                decimal previous =
                    previousTotals
                        .FirstOrDefault(
                            x => x.Category == cat)
                        ?.Total ?? 0m;


                decimal changeAmount =
                    current -
                    previous;


                decimal? changePercent =
                    previous != 0
                        ? Math.Round(
                            (changeAmount /
                             previous) * 100,
                            2)
                        : (decimal?)null;


                rows.Add(
                    new CategoryAnalysisRow
                    {
                        Category = cat,

                        CurrentSales =
                            current,

                        PreviousSales =
                            previous,

                        ChangeAmount =
                            changeAmount,

                        ChangePercent =
                            changePercent
                    });
            }


            rows =
                rows
                    .OrderByDescending(
                        x => x.CurrentSales)
                    .ToList();


            var summary =
                new CategoryAnalysisSummary
                {
                    Categories =
                        rows,

                    HasPreviousPeriod =
                        hasPreviousPeriod
                };


            var highestPerforming =
                rows
                    .OrderByDescending(
                        x => x.CurrentSales)
                    .FirstOrDefault();


            summary.HighestPerformingCategory =
                highestPerforming?.Category;


            if (hasPreviousPeriod)
            {
                var largestIncrease =
                    rows
                        .Where(x =>
                            x.ChangePercent.HasValue)
                        .OrderByDescending(
                            x => x.ChangePercent)
                        .FirstOrDefault(
                            x =>
                                x.ChangePercent > 0);


                if (largestIncrease != null)
                {
                    summary.LargestIncreaseCategory =
                        largestIncrease.Category;

                    summary.LargestIncreasePercent =
                        largestIncrease.ChangePercent;
                }


                var largestDecrease =
                    rows
                        .Where(x =>
                            x.ChangePercent.HasValue)
                        .OrderBy(
                            x => x.ChangePercent)
                        .FirstOrDefault(
                            x =>
                                x.ChangePercent < 0);


                if (largestDecrease != null)
                {
                    summary.LargestDecreaseCategory =
                        largestDecrease.Category;

                    summary.LargestDecreasePercent =
                        largestDecrease.ChangePercent;
                }
            }


            return summary;
        }


        // =====================================================
        // C. ITEM PERFORMANCE ANALYSIS
        // =====================================================

        private static ItemAnalysisSummary BuildItemAnalysis(
            List<History> currentSales,
            List<History> previousSales,
            bool hasPreviousPeriod)
        {
            var currentItems =
                currentSales
                    .GroupBy(x =>
                        new
                        {
                            x.ItemID,
                            x.ItemName
                        })
                    .Select(g => new
                    {
                        g.Key.ItemID,
                        g.Key.ItemName,

                        Quantity =
                            g.Sum(x =>
                                x.QuantitySold),

                        Sales =
                            g.Sum(x =>
                                x.SellingPrice *
                                x.QuantitySold)
                    })
                    .ToList();


            var previousItems =
                previousSales
                    .GroupBy(x =>
                        new
                        {
                            x.ItemID,
                            x.ItemName
                        })
                    .Select(g => new
                    {
                        g.Key.ItemID,
                        g.Key.ItemName,

                        Quantity =
                            g.Sum(x =>
                                x.QuantitySold),

                        Sales =
                            g.Sum(x =>
                                x.SellingPrice *
                                x.QuantitySold)
                    })
                    .ToList();


            var rows =
                currentItems
                    .Select(c =>
                    {
                        var prev =
                            previousItems
                                .FirstOrDefault(
                                    p =>
                                        p.ItemID ==
                                        c.ItemID);


                        int previousQuantity =
                            prev?.Quantity ?? 0;


                        decimal previousSalesTotal =
                            prev?.Sales ?? 0m;


                        decimal? quantityChangePercent =
                            previousQuantity != 0
                                ? Math.Round(
                                    ((decimal)(
                                        c.Quantity -
                                        previousQuantity) /
                                     previousQuantity) *
                                    100,
                                    2)
                                : (decimal?)null;


                        return new ItemAnalysisRow
                        {
                            ItemID =
                                c.ItemID,

                            ItemName =
                                c.ItemName,

                            CurrentQuantity =
                                c.Quantity,

                            CurrentSales =
                                c.Sales,

                            PreviousQuantity =
                                previousQuantity,

                            PreviousSales =
                                previousSalesTotal,

                            QuantityChangePercent =
                                quantityChangePercent
                        };
                    })
                    .ToList();


            var summary =
                new ItemAnalysisSummary
                {
                    HasPreviousPeriod =
                        hasPreviousPeriod,

                    HighestQuantityItem =
                        rows
                            .OrderByDescending(
                                x => x.CurrentQuantity)
                            .FirstOrDefault(),

                    HighestSalesItem =
                        rows
                            .OrderByDescending(
                                x => x.CurrentSales)
                            .FirstOrDefault()
                };


            if (hasPreviousPeriod)
            {
                summary.IncreasedItems =
                    rows
                        .Where(x =>
                            x.QuantityChangePercent.HasValue &&
                            x.QuantityChangePercent > 0)
                        .OrderByDescending(
                            x =>
                                x.QuantityChangePercent)
                        .Take(3)
                        .ToList();


                summary.DecreasedItems =
                    rows
                        .Where(x =>
                            x.QuantityChangePercent.HasValue &&
                            x.QuantityChangePercent < 0)
                        .OrderBy(
                            x =>
                                x.QuantityChangePercent)
                        .Take(3)
                        .ToList();
            }


            return summary;
        }


        // =====================================================
        // D. SALES TREND ANALYSIS
        // =====================================================

        private static TrendAnalysis BuildTrendAnalysis(
            List<History> currentSales)
        {
            var dailyTotals =
                currentSales
                    .GroupBy(
                        x => x.TransactionDate.Date)
                    .OrderBy(
                        g => g.Key)
                    .Select(g => new
                    {
                        Date = g.Key,

                        Total =
                            g.Sum(x =>
                                x.SellingPrice *
                                x.QuantitySold)
                    })
                    .ToList();


            if (dailyTotals.Count == 0)
            {
                return new TrendAnalysis
                {
                    HasData = false
                };
            }


            var highest =
                dailyTotals
                    .OrderByDescending(
                        x => x.Total)
                    .First();


            var lowest =
                dailyTotals
                    .OrderBy(
                        x => x.Total)
                    .First();


            decimal beginningTotal =
                dailyTotals.First().Total;


            decimal endingTotal =
                dailyTotals.Last().Total;


            decimal changeAmount =
                endingTotal -
                beginningTotal;


            decimal? changePercent =
                beginningTotal != 0
                    ? Math.Round(
                        (changeAmount /
                         beginningTotal) * 100,
                        2)
                    : (decimal?)null;


            return new TrendAnalysis
            {
                HasData = true,

                HighestSalesDate =
                    highest.Date,

                HighestSalesDateTotal =
                    highest.Total,

                LowestSalesDate =
                    lowest.Date,

                LowestSalesDateTotal =
                    lowest.Total,

                BeginningTotal =
                    beginningTotal,

                EndingTotal =
                    endingTotal,

                ChangeAmount =
                    changeAmount,

                ChangePercent =
                    changePercent
            };
        }


        // =====================================================
        // PERIOD
        // =====================================================

        private static string GetPeriodText(
            string? range,
            DateTime? fromDate,
            DateTime? toDate)
        {
            if (range == "daily" &&
                fromDate.HasValue)
            {
                return fromDate.Value
                    .ToString("MMMM dd, yyyy");
            }


            if (range == "weekly" &&
                fromDate.HasValue)
            {
                var end =
                    fromDate.Value.AddDays(6);

                return
                    $"{fromDate.Value:MMMM dd} - " +
                    $"{end:MMMM dd, yyyy}";
            }


            if (range == "monthly" &&
                fromDate.HasValue)
            {
                return fromDate.Value
                    .ToString("MMMM yyyy");
            }


            if (range == "custom" &&
                fromDate.HasValue &&
                toDate.HasValue)
            {
                return
                    $"{fromDate.Value:MMMM dd} - " +
                    $"{toDate.Value:MMMM dd, yyyy}";
            }


            return "All Dates";
        }


        // =====================================================
        // STYLES
        // =====================================================

        private static IContainer HeaderStyle(
            IContainer c)
        {
            return c
                .Background(
                    QuestColors.Green.Darken2)
                .Padding(5)
                .DefaultTextStyle(x =>
                    x.FontColor(
                        QuestColors.White)
                     .Bold()
                     .FontSize(8));
        }


        private static IContainer CellStyle(
            IContainer c)
        {
            return c
                .BorderBottom(1)
                .BorderColor(
                    QuestColors.Grey.Lighten2)
                .Padding(4)
                .DefaultTextStyle(
                    x => x.FontSize(7));
        }


        private static IContainer LabelStyle(
            IContainer c)
        {
            return c
                .Padding(4)
                .DefaultTextStyle(x =>
                    x.Bold()
                     .FontSize(9));
        }


        private static IContainer ValueStyle(
            IContainer c)
        {
            return c
                .Padding(4)
                .DefaultTextStyle(
                    x => x.FontSize(9));
        }


        // =====================================================
        // DELETE TEMP FILE
        // =====================================================

        private static void DeleteFile(
            string path)
        {
            try
            {
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
            catch
            {
            }
        }
    }
}