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
            var sales =
                GetFilteredSales(
                    range,
                    fromDate,
                    toDate,
                    category);

            decimal totalSales =
                sales.Sum(x =>
                    x.SellingPrice * x.QuantitySold);

            int totalTransactions =
                sales.Count;

            var topItem =
                sales
                    .GroupBy(x => x.ItemName)
                    .Select(g => new
                    {
                        ItemName = g.Key,
                        Quantity = g.Sum(x => x.QuantitySold)
                    })
                    .OrderByDescending(x => x.Quantity)
                    .FirstOrDefault();

            var bestCategory =
                sales
                    .GroupBy(x => x.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        Total = g.Sum(x =>
                            x.SellingPrice * x.QuantitySold)
                    })
                    .OrderByDescending(x => x.Total)
                    .FirstOrDefault();

            var salesTrend =
                sales
                    .GroupBy(x => x.TransactionDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        Date = g.Key.ToString("MMM dd, yyyy"),
                        Total = g.Sum(x =>
                            x.SellingPrice * x.QuantitySold)
                    })
                    .ToList();

            var categoryPerformance =
                sales
                    .GroupBy(x => x.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        Total = g.Sum(x =>
                            x.SellingPrice * x.QuantitySold)
                    })
                    .OrderByDescending(x => x.Total)
                    .ToList();

            var topItems =
                sales
                    .GroupBy(x => x.ItemName)
                    .Select(g => new
                    {
                        ItemName = g.Key,
                        Quantity = g.Sum(x =>
                            x.QuantitySold)
                    })
                    .OrderByDescending(x => x.Quantity)
                    .Take(5)
                    .ToList();

            // =====================================================
            // SUMMARY VALUES
            // =====================================================

            int totalQuantitySold =
                sales.Sum(x => x.QuantitySold);

            decimal averageTransactionValue =
                totalTransactions > 0
                    ? totalSales / totalTransactions
                    : 0m;

            ViewBag.TotalSales =
                totalSales;

            ViewBag.TotalTransactions =
                totalTransactions;

            ViewBag.TopItem =
                topItem?.ItemName ?? "-";

            ViewBag.BestCategory =
                bestCategory?.Category ?? "-";

            ViewBag.TotalQuantitySold =
                totalQuantitySold;

            ViewBag.AverageTransactionValue =
                averageTransactionValue;

            // =====================================================
            // FILTER VALUES
            // =====================================================

            ViewBag.Range =
                range ?? "";

            ViewBag.FromDate =
                fromDate?.ToString("yyyy-MM-dd") ?? "";

            ViewBag.ToDate =
                toDate?.ToString("yyyy-MM-dd") ?? "";

            ViewBag.Category =
                string.IsNullOrWhiteSpace(category)
                    ? "All"
                    : category;

            ViewBag.CategoryText =
                string.IsNullOrWhiteSpace(category) ||
                category.Equals(
                    "All",
                    StringComparison.OrdinalIgnoreCase)
                    ? "All Categories"
                    : category;

            ViewBag.PeriodText =
                GetPeriodText(
                    range,
                    fromDate,
                    toDate);

            // =====================================================
            // JSON DATA FOR WEB CHARTS
            // =====================================================

            var options =
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null
                };

            ViewBag.SalesTrendJson =
                JsonSerializer.Serialize(
                    salesTrend,
                    options);

            ViewBag.CategoryPerformanceJson =
                JsonSerializer.Serialize(
                    categoryPerformance,
                    options);

            ViewBag.TopItemsJson =
                JsonSerializer.Serialize(
                    topItems,
                    options);

            // =====================================================
            // PREVIOUS PERIOD
            // =====================================================

            var (
                periodStart,
                periodEnd
            ) =
                GetPeriodBounds(
                    range,
                    fromDate,
                    toDate);

            bool hasPreviousPeriod =
                periodStart.HasValue &&
                periodEnd.HasValue;

            List<History> previousSales =
                new List<History>();

            if (hasPreviousPeriod)
            {
                TimeSpan duration =
                    periodEnd!.Value -
                    periodStart!.Value;

                DateTime previousStart =
                    periodStart.Value -
                    duration;

                DateTime previousEnd =
                    periodStart.Value;

                previousSales =
                    GetSalesForRange(
                        previousStart,
                        previousEnd,
                        category);
            }

            // =====================================================
            // ANALYSIS
            // =====================================================

            var salesGrowth =
                BuildGrowthAnalysis(
                    sales,
                    previousSales,
                    hasPreviousPeriod);

            var categoryAnalysis =
                BuildCategoryAnalysis(
                    sales,
                    previousSales,
                    hasPreviousPeriod);

            var itemAnalysis =
                BuildItemAnalysis(
                    sales,
                    previousSales,
                    hasPreviousPeriod);

            var trendAnalysis =
                BuildTrendAnalysis(
                    sales);

            ViewBag.SalesGrowth =
                salesGrowth;

            ViewBag.CategoryAnalysis =
                categoryAnalysis;

            ViewBag.ItemAnalysis =
                itemAnalysis;

            ViewBag.TrendAnalysis =
                trendAnalysis;

            ViewBag.GrowthInsight =
                BuildOverallGrowthInsight(
                    salesGrowth,
                    categoryAnalysis);

            ViewBag.TrendInsight =
                BuildTrendInsight(
                    sales);

            // =====================================================
            // COMPOSITION
            // =====================================================

            var composition =
                BuildCompositionAnalysis(
                    sales,
                    category);

            ViewBag.Composition =
                composition;

            ViewBag.CompositionJson =
                JsonSerializer.Serialize(
                    composition.Rows.Select(r => new
                {
                    Label = r.Label,
                    Total = r.Total,
                    Percent = r.Percent
                }),
                options);

            ViewBag.CompositionInsight =
                BuildCompositionInsight(composition);

            ViewBag.ItemInsight =
                BuildItemInsight(itemAnalysis);

            ViewBag.ItemConcentrationInsight =
                BuildItemConcentrationInsight(itemAnalysis);

            // =====================================================
            // FINAL SUMMARY
            // =====================================================

            ViewBag.FinalSummary =
                BuildFinalSummary(
                    salesGrowth,
                    itemAnalysis,
                    categoryAnalysis,
                    composition);

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
            {
                return Content(
                    "No sales records found.");
            }

            // =====================================================
            // BASIC REPORT VALUES
            // =====================================================

            decimal totalSales =
                sales.Sum(x =>
                    x.SellingPrice *
                    x.QuantitySold);

            int totalTransactions =
                sales.Count;

            int totalQuantitySold =
                sales.Sum(x =>
                    x.QuantitySold);

            decimal averageTransactionValue =
                totalTransactions > 0
                    ? totalSales / totalTransactions
                    : 0m;

            var topItem =
                sales
                    .GroupBy(x => x.ItemName)
                    .Select(g => new
                    {
                        ItemName = g.Key,
                        Quantity = g.Sum(x =>
                            x.QuantitySold)
                    })
                    .OrderByDescending(x => x.Quantity)
                    .FirstOrDefault();

            var bestCategory =
                sales
                    .GroupBy(x => x.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        Total = g.Sum(x =>
                            x.SellingPrice *
                            x.QuantitySold)
                    })
                    .OrderByDescending(x => x.Total)
                    .FirstOrDefault();

            // =====================================================
            // DAILY SUMMARY
            // =====================================================

            var dailySummary =
                sales
                    .GroupBy(x =>
                        x.TransactionDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        Date = g.Key,

                        Transactions =
                            g.Count(),

                        Quantity =
                            g.Sum(x =>
                                x.QuantitySold),

                        TotalSales =
                            g.Sum(x =>
                                x.SellingPrice *
                                x.QuantitySold)
                    })
                    .ToList();

            // =====================================================
            // CATEGORY SUMMARY
            // =====================================================

            var categorySummary =
                sales
                    .GroupBy(x => x.Category)
                    .OrderByDescending(g =>
                        g.Sum(x =>
                            x.SellingPrice *
                            x.QuantitySold))
                    .Select(g => new
                    {
                        Category = g.Key,

                        Transactions =
                            g.Count(),

                        Quantity =
                            g.Sum(x =>
                                x.QuantitySold),

                        TotalSales =
                            g.Sum(x =>
                                x.SellingPrice *
                                x.QuantitySold)
                    })
                    .ToList();

            // =====================================================
            // PERIOD / CATEGORY TEXT
            // =====================================================

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
            // PDF ANALYSIS OBJECTS
            // =====================================================

            var pdfGrowth =
                new SalesGrowthAnalysis();

            var pdfCategoryAnalysis =
                new CategoryAnalysisSummary();

            var pdfTrend =
                new TrendAnalysis();

            var pdfItemAnalysis =
                new ItemAnalysisSummary();

            bool pdfHasPreviousPeriod =
                false;

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
                        currentStart;
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
                        (currentEnd -
                         currentStart).Days + 1;

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
                        currentStart;
                }

                previousSales =
                    GetSalesForRange(
                        previousStart,
                        previousEnd,
                        category);
            }

            // =====================================================
            // SALES GROWTH
            // =====================================================

            decimal currentTotal =
                sales.Sum(x =>
                    x.SellingPrice *
                    x.QuantitySold);

            decimal previousTotal =
                previousSales.Sum(x =>
                    x.SellingPrice *
                    x.QuantitySold);

            if (previousSales.Count > 0)
            {
                pdfHasPreviousPeriod =
                    true;

                pdfGrowth.HasPreviousPeriod =
                    true;

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
                        Math.Round(
                            (pdfGrowth.ChangeAmount /
                             previousTotal) *
                            100m,
                            2);
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
            else
            {
                pdfGrowth.CurrentTotal =
                    currentTotal;

                pdfGrowth.PreviousTotal =
                    0m;

                pdfGrowth.ChangeAmount =
                    currentTotal;

                pdfGrowth.HasPreviousPeriod =
                    false;

                pdfGrowth.Direction =
                    "No Comparison";
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

                        Total =
                            g.Sum(x =>
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

                        Total =
                            g.Sum(x =>
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
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

            foreach (var categoryName in allCategories)
            {
                decimal currentCategorySales =
                    currentCategoryTotals
                        .Where(x =>
                            x.Category ==
                            categoryName)
                        .Select(x => x.Total)
                        .FirstOrDefault();

                decimal previousCategorySales =
                    previousCategoryTotals
                        .Where(x =>
                            x.Category ==
                            categoryName)
                        .Select(x => x.Total)
                        .FirstOrDefault();

                decimal changeAmount =
                    currentCategorySales -
                    previousCategorySales;

                decimal? changePercent =
                    previousCategorySales != 0
                        ? Math.Round(
                            (changeAmount /
                             previousCategorySales) *
                            100m,
                            2)
                        : null;

                pdfCategoryAnalysis.Categories.Add(
                    new CategoryAnalysisRow
                    {
                        Category =
                            categoryName,

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

            pdfCategoryAnalysis.Categories =
                pdfCategoryAnalysis.Categories
                    .OrderByDescending(
                        x => x.CurrentSales)
                    .ToList();

            var highestCategory =
                pdfCategoryAnalysis.Categories
                    .FirstOrDefault();

            if (highestCategory != null)
            {
                pdfCategoryAnalysis
                    .HighestPerformingCategory =
                    highestCategory.Category;
            }

            if (pdfHasPreviousPeriod)
            {
                var increase =
                    pdfCategoryAnalysis.Categories
                        .Where(x =>
                            x.ChangePercent.HasValue &&
                            x.ChangePercent.Value > 0)
                        .OrderByDescending(
                            x =>
                                x.ChangePercent.Value)
                        .FirstOrDefault();

                if (increase != null)
                {
                    pdfCategoryAnalysis
                        .LargestIncreaseCategory =
                        increase.Category;

                    pdfCategoryAnalysis
                        .LargestIncreasePercent =
                        increase.ChangePercent;
                }

                var decrease =
                    pdfCategoryAnalysis.Categories
                        .Where(x =>
                            x.ChangePercent.HasValue &&
                            x.ChangePercent.Value < 0)
                        .OrderBy(
                            x =>
                                x.ChangePercent.Value)
                        .FirstOrDefault();

                if (decrease != null)
                {
                    pdfCategoryAnalysis
                        .LargestDecreaseCategory =
                        decrease.Category;

                    pdfCategoryAnalysis
                        .LargestDecreasePercent =
                        decrease.ChangePercent;
                }

                pdfCategoryAnalysis.HasPreviousPeriod =
                    true;
            }

            // =====================================================
            // ITEM ANALYSIS
            // =====================================================

            pdfItemAnalysis =
                BuildItemAnalysis(
                    sales,
                    previousSales,
                    pdfHasPreviousPeriod);

            // =====================================================
            // TREND ANALYSIS
            // =====================================================

            pdfTrend =
                BuildTrendAnalysis(
                    sales);

            // =====================================================
            // COMPOSITION
            // =====================================================

            var pdfComposition =
                BuildCompositionAnalysis(
                    sales,
                    category);

            // =====================================================
            // FINAL SUMMARY
            // =====================================================

            string pdfFinalSummary =
            BuildFinalSummary(
                pdfGrowth,
                pdfItemAnalysis,
                pdfCategoryAnalysis,
                pdfComposition);


            // =====================================================
            // WRITTEN ANALYSIS
            // =====================================================

            string overallAnalysisText =
                BuildOverallGrowthInsight(
                    pdfGrowth,
                    pdfCategoryAnalysis);

            string trendAnalysisText =
                BuildTrendInsight(
                    sales);

            string compositionAnalysisText =
                BuildCompositionInsight(pdfComposition);

            string itemAnalysisText =
                BuildItemInsight(pdfItemAnalysis);

            string itemConcentrationText =
                BuildItemConcentrationInsight(pdfItemAnalysis);

            // =====================================================
            // TEMPORARY CHART FILES
            // =====================================================

            string tempFolder =
                Path.Combine(
                    Path.GetTempPath(),
                    "LostBooksCharts");

            Directory.CreateDirectory(
                tempFolder);

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
                // =====================================================
                // CREATE CHARTS
                // =====================================================

                CreateSalesTrendChart(
                    sales,
                    lineChartPath);

                CreateCompositionChart(
                    sales,
                    category,
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

                // =====================================================
                // PDF DOCUMENT
                // =====================================================

                var document =
                    Document.Create(container =>
                    {
                        // =================================================
                        // PAGE 1 — REPORT
                        // =================================================

                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);

                            page.Margin(35);

                            page.Header()
                                .Column(col =>
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

                                    // =================================================
                                    // SUMMARY
                                    // =================================================

                                    col.Item()
                                        .Text("SUMMARY")
                                        .Bold()
                                        .FontSize(13);

                                    col.Item()
                                        .Table(table =>
                                        {
                                            table.ColumnsDefinition(c =>
                                            {
                                                c.RelativeColumn(2);
                                                c.RelativeColumn(3);
                                                c.RelativeColumn(2);
                                                c.RelativeColumn(3);
                                            });

                                            table.Cell()
                                                .Element(LabelStyle)
                                                .Text("Total Sales");

                                            table.Cell()
                                                .Element(ValueStyle)
                                                .Text(
                                                    $"₱{totalSales:N2}");

                                            table.Cell()
                                                .Element(LabelStyle)
                                                .Text("Transactions");

                                            table.Cell()
                                                .Element(ValueStyle)
                                                .Text(
                                                    totalTransactions);

                                            table.Cell()
                                                .Element(LabelStyle)
                                                .Text("Total Quantity");

                                            table.Cell()
                                                .Element(ValueStyle)
                                                .Text(
                                                    totalQuantitySold);

                                            table.Cell()
                                                .Element(LabelStyle)
                                                .Text("Average Transaction");

                                            table.Cell()
                                                .Element(ValueStyle)
                                                .Text(
                                                    $"₱{averageTransactionValue:N2}");

                                            table.Cell()
                                                .Element(LabelStyle)
                                                .Text("Top Selling Item");

                                            table.Cell()
                                                .Element(ValueStyle)
                                                .Text(
                                                    topItem?.ItemName ?? "-");

                                            table.Cell()
                                                .Element(LabelStyle)
                                                .Text("Best Category");

                                            table.Cell()
                                                .Element(ValueStyle)
                                                .Text(
                                                    bestCategory?.Category ?? "-");
                                        });

                                    // =================================================
                                    // DAILY SALES SUMMARY
                                    // =================================================

                                    col.Item()
                                        .PaddingTop(6)
                                        .Text("DAILY SALES SUMMARY")
                                        .Bold()
                                        .FontSize(13);

                                    col.Item()
                                        .Table(table =>
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
                                                h.Cell()
                                                    .Element(HeaderStyle)
                                                    .Text("Date");

                                                h.Cell()
                                                    .Element(HeaderStyle)
                                                    .Text("Transactions");

                                                h.Cell()
                                                    .Element(HeaderStyle)
                                                    .Text("Quantity");

                                                h.Cell()
                                                    .Element(HeaderStyle)
                                                    .Text("Total Sales");
                                            });

                                            foreach (var item in dailySummary)
                                            {
                                                table.Cell()
                                                    .Element(CellStyle)
                                                    .Text(
                                                        item.Date.ToString(
                                                            "MMM dd, yyyy"));

                                                table.Cell()
                                                    .Element(CellStyle)
                                                    .Text(
                                                        item.Transactions);

                                                table.Cell()
                                                    .Element(CellStyle)
                                                    .Text(
                                                        item.Quantity);

                                                table.Cell()
                                                    .Element(CellStyle)
                                                    .Text(
                                                        $"₱{item.TotalSales:N2}");
                                            }
                                        });

                                    // =================================================
                                    // CATEGORY SUMMARY
                                    // =================================================

                                    col.Item()
                                        .PaddingTop(6)
                                        .Text("CATEGORY SUMMARY")
                                        .Bold()
                                        .FontSize(13);

                                    col.Item()
                                        .Table(table =>
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
                                                h.Cell()
                                                    .Element(HeaderStyle)
                                                    .Text("Category");

                                                h.Cell()
                                                    .Element(HeaderStyle)
                                                    .Text("Transactions");

                                                h.Cell()
                                                    .Element(HeaderStyle)
                                                    .Text("Quantity");

                                                h.Cell()
                                                    .Element(HeaderStyle)
                                                    .Text("Total Sales");
                                            });

                                            foreach (var item in categorySummary)
                                            {
                                                table.Cell()
                                                    .Element(CellStyle)
                                                    .Text(
                                                        item.Category);

                                                table.Cell()
                                                    .Element(CellStyle)
                                                    .Text(
                                                        item.Transactions);

                                                table.Cell()
                                                    .Element(CellStyle)
                                                    .Text(
                                                        item.Quantity);

                                                table.Cell()
                                                    .Element(CellStyle)
                                                    .Text(
                                                        $"₱{item.TotalSales:N2}");
                                            }
                                        });

                                    // =================================================
                                    // GRAND TOTAL
                                    // =================================================

                                    col.Item()
                                        .PaddingTop(12)
                                        .AlignRight()
                                        .Text(
                                            $"GRAND TOTAL SALES: ₱{totalSales:N2}")
                                        .Bold()
                                        .FontSize(14);
                                });

                            page.Footer()
                                .AlignCenter()
                                .Text(
                                    $"Generated on: {DateTime.Now:MMMM dd, yyyy h:mm tt}")
                                .FontSize(8);
                        });

                        // =================================================
                        // PAGE 2 — SALES ANALYSIS VISUALS
                        // =================================================

                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);

                            page.Margin(35);

                            page.Header()
                                .Column(col =>
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
                                .PaddingTop(12)
                                .Column(col =>
                                {
                                    col.Spacing(12);

                                    // =================================================
                                    // SALES TREND
                                    // =================================================

                                    col.Item()
                                        .Border(1)
                                        .BorderColor(
                                            QuestColors.Grey.Lighten2)
                                        .Column(chart =>
                                        {
                                            chart.Item()
                                                .Background(
                                                    QuestColors.Green.Darken2)
                                                .Padding(6)
                                                .Text("SALES TREND")
                                                .Bold()
                                                .FontColor(
                                                    QuestColors.White)
                                                .FontSize(10);

                                            chart.Item()
                                                .Padding(6)
                                                .Height(180)
                                                .Image(lineBytes)
                                                .FitArea();

                                            chart.Item()
                                                .Background(
                                                    QuestColors.Green.Lighten5)
                                                .BorderTop(2)
                                                .BorderColor(
                                                    QuestColors.Green.Darken2)
                                                .Padding(8)
                                                .Column(text =>
                                                {
                                                    text.Item()
                                                        .Text("Analysis")
                                                        .Bold()
                                                        .FontColor(
                                                            QuestColors.Green.Darken2)
                                                        .FontSize(9);

                                                    text.Item()
                                                        .PaddingTop(3)
                                                        .Text(
                                                            trendAnalysisText)
                                                        .FontSize(8);
                                                });
                                        });

                                    // =================================================
                                    // CATEGORY / COMPOSITION
                                    // =================================================

                                    col.Item()
                                        .Border(1)
                                        .BorderColor(
                                            QuestColors.Grey.Lighten2)
                                        .Column(chart =>
                                        {
                                            chart.Item()
                                                .Background(
                                                    QuestColors.Green.Darken2)
                                                .Padding(6)
                                                .Text(
                                                    pdfComposition.ChartTitle)
                                                .Bold()
                                                .FontColor(
                                                    QuestColors.White)
                                                .FontSize(10);

                                            chart.Item()
                                                .Padding(6)
                                                .Height(180)
                                                .Image(pieBytes)
                                                .FitArea();

                                            chart.Item()
                                                .Background(
                                                    QuestColors.Green.Lighten5)
                                                .BorderTop(2)
                                                .BorderColor(
                                                    QuestColors.Green.Darken2)
                                                .Padding(8)
                                                .Column(text =>
                                                {
                                                    text.Item()
                                                        .Text("Analysis")
                                                        .Bold()
                                                        .FontColor(
                                                            QuestColors.Green.Darken2)
                                                        .FontSize(9);

                                                    text.Item()
                                                        .PaddingTop(3)
                                                        .Text(
                                                            compositionAnalysisText)
                                                        .FontSize(8);
                                                });
                                        });

                                    // =================================================
                                    // TOP SELLING ITEMS
                                    // =================================================

                                    col.Item()
                                        .Border(1)
                                        .BorderColor(
                                            QuestColors.Grey.Lighten2)
                                        .Column(chart =>
                                        {
                                            chart.Item()
                                                .Background(
                                                    QuestColors.Green.Darken2)
                                                .Padding(6)
                                                .Text(
                                                    "TOP SELLING ITEMS")
                                                .Bold()
                                                .FontColor(
                                                    QuestColors.White)
                                                .FontSize(10);

                                            chart.Item()
                                                .Padding(6)
                                                .Height(180)
                                                .Image(barBytes)
                                                .FitArea();

                                            chart.Item()
                                                .Background(
                                                    QuestColors.Green.Lighten5)
                                                .BorderTop(2)
                                                .BorderColor(
                                                    QuestColors.Green.Darken2)
                                                .Padding(8)
                                                .Column(text =>
                                                {
                                                    text.Item()
                                                        .Text("Analysis")
                                                        .Bold()
                                                        .FontColor(
                                                            QuestColors.Green.Darken2)
                                                        .FontSize(9);

                                                    text.Item()
                                                        .PaddingTop(3)
                                                        .Text(
                                                            itemAnalysisText)
                                                        .FontSize(8);
                                                });
                                        });
                                });

                            page.Footer()
                                .AlignCenter()
                                .Text(
                                    $"Generated on: {DateTime.Now:MMMM dd, yyyy h:mm tt}")
                                .FontSize(8);
                        });

                        // =================================================
                        // PAGE 3 — ITEM PERFORMANCE + FINAL SUMMARY
                        // =================================================

                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);

                            page.Margin(35);

                            page.Header()
                                .Column(col =>
                                {
                                    col.Item()
                                        .AlignCenter()
                                        .Text("LOST BOOKS CEBU")
                                        .Bold()
                                        .FontSize(20);

                                    col.Item()
                                        .AlignCenter()
                                        .Text("ITEM PERFORMANCE")
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
                                .PaddingTop(18)
                                .Column(col =>
                                {
                                    col.Spacing(12);

                                    // =================================================
                                    // ITEM PERFORMANCE TABLE
                                    // =================================================

                                    col.Item()
                                        .Text(
                                            "ITEM PERFORMANCE TABLE")
                                        .Bold()
                                        .FontSize(13);

                                    col.Item()
                                        .Table(table =>
                                        {
                                            table.ColumnsDefinition(c =>
                                            {
                                                c.RelativeColumn(1);
                                                c.RelativeColumn(4);
                                                c.RelativeColumn(2.5f);
                                                c.RelativeColumn(1.5f);
                                                c.RelativeColumn(2.5f);
                                                c.RelativeColumn(2);
                                            });

                                            table.Header(h =>
                                            {
                                                h.Cell()
                                                    .Element(HeaderStyle)
                                                    .Text("Rank");

                                                h.Cell()
                                                    .Element(HeaderStyle)
                                                    .Text("Item");

                                                h.Cell()
                                                    .Element(HeaderStyle)
                                                    .Text("Category");

                                                h.Cell()
                                                    .Element(HeaderStyle)
                                                    .Text("Qty");

                                                h.Cell()
                                                    .Element(HeaderStyle)
                                                    .Text("Sales");

                                                h.Cell()
                                                    .Element(HeaderStyle)
                                                    .Text("Change%");
                                            });

                                            int rank = 1;

                                            foreach (
                                                var item
                                                in pdfItemAnalysis.AllItems)
                                            {
                                                table.Cell()
                                                    .Element(CellStyle)
                                                    .Text(rank);

                                                table.Cell()
                                                    .Element(CellStyle)
                                                    .Text(
                                                        item.ItemName);

                                                table.Cell()
                                                    .Element(CellStyle)
                                                    .Text(
                                                        item.Category);

                                                table.Cell()
                                                    .Element(CellStyle)
                                                    .Text(
                                                        item.CurrentQuantity);

                                                table.Cell()
                                                    .Element(CellStyle)
                                                    .Text(
                                                        $"₱{item.CurrentSales:N2}");

                                                string changeText =
                                                    item.QuantityChangePercent.HasValue
                                                        ? $"{item.QuantityChangePercent.Value:+0.00;-0.00;0.00}%"
                                                        : "N/A";

                                                table.Cell()
                                                    .Element(CellStyle)
                                                    .Text(
                                                        changeText);

                                                rank++;
                                            }
                                        });

                                    col.Item()
                                        .PaddingTop(6)
                                        .Background(QuestColors.Green.Lighten5)
                                        .BorderLeft(3)
                                        .BorderColor(QuestColors.Green.Darken2)
                                        .Padding(8)
                                        .Text(itemConcentrationText)
                                        .FontSize(8);

                                    // =================================================
                                    // FINAL ANALYSIS SUMMARY
                                    // =================================================

                                    col.Item()
                                        .PaddingTop(15)
                                        .Text(
                                            "FINAL ANALYSIS SUMMARY")
                                        .Bold()
                                        .FontSize(13);

                                    col.Item()
                                        .LineHorizontal(1);

                                    col.Item()
                                        .PaddingTop(6)
                                        .Background(
                                            QuestColors.Green.Lighten5)
                                        .BorderLeft(3)
                                        .BorderColor(
                                            QuestColors.Green.Darken2)
                                        .Padding(10)
                                        .Text(
                                            pdfFinalSummary)
                                        .FontSize(9);
                                });

                            page.Footer()
                                .AlignCenter()
                                .Text(
                                    $"Generated on: {DateTime.Now:MMMM dd, yyyy h:mm tt}")
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
                    .GroupBy(x =>
                        x.TransactionDate.Date)
                    .OrderBy(x => x.Key)
                    .ToList();

            double[] xs =
                Enumerable
                    .Range(
                        0,
                        grouped.Count)
                    .Select(x =>
                        (double)x)
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
                    .Select(g =>
                        g.Key.ToString("MMM dd"))
                    .ToArray();

            var plot =
                new Plot();

            if (ys.Length > 0)
            {
                var scatter =
                    plot.Add.Scatter(
                        xs,
                        ys);

                scatter.LineWidth =
                    3;

                scatter.MarkerSize =
                    7;

                scatter.Color =
                    ScottPlot.Color.FromHex(
                        "#198754");

                plot.Axes.Bottom.SetTicks(
                    xs,
                    labels);
            }

            plot.Title(
                "Sales Trend");

            plot.XLabel(
                "Date");

            plot.YLabel(
                "Sales (₱)");

            plot.SavePng(
                path,
                1000,
                450);
        }

        // =====================================================
        // COMPOSITION PIE CHART
        // =====================================================

        private static void CreateCompositionChart(
            List<History> sales,
            string? category,
            string path)
        {
            bool isAll =
                string.IsNullOrWhiteSpace(category) ||
                category.Equals(
                    "All",
                    StringComparison.OrdinalIgnoreCase);

            var grouped =
                isAll
                    ? sales
                        .GroupBy(x => x.Category)
                        .Select(g => new
                        {
                            Label =
                                string.IsNullOrWhiteSpace(
                                    g.Key)
                                    ? "Unknown"
                                    : g.Key,

                            Total =
                                g.Sum(x =>
                                    x.SellingPrice *
                                    x.QuantitySold)
                        })
                        .OrderByDescending(
                            x => x.Total)
                        .ToList()
                    : sales
                        .GroupBy(x => x.ItemName)
                        .Select(g => new
                        {
                            Label =
                                string.IsNullOrWhiteSpace(
                                    g.Key)
                                    ? "Unknown"
                                    : g.Key,

                            Total =
                                g.Sum(x =>
                                    x.SellingPrice *
                                    x.QuantitySold)
                        })
                        .OrderByDescending(
                            x => x.Total)
                        .ToList();

            string[] palette =
            {
                "#198754",
                "#ffc107",
                "#0d6efd",
                "#dc3545",
                "#6f42c1",
                "#20c997"
            };

            var plot =
                new Plot();

            if (grouped.Count > 0)
            {
                var slices =
                    new List<PieSlice>();

                for (
                    int i = 0;
                    i < grouped.Count;
                    i++)
                {
                    slices.Add(
                        new PieSlice
                        {
                            Value =
                                (double)
                                grouped[i].Total,

                            Label =
                                grouped[i].Label,

                            FillColor =
                                ScottPlot.Color.FromHex(
                                    palette[
                                        i %
                                        palette.Length])
                        });
                }

                plot.Add.Pie(
                    slices);

                plot.ShowLegend();
            }

            plot.Title(
                isAll
                    ? "Category Performance"
                    : "Sales Composition");

            plot.SavePng(
                path,
                900,
                450);
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
                            string.IsNullOrWhiteSpace(
                                g.Key)
                                ? "Unknown"
                                : g.Key,

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
                    .Range(
                        0,
                        grouped.Count)
                    .Select(x =>
                        (double)x)
                    .ToArray();

            string[] labels =
                grouped
                    .Select(x =>
                        x.ItemName)
                    .ToArray();

            var plot =
                new Plot();

            if (values.Length > 0)
            {
                var bars =
                    plot.Add.Bars(
                        positions,
                        values);

                foreach (var bar in bars.Bars)
                {
                    bar.FillColor =
                        ScottPlot.Color.FromHex(
                            "#198754");
                }

                plot.Axes.Bottom.SetTicks(
                    positions,
                    labels);
            }

            plot.Title(
                "Top Selling Items");

            plot.XLabel(
                "Items");

            plot.YLabel(
                "Quantity Sold");

            plot.SavePng(
                path,
                1000,
                450);
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

            if (
                range == "daily" &&
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
            else if (
                range == "weekly" &&
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
            else if (
                range == "monthly" &&
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
            else if (
                range == "custom" &&
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

            if (
                !string.IsNullOrWhiteSpace(category) &&
                !category.Equals(
                    "All",
                    StringComparison.OrdinalIgnoreCase))
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
            DateTime? End)
            GetPeriodBounds(
                string? range,
                DateTime? fromDate,
                DateTime? toDate)
        {
            if (
                range == "daily" &&
                fromDate.HasValue)
            {
                DateTime start =
                    fromDate.Value.Date;

                return (
                    start,
                    start.AddDays(1));
            }

            if (
                range == "weekly" &&
                fromDate.HasValue)
            {
                DateTime start =
                    fromDate.Value.Date;

                return (
                    start,
                    start.AddDays(7));
            }

            if (
                range == "monthly" &&
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

            if (
                range == "custom" &&
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

            if (
                start.HasValue &&
                end.HasValue)
            {
                query =
                    query.Where(x =>
                        x.TransactionDate >=
                            start.Value &&
                        x.TransactionDate <
                            end.Value);
            }

            if (
                !string.IsNullOrWhiteSpace(category) &&
                !category.Equals(
                    "All",
                    StringComparison.OrdinalIgnoreCase))
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
        // A. SALES GROWTH ANALYSIS
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
                         previousTotal) *
                        100m,
                        2)
                    : null;

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
        // A2. OVERALL GROWTH INSIGHT
        // =====================================================

        private static string BuildOverallGrowthInsight(
            SalesGrowthAnalysis growth,
            CategoryAnalysisSummary catAnalysis)
        {
            if (!growth.HasPreviousPeriod)
            {
                return
                    $"The current report for the selected period shows " +
                    $"₱{growth.CurrentTotal:N2} in total sales. " +
                    "No previous equivalent period is available for comparison.";
            }

            string direction =
                growth.ChangeAmount > 0
                    ? "increased"
                    : growth.ChangeAmount < 0
                        ? "decreased"
                        : "remained unchanged";

            string overallSentence =
                $"Sales {direction} from " +
                $"₱{growth.PreviousTotal:N2} to " +
                $"₱{growth.CurrentTotal:N2}, " +
                $"a change of " +
                $"₱{Math.Abs(growth.ChangeAmount):N2}" +
                (
                    growth.ChangePercent.HasValue
                        ? $" or {Math.Abs(growth.ChangePercent.Value):N2}%."
                        : "."
                );

            if (
                growth.ChangeAmount == 0 ||
                catAnalysis.Categories.Count == 0)
            {
                return overallSentence;
            }

            CategoryAnalysisRow? driver =
                growth.ChangeAmount > 0
                    ? catAnalysis.Categories
                        .Where(c =>
                            c.ChangeAmount > 0)
                        .OrderByDescending(
                            c =>
                                c.ChangeAmount)
                        .FirstOrDefault()
                    : catAnalysis.Categories
                        .Where(c =>
                            c.ChangeAmount < 0)
                        .OrderBy(
                            c =>
                                c.ChangeAmount)
                        .FirstOrDefault();

            if (driver == null)
            {
                return
                    overallSentence +
                    " The available data does not identify a specific category driving this change.";
            }

            decimal driverContributionPercent =
                growth.ChangeAmount != 0
                    ? Math.Round(
                        Math.Abs(
                            driver.ChangeAmount /
                            growth.ChangeAmount) *
                        100m,
                        2)
                    : 0m;

            string driverSentence =
                $"{driver.Category} sales moved from " +
                $"₱{driver.PreviousSales:N2} to " +
                $"₱{driver.CurrentSales:N2}, " +
                $"a change of " +
                $"₱{Math.Abs(driver.ChangeAmount):N2}" +
                (
                    driver.ChangePercent.HasValue
                        ? $" or {Math.Abs(driver.ChangePercent.Value):N2}%, "
                        : ", "
                ) +
                $"accounting for roughly " +
                $"{driverContributionPercent:N2}% of the overall " +
                $"{(growth.ChangeAmount > 0 ? "increase" : "decrease")}, " +
                "making it the strongest contributor to this period's change.";

            return
                overallSentence +
                " " +
                driverSentence;
        }

        // =====================================================
        // B. CATEGORY PERFORMANCE
        // =====================================================

        private static CategoryAnalysisSummary BuildCategoryAnalysis(
            List<History> currentSales,
            List<History> previousSales,
            bool hasPreviousPeriod)
        {
            var currentTotals =
                currentSales
                    .GroupBy(x =>
                        x.Category)
                    .Select(g => new
                    {
                        Category =
                            g.Key,

                        Total =
                            g.Sum(x =>
                                x.SellingPrice *
                                x.QuantitySold)
                    })
                    .ToList();

            var previousTotals =
                previousSales
                    .GroupBy(x =>
                        x.Category)
                    .Select(g => new
                    {
                        Category =
                            g.Key,

                        Total =
                            g.Sum(x =>
                                x.SellingPrice *
                                x.QuantitySold)
                    })
                    .ToList();

            var categoryNames =
                currentTotals
                    .Select(x =>
                        x.Category)
                    .Union(
                        previousTotals
                            .Select(x =>
                                x.Category))
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

            var rows =
                new List<CategoryAnalysisRow>();

            foreach (
                var cat
                in categoryNames)
            {
                decimal current =
                    currentTotals
                        .FirstOrDefault(
                            x =>
                                x.Category == cat)
                        ?.Total ?? 0m;

                decimal previous =
                    previousTotals
                        .FirstOrDefault(
                            x =>
                                x.Category == cat)
                        ?.Total ?? 0m;

                decimal changeAmount =
                    current -
                    previous;

                decimal? changePercent =
                    previous != 0
                        ? Math.Round(
                            (changeAmount /
                             previous) *
                            100m,
                            2)
                        : null;

                rows.Add(
                    new CategoryAnalysisRow
                    {
                        Category =
                            cat,

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
                        x =>
                            x.CurrentSales)
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
                        x =>
                            x.CurrentSales)
                    .FirstOrDefault();

            summary.HighestPerformingCategory =
                highestPerforming?.Category;

            if (hasPreviousPeriod)
            {
                var largestIncrease =
                    rows
                        .Where(x =>
                            x.ChangePercent.HasValue &&
                            x.ChangePercent.Value > 0)
                        .OrderByDescending(
                            x =>
                                x.ChangePercent.Value)
                        .FirstOrDefault();

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
                            x.ChangePercent.HasValue &&
                            x.ChangePercent.Value < 0)
                        .OrderBy(
                            x =>
                                x.ChangePercent.Value)
                        .FirstOrDefault();

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
        // C. ITEM PERFORMANCE
        // =====================================================

        private static ItemAnalysisSummary BuildItemAnalysis(
            List<History> currentSales,
            List<History> previousSales,
            bool hasPreviousPeriod)
        {
            var currentItems =
                currentSales
                    .GroupBy(x => new
                    {
                        x.ItemID,
                        x.ItemName,
                        x.Category
                    })
                    .Select(g => new
                    {
                        g.Key.ItemID,
                        g.Key.ItemName,
                        g.Key.Category,

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
                    .GroupBy(x => new
                    {
                        x.ItemID,
                        x.ItemName,
                        x.Category
                    })
                    .Select(g => new
                    {
                        g.Key.ItemID,
                        g.Key.ItemName,
                        g.Key.Category,

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
                                    (
                                        (decimal)(
                                            c.Quantity -
                                            previousQuantity)
                                        /
                                        previousQuantity
                                    ) *
                                    100m,
                                    2)
                                : null;

                        decimal? revenueChangePercent =
                            previousSalesTotal != 0
                                ? Math.Round(
                                    (
                                        (
                                            c.Sales -
                                            previousSalesTotal
                                        )
                                        /
                                        previousSalesTotal
                                    ) *
                                    100m,
                                    2)
                                : null;

                        return new ItemAnalysisRow
                        {
                            ItemID =
                                c.ItemID,

                            ItemName =
                                c.ItemName,

                            Category =
                                c.Category,

                            CurrentQuantity =
                                c.Quantity,

                            CurrentSales =
                                c.Sales,

                            PreviousQuantity =
                                previousQuantity,

                            PreviousSales =
                                previousSalesTotal,

                            QuantityChangePercent =
                                quantityChangePercent,

                            RevenueChangePercent =
                                revenueChangePercent
                        };
                    })
                    .OrderByDescending(
                        x =>
                            x.CurrentQuantity)
                    .ToList();

            var summary =
                new ItemAnalysisSummary
                {
                    HasPreviousPeriod =
                        hasPreviousPeriod,

                    AllItems =
                        rows,

                    HighestQuantityItem =
                        rows
                            .OrderByDescending(
                                x =>
                                    x.CurrentQuantity)
                            .FirstOrDefault(),

                    HighestSalesItem =
                        rows
                            .OrderByDescending(
                                x =>
                                    x.CurrentSales)
                            .FirstOrDefault()
                };

            if (hasPreviousPeriod)
            {
                summary.IncreasedItems =
                    rows
                        .Where(x =>
                            x.QuantityChangePercent.HasValue &&
                            x.QuantityChangePercent.Value > 0)
                        .OrderByDescending(
                            x =>
                                x.QuantityChangePercent.Value)
                        .Take(3)
                        .ToList();

                summary.DecreasedItems =
                    rows
                        .Where(x =>
                            x.QuantityChangePercent.HasValue &&
                            x.QuantityChangePercent.Value < 0)
                        .OrderBy(
                            x =>
                                x.QuantityChangePercent.Value)
                        .Take(3)
                        .ToList();
            }

            return summary;
        }

        // =====================================================
        // C2. ITEM PERFORMANCE INSIGHT
        // =====================================================

        private static string BuildItemInsight(ItemAnalysisSummary itemAnalysis)
        {
            if (itemAnalysis.HighestQuantityItem == null)
            {
                return "No item data available for the selected period.";
            }

            var parts = new List<string>();

            if (itemAnalysis.HighestSalesItem != null &&
                itemAnalysis.HighestQuantityItem.ItemID != itemAnalysis.HighestSalesItem.ItemID)
            {
                var volumeLeader = itemAnalysis.HighestQuantityItem;
                var revenueLeader = itemAnalysis.HighestSalesItem;

                decimal revenueDiff = revenueLeader.CurrentSales - volumeLeader.CurrentSales;
                int unitDiff = volumeLeader.CurrentQuantity - revenueLeader.CurrentQuantity;

                decimal volumeLeaderAvgPrice = volumeLeader.CurrentQuantity > 0
                    ? volumeLeader.CurrentSales / volumeLeader.CurrentQuantity
                    : 0;

                decimal revenueLeaderAvgPrice = revenueLeader.CurrentQuantity > 0
                    ? revenueLeader.CurrentSales / revenueLeader.CurrentQuantity
                    : 0;

                parts.Add(
                    $"{volumeLeader.ItemName} sold in the greatest volume at {volumeLeader.CurrentQuantity} unit(s), " +
                    $"but {revenueLeader.ItemName} generated more revenue overall at ₱{revenueLeader.CurrentSales:N2} " +
                    $"(₱{revenueDiff:N2} more) despite selling {Math.Max(0, unitDiff)} fewer unit(s)" +
                    (revenueLeaderAvgPrice > volumeLeaderAvgPrice
                        ? $" — averaging about ₱{revenueLeaderAvgPrice:N2} per unit versus ₱{volumeLeaderAvgPrice:N2}."
                        : "."));
            }
            else
            {
                parts.Add(
                    $"{itemAnalysis.HighestQuantityItem.ItemName} led in both quantity sold " +
                    $"({itemAnalysis.HighestQuantityItem.CurrentQuantity} unit(s)) and revenue " +
                    $"(₱{itemAnalysis.HighestQuantityItem.CurrentSales:N2}), with no other item close on either metric.");
            }

            if (itemAnalysis.HasPreviousPeriod)
            {
                var biggestGain = itemAnalysis.IncreasedItems.FirstOrDefault();
                var biggestDrop = itemAnalysis.DecreasedItems.FirstOrDefault();

                if (biggestGain != null && biggestDrop != null && biggestGain.ItemID != biggestDrop.ItemID)
                {
                    parts.Add(
                        $"{biggestGain.ItemName} saw the largest jump in demand " +
                        $"(+{biggestGain.QuantityChangePercent!.Value:N2}%), while {biggestDrop.ItemName} pulled back the most " +
                        $"({biggestDrop.QuantityChangePercent!.Value:N2}%) compared with the previous period.");
                }
                else if (biggestGain != null)
                {
                    parts.Add(
                        $"{biggestGain.ItemName} recorded the largest increase in demand at " +
                        $"+{biggestGain.QuantityChangePercent!.Value:N2}% compared with the previous period.");
                }
                else if (biggestDrop != null)
                {
                    parts.Add(
                        $"{biggestDrop.ItemName} recorded the largest decline in demand at " +
                        $"{biggestDrop.QuantityChangePercent!.Value:N2}% compared with the previous period.");
                }
            }

            return string.Join(" ", parts);
        }

        // =====================================================
        // C3. ITEM CONCENTRATION INSIGHT (for the full Item Performance table)
        // =====================================================

        private static string BuildItemConcentrationInsight(ItemAnalysisSummary itemAnalysis)
        {
            if (itemAnalysis.AllItems == null || itemAnalysis.AllItems.Count == 0)
            {
                return "No item data available for the selected period.";
            }

            var byRevenue = itemAnalysis.AllItems
                .OrderByDescending(i => i.CurrentSales)
                .ToList();

            decimal grandTotal = byRevenue.Sum(i => i.CurrentSales);

            var parts = new List<string>();

            if (grandTotal > 0)
            {
                int topCount = Math.Min(3, byRevenue.Count);
                var topItems = byRevenue.Take(topCount).ToList();
                decimal topTotal = topItems.Sum(i => i.CurrentSales);
                decimal topShare = Math.Round((topTotal / grandTotal) * 100m, 2);

                string itemNames = string.Join(", ", topItems.Select(i => i.ItemName));

                if (byRevenue.Count > topCount)
                {
                    int remaining = byRevenue.Count - topCount;
                    decimal remainingShare = Math.Max(0, 100m - topShare);

                    parts.Add(
                        $"The top {topCount} item(s) by revenue — {itemNames} — accounted for ₱{topTotal:N2}, " +
                        $"or {topShare:N2}% of total sales across all {byRevenue.Count} items, leaving the remaining " +
                        $"{remaining} item(s) to share just {remainingShare:N2}%. " +
                        (topShare >= 50m
                            ? "This indicates item-level sales were heavily concentrated in a small number of products."
                            : "This indicates sales were relatively distributed across the item catalog."));
                }
                else
                {
                    parts.Add(
                        $"{itemNames} — the only {byRevenue.Count} item(s) in this period — together generated " +
                        $"₱{topTotal:N2} in total sales.");
                }
            }

            if (itemAnalysis.HasPreviousPeriod)
            {
                var biggestGain = itemAnalysis.IncreasedItems.FirstOrDefault();
                var biggestDrop = itemAnalysis.DecreasedItems.FirstOrDefault();

                if (biggestGain != null && biggestDrop != null && biggestGain.ItemID != biggestDrop.ItemID)
                {
                    parts.Add(
                        $"{biggestGain.ItemName} saw the largest jump in demand " +
                        $"(+{biggestGain.QuantityChangePercent!.Value:N2}%), while {biggestDrop.ItemName} pulled back the most " +
                        $"({biggestDrop.QuantityChangePercent!.Value:N2}%) compared with the previous period.");
                }
                else if (biggestGain != null)
                {
                    parts.Add(
                        $"{biggestGain.ItemName} recorded the largest increase in demand at " +
                        $"+{biggestGain.QuantityChangePercent!.Value:N2}% compared with the previous period.");
                }
                else if (biggestDrop != null)
                {
                    parts.Add(
                        $"{biggestDrop.ItemName} recorded the largest decline in demand at " +
                        $"{biggestDrop.QuantityChangePercent!.Value:N2}% compared with the previous period.");
                }
            }

            return string.Join(" ", parts);
        }

        // =====================================================
        // D. SALES TREND
        // =====================================================

        private static TrendAnalysis BuildTrendAnalysis(
            List<History> currentSales)
        {
            var dailyTotals =
                currentSales
                    .GroupBy(
                        x =>
                            x.TransactionDate.Date)
                    .OrderBy(
                        g =>
                            g.Key)
                    .Select(g => new
                    {
                        Date =
                            g.Key,

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
                    HasData =
                        false
                };
            }

            var highest =
                dailyTotals
                    .OrderByDescending(
                        x =>
                            x.Total)
                    .First();

            var lowest =
                dailyTotals
                    .OrderBy(
                        x =>
                            x.Total)
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
                         beginningTotal) *
                        100m,
                        2)
                    : null;

            return new TrendAnalysis
            {
                HasData =
                    true,

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
        // D2. TREND INSIGHT
        // =====================================================

        private static string BuildTrendInsight(
            List<History> sales)
        {
            var dailyTotals =
                sales
                    .GroupBy(
                        x =>
                            x.TransactionDate.Date)
                    .Select(g => new
                    {
                        Date =
                            g.Key,

                        Total =
                            g.Sum(x =>
                                x.SellingPrice *
                                x.QuantitySold)
                    })
                    .OrderBy(
                        x =>
                            x.Date)
                    .ToList();

            if (dailyTotals.Count == 0)
            {
                return
                    "No sales trend data available for the selected period.";
            }

            var highest =
                dailyTotals
                    .OrderByDescending(
                        x =>
                            x.Total)
                    .First();

            var lowest =
                dailyTotals
                    .OrderBy(
                        x =>
                            x.Total)
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
                         beginningTotal) *
                        100m,
                        2)
                    : null;

            var parts =
                new List<string>();

            parts.Add(
    $"Sales reached their highest point on " +
    $"{highest.Date:MMMM dd, yyyy} at " +
    $"₱{highest.Total:N2}, while the lowest sales were " +
    $"recorded on {lowest.Date:MMMM dd, yyyy} at " +
    $"₱{lowest.Total:N2}.");

            // =================================================
            // PEAK DAY CONTRIBUTING FACTOR
            // =================================================

            var highestDaySales =
                sales
                    .Where(x =>
                        x.TransactionDate.Date == highest.Date)
                    .ToList();

            decimal highestDayTotal =
                highestDaySales.Sum(x =>
                    x.SellingPrice * x.QuantitySold);

            if (highestDayTotal > 0)
            {
                var topCategoryOnPeakDay =
                    highestDaySales
                        .GroupBy(x => x.Category)
                        .Select(g => new
                        {
                            Category =
                                string.IsNullOrWhiteSpace(g.Key)
                                    ? "Unknown"
                                    : g.Key,

                            Total =
                                g.Sum(x =>
                                    x.SellingPrice * x.QuantitySold)
                        })
                        .OrderByDescending(x => x.Total)
                        .FirstOrDefault();

                if (topCategoryOnPeakDay != null)
                {
                    decimal peakDayCategoryShare =
                        Math.Round(
                            (topCategoryOnPeakDay.Total / highestDayTotal) * 100m,
                            2);

                    parts.Add(
                        $"{topCategoryOnPeakDay.Category} accounted for " +
                        $"{peakDayCategoryShare:N2}% of sales on the single " +
                        $"highest-selling day ({highest.Date:MMMM dd, yyyy}), " +
                        "making it the primary contributor to that peak.");
                }
            }

            if (
                changePercent.HasValue &&
                dailyTotals.Count > 1)
            {
                string direction =
                    changeAmount > 0
                        ? "increased"
                        : changeAmount < 0
                            ? "decreased"
                            : "remained unchanged";

                parts.Add(
                    $"Between the beginning " +
                    $"({dailyTotals.First().Date:MMMM dd}) " +
                    $"and end " +
                    $"({dailyTotals.Last().Date:MMMM dd}) " +
                    $"of the selected period, sales {direction} by " +
                    $"{Math.Abs(changePercent.Value):N2}%.");
            }

            // =================================================
            // SALES CONCENTRATION
            // =================================================

            decimal grandTotal =
                dailyTotals.Sum(
                    d =>
                        d.Total);

            if (
                dailyTotals.Count >= 3 &&
                grandTotal > 0)
            {
                int topCount =
                    Math.Min(
                        3,
                        dailyTotals.Count);

                var topDays =
                    dailyTotals
                        .OrderByDescending(
                            d =>
                                d.Total)
                        .Take(
                            topCount)
                        .ToList();

                decimal topShare =
                    Math.Round(
                        (
                            topDays.Sum(
                                d =>
                                    d.Total)
                            /
                            grandTotal
                        ) *
                        100m,
                        2);

                if (topShare >= 50m)
                {
                    parts.Add(
                        $"The {topCount} highest-selling day(s) alone " +
                        $"accounted for {topShare:N2}% of total sales " +
                        "during the selected period, indicating that " +
                        "sales performance was concentrated around a " +
                        "small number of days rather than spread evenly " +
                        "throughout the period.");
                }
            }

            parts.Add(
                "The available data shows when these changes occurred " +
                "but does not establish what caused them.");

            return string.Join(
                " ",
                parts);
        }

        // =====================================================
        // E. SALES COMPOSITION
        // =====================================================

        private static CompositionSummary BuildCompositionAnalysis(
            List<History> sales,
            string? category)
        {
            bool isAll =
                string.IsNullOrWhiteSpace(category) ||
                category.Equals(
                    "All",
                    StringComparison.OrdinalIgnoreCase);

            decimal grandTotal =
                sales.Sum(x =>
                    x.SellingPrice *
                    x.QuantitySold);

            var summary =
                new CompositionSummary
                {
                    IsSingleCategory =
                        !isAll,

                    ChartTitle =
                        isAll
                            ? "Category Performance"
                            : "Sales Composition"
                };

            if (isAll)
            {
                var groups =
                    sales
                        .GroupBy(
                            x =>
                                x.Category)
                        .Select(g => new
                        {
                            Label =
                                string.IsNullOrWhiteSpace(
                                    g.Key)
                                    ? "Unknown"
                                    : g.Key,

                            Total =
                                g.Sum(x =>
                                    x.SellingPrice *
                                    x.QuantitySold)
                        })
                        .OrderByDescending(
                            x =>
                                x.Total);

                foreach (var g in groups)
                {
                    summary.Rows.Add(
                        new CompositionRow
                        {
                            Label =
                                g.Label,

                            Total =
                                g.Total,

                            Percent =
                                grandTotal != 0
                                    ? Math.Round(
                                        (
                                            g.Total /
                                            grandTotal
                                        ) *
                                        100m,
                                        2)
                                    : 0m
                        });
                }
            }
            else
            {
                var groups =
                    sales
                        .GroupBy(
                            x =>
                                x.ItemName)
                        .Select(g => new
                        {
                            Label =
                                string.IsNullOrWhiteSpace(
                                    g.Key)
                                    ? "Unknown"
                                    : g.Key,

                            Total =
                                g.Sum(x =>
                                    x.SellingPrice *
                                    x.QuantitySold)
                        })
                        .OrderByDescending(
                            x =>
                                x.Total);

                foreach (var g in groups)
                {
                    summary.Rows.Add(
                        new CompositionRow
                        {
                            Label =
                                g.Label,

                            Total =
                                g.Total,

                            Percent =
                                grandTotal != 0
                                    ? Math.Round(
                                        (
                                            g.Total /
                                            grandTotal
                                        ) *
                                        100m,
                                        2)
                                    : 0m
                        });
                }
            }

            return summary;
        }

        // =====================================================
        // E2. SALES COMPOSITION INSIGHT
        // =====================================================

        private static string BuildCompositionInsight(CompositionSummary composition)
        {
            if (composition.Rows.Count == 0)
            {
                return "No composition data available for the selected period.";
            }

            string noun = composition.IsSingleCategory
                ? "sales within the selected category"
                : "total sales";

            string itemWord = composition.IsSingleCategory ? "item" : "category";

            var rows = composition.Rows;
            var top = rows[0];

            string leadSentence;

            if (rows.Count == 1)
            {
                leadSentence =
                    $"{top.Label} accounted for all {noun} recorded during the selected period.";
            }
            else
            {
                var second = rows[1];
                decimal gap = top.Percent - second.Percent;

                leadSentence =
                    $"{top.Label} led with {top.Percent:N2}% of {noun}, " +
                    (gap >= 0.01m
                        ? $"ahead of {second.Label} by {gap:N2} percentage points ({second.Percent:N2}%)."
                        : $"closely followed by {second.Label} at {second.Percent:N2}%.");
            }

            int topCount = Math.Min(3, rows.Count);
            decimal topShare = rows.Take(topCount).Sum(r => r.Percent);

            string concentrationSentence = "";

            if (rows.Count > topCount)
            {
                int remaining = rows.Count - topCount;

                concentrationSentence =
                    $" The top {topCount} {(topCount == 1 ? itemWord : itemWord + "s")} made up " +
                    $"{topShare:N2}% of {noun}, while the remaining {remaining} " +
                    $"{(remaining == 1 ? itemWord : itemWord + "s")} shared the other " +
                    $"{Math.Max(0, 100m - topShare):N2}%.";
            }

            return leadSentence + concentrationSentence;
        }

        // =====================================================
        // F. FINAL ANALYSIS SUMMARY
        // =====================================================

        private static string BuildFinalSummary(
    SalesGrowthAnalysis growth,
    ItemAnalysisSummary itemAnalysis,
    CategoryAnalysisSummary catAnalysis,
    CompositionSummary composition)
        {
            var parts = new List<string>();

            if (growth.HasPreviousPeriod && growth.ChangePercent.HasValue)
            {
                parts.Add(
                    $"Sales {growth.Direction.ToLower()} by " +
                    $"{Math.Abs(growth.ChangePercent.Value):N2}% " +
                    "compared with the previous equivalent period.");
            }
            else
            {
                parts.Add(
                    $"The selected period generated " +
                    $"₱{growth.CurrentTotal:N2} in total sales. " +
                    "A comparative growth rate is not available " +
                    "because no previous equivalent period was found.");
            }

            // =====================================================
            // CROSS-SECTION ALIGNMENT CHECK
            // Does the same category drive growth, lead composition,
            // AND contain the highest-revenue item? Only meaningful
            // when "All Categories" is selected (composition is
            // category-level, not item-level).
            // =====================================================

            string? alignedCategory = null;

            if (!composition.IsSingleCategory &&
                composition.Rows.Count > 0 &&
                itemAnalysis.HighestSalesItem != null)
            {
                string topCompositionCategory = composition.Rows[0].Label;

                CategoryAnalysisRow? driver = null;

                if (growth.HasPreviousPeriod && growth.ChangeAmount != 0)
                {
                    driver =
                        growth.ChangeAmount > 0
                            ? catAnalysis.Categories
                                .Where(c => c.ChangeAmount > 0)
                                .OrderByDescending(c => c.ChangeAmount)
                                .FirstOrDefault()
                            : catAnalysis.Categories
                                .Where(c => c.ChangeAmount < 0)
                                .OrderBy(c => c.ChangeAmount)
                                .FirstOrDefault();
                }

                bool topItemMatches =
                    itemAnalysis.HighestSalesItem.Category == topCompositionCategory;

                if (driver != null &&
                    driver.Category == topCompositionCategory &&
                    topItemMatches)
                {
                    alignedCategory = topCompositionCategory;
                }
            }

            if (alignedCategory != null)
            {
                parts.Add(
                    $"{alignedCategory} was consistently the strongest performer this period — " +
                    "it drove the overall sales change, led total sales composition, and contained " +
                    $"{itemAnalysis.HighestSalesItem!.ItemName}, the highest-revenue item overall. " +
                    "This indicates sales performance was concentrated in a single category rather " +
                    "than distributed across the business.");
            }
            else
            {
                if (itemAnalysis.HighestQuantityItem != null &&
                    itemAnalysis.HighestSalesItem != null &&
                    itemAnalysis.HighestQuantityItem.ItemID != itemAnalysis.HighestSalesItem.ItemID)
                {
                    parts.Add(
                        $"{itemAnalysis.HighestQuantityItem.ItemName} " +
                        $"sold in the greatest volume, but " +
                        $"{itemAnalysis.HighestSalesItem.ItemName} " +
                        $"generated more revenue overall, showing that " +
                        "the top-selling item by volume is not always " +
                        "the top earner.");
                }
                else if (itemAnalysis.HighestQuantityItem != null)
                {
                    parts.Add(
                        $"{itemAnalysis.HighestQuantityItem.ItemName} " +
                        "led in both quantity sold and revenue generated.");
                }

                if (catAnalysis.HasPreviousPeriod &&
                    catAnalysis.LargestIncreaseCategory != null &&
                    catAnalysis.LargestDecreaseCategory != null)
                {
                    parts.Add(
                        $"{catAnalysis.LargestIncreaseCategory} gained " +
                        $"the most ground " +
                        $"(+{catAnalysis.LargestIncreasePercent:N2}%), " +
                        $"while {catAnalysis.LargestDecreaseCategory} " +
                        $"pulled back the most " +
                        $"({catAnalysis.LargestDecreasePercent:N2}%).");
                }

                if (catAnalysis.HighestPerformingCategory != null)
                {
                    parts.Add(
                        $"{catAnalysis.HighestPerformingCategory} was the " +
                        "highest-performing category based on current-period sales.");
                }
            }

            return string.Join(" ", parts);
        }

        // =====================================================
        // PERIOD TEXT
        // =====================================================

        private static string GetPeriodText(
            string? range,
            DateTime? fromDate,
            DateTime? toDate)
        {
            if (
                range == "daily" &&
                fromDate.HasValue)
            {
                return fromDate.Value
                    .ToString(
                        "MMMM dd, yyyy");
            }

            if (
                range == "weekly" &&
                fromDate.HasValue)
            {
                var end =
                    fromDate.Value.AddDays(6);

                return
                    $"{fromDate.Value:MMMM dd} - " +
                    $"{end:MMMM dd, yyyy}";
            }

            if (
                range == "monthly" &&
                fromDate.HasValue)
            {
                return fromDate.Value
                    .ToString(
                        "MMMM yyyy");
            }

            if (
                range == "custom" &&
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
                .DefaultTextStyle(
                    x =>
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
                    x =>
                        x.FontSize(7));
        }

        private static IContainer LabelStyle(
            IContainer c)
        {
            return c
                .Padding(4)
                .DefaultTextStyle(
                    x =>
                        x.Bold()
                         .FontSize(9));
        }

        private static IContainer ValueStyle(
            IContainer c)
        {
            return c
                .Padding(4)
                .DefaultTextStyle(
                    x =>
                        x.FontSize(9));
        }

        // =====================================================
        // DELETE TEMP FILE
        // =====================================================

        private static void DeleteFile(
            string path)
        {
            try
            {
                if (
                    System.IO.File.Exists(
                        path))
                {
                    System.IO.File.Delete(
                        path);
                }
            }
            catch
            {
                // Ignore cleanup errors.
            }
        }
    }
}