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
using SkiaSharp;
using System.Composition;
using System.Text.Json;

using QuestColors = QuestPDF.Helpers.Colors;

namespace LOSTBOOKS.Controllers
{
    public class SalesReportingController : Controller
    {
        private readonly LOSTBOOKSContext _context;
        private readonly LOSTBOOKS.Services.IActivityLogger _activityLogger;

        public SalesReportingController(
            LOSTBOOKSContext context,
            LOSTBOOKS.Services.IActivityLogger activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
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

            bool isSingleCategoryFilter =
                !string.IsNullOrWhiteSpace(category) &&
                !category.Equals("All", StringComparison.OrdinalIgnoreCase);

            var growthResult =
                BuildOverallGrowthInsight(
                    salesGrowth,
                    categoryAnalysis);

            ViewBag.GrowthFindings = growthResult.Findings;
            ViewBag.GrowthInsightText = growthResult.Insight;
            ViewBag.GrowthConsider = growthResult.Consider;
            ViewBag.GrowthBasis = growthResult.Basis;

            var trendResult =
                BuildTrendInsight(sales, isSingleCategoryFilter);

            ViewBag.TrendFindings = trendResult.Findings;
            ViewBag.TrendInsightText = trendResult.Insight;
            ViewBag.TrendConsider = trendResult.Consider;
            ViewBag.TrendBasis = trendResult.Basis;

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

            var compositionResult =
                BuildCompositionInsight(composition);

            ViewBag.CompositionFindings = compositionResult.Findings;
            ViewBag.CompositionInsightText = compositionResult.Insight;
            ViewBag.CompositionConsider = compositionResult.Consider;
            ViewBag.CompositionBasis = compositionResult.Basis;

            var itemResult =
                BuildItemInsight(itemAnalysis);

            ViewBag.ItemFindings = itemResult.Findings;
            ViewBag.ItemInsightText = itemResult.Insight;
            ViewBag.ItemConsider = itemResult.Consider;
            ViewBag.ItemBasis = itemResult.Basis;

            var itemConcentrationResult =
                BuildItemConcentrationInsight(itemAnalysis);

            ViewBag.ItemConcentrationFindings = itemConcentrationResult.Findings;
            ViewBag.ItemConcentrationInsightText = itemConcentrationResult.Insight;
            ViewBag.ItemConcentrationConsider = itemConcentrationResult.Consider;
            ViewBag.ItemConcentrationBasis = itemConcentrationResult.Basis;

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
            //
            // 
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

            bool pdfIsSingleCategoryFilter =
    !string.IsNullOrWhiteSpace(category) &&
    !category.Equals("All", StringComparison.OrdinalIgnoreCase);

            var overallResult =
                BuildOverallGrowthInsight(
                    pdfGrowth,
                    pdfCategoryAnalysis);

            List<string> overallFindings = overallResult.Findings;
            string? overallInsightText = overallResult.Insight;
            string? overallConsiderText = overallResult.Consider;
            string overallBasisText = overallResult.Basis;

            var trendResult =
                BuildTrendInsight(sales, pdfIsSingleCategoryFilter);

            List<string> trendFindings = trendResult.Findings;
            string? trendInsightText = trendResult.Insight;
            string? trendConsiderText = trendResult.Consider;
            string trendBasisText = trendResult.Basis;

            var compositionResult =
                BuildCompositionInsight(pdfComposition);

            List<string> compositionFindings = compositionResult.Findings;
            string? compositionInsightText = compositionResult.Insight;
            string? compositionConsiderText = compositionResult.Consider;
            string compositionBasisText = compositionResult.Basis;

            var itemResult =
                BuildItemInsight(pdfItemAnalysis);

            List<string> itemFindings = itemResult.Findings;
            string? itemInsightText = itemResult.Insight;
            string? itemConsiderText = itemResult.Consider;
            string itemBasisText = itemResult.Basis;

            var itemConcentrationResult =
                BuildItemConcentrationInsight(pdfItemAnalysis);

            List<string> itemConcentrationFindings = itemConcentrationResult.Findings;
            string? itemConcentrationInsightText = itemConcentrationResult.Insight;
            string? itemConcentrationConsiderText = itemConcentrationResult.Consider;
            string itemConcentrationBasisText = itemConcentrationResult.Basis;

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
                                    // SALES ANALYSIS
                                    // =================================================

                                    col.Item()
                                        .PaddingTop(10)
                                        .Text("SALES ANALYSIS")
                                        .Bold()
                                        .FontSize(13);

                                    col.Item()
                                        .PaddingTop(2)
                                        .Text(
                                            "Analysis sections identify patterns, comparisons, and relationships " +
                                            "in the sales data — they do not determine the causes behind them.")
                                        .Italic()
                                        .FontColor(QuestColors.Grey.Darken1)
                                        .FontSize(7.5f);

                                    col.Item()
                                        .PaddingTop(4)
                                        .Column(analysisCol =>
                                        {
                                            RenderFindings(
                                            analysisCol,
                                            overallFindings,
                                            overallInsightText,
                                            overallConsiderText,
                                            overallBasisText,
                                            findingFontSize: 9,
                                            considerFontSize: 9,
                                            basisFontSize: 8);
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

                                                    RenderFindings(
                                                        text,
                                                        trendFindings,
                                                        trendInsightText,
                                                        trendConsiderText,
                                                        trendBasisText);
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

                                                    RenderFindings(
                                                        text,
                                                        compositionFindings,
                                                        compositionInsightText,
                                                        compositionConsiderText,
                                                        compositionBasisText);
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

                                                    RenderFindings(
                                                        text,
                                                        itemFindings,
                                                        itemInsightText,
                                                        itemConsiderText,
                                                        itemBasisText);
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
                                        .Column(concentrationCol =>
                                        {
                                            RenderFindings(
                                                concentrationCol,
                                                itemConcentrationFindings,
                                                itemConcentrationInsightText,
                                                itemConcentrationConsiderText,
                                                itemConcentrationBasisText);
                                        });

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

                _activityLogger.Log(
                    "Reports",
                    "Sales Report Generated",
                    "Sales Reporting and Analysis report generated");

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
        // RENDER FINDINGS AS BULLETS (PDF)
        // =====================================================

        private static void RenderFindings(
        QuestPDF.Fluent.ColumnDescriptor column,
        List<string> findings,
        string? insight,
        string? consider,
        string basis,
        float findingFontSize = 8,
        float insightFontSize = 7.5f,
        float considerFontSize = 7.5f,
        float basisFontSize = 7)
        {
            foreach (var line in findings)
            {
                column.Item()
                    .PaddingTop(2)
                    .Text($"• {line}")
                    .FontSize(findingFontSize);
            }

            if (!string.IsNullOrWhiteSpace(insight))
            {
                column.Item()
                    .PaddingTop(3)
                    .Text($"Insight: {insight}")
                    .FontSize(insightFontSize);
            }

            if (!string.IsNullOrWhiteSpace(consider))
            {
                column.Item()
                    .PaddingTop(3)
                    .Text($"Consider: {consider}")
                    .Italic()
                    .FontSize(considerFontSize);
            }

            if (!string.IsNullOrWhiteSpace(basis))
            {
                column.Item()
                    .PaddingTop(3)
                    .Text($"Basis: {basis}")
                    .Italic()
                    .FontColor(QuestColors.Grey.Darken1)
                    .FontSize(basisFontSize);
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

        private static (List<string> Findings, string? Insight, string? Consider, string Basis) BuildOverallGrowthInsight(
    SalesGrowthAnalysis growth,
    CategoryAnalysisSummary catAnalysis)
        {
            const string basisText = "Current period totals vs. previous equivalent period, by category";

            if (!growth.HasPreviousPeriod)
            {
                return (
                    new List<string>
                    {
                        $"₱{growth.CurrentTotal:N2} in total sales this period. No previous equivalent period is available for comparison."
                    },
                    null, null, basisText);
            }

            string direction =
                growth.ChangeAmount > 0 ? "increased" :
                growth.ChangeAmount < 0 ? "decreased" : "remained unchanged";

            string overallSentence =
                $"Sales {direction} by ₱{Math.Abs(growth.ChangeAmount):N2}" +
                (growth.ChangePercent.HasValue ? $" ({Math.Abs(growth.ChangePercent.Value):N2}%) vs. the previous period." : ".");

            var findings = new List<string> { overallSentence };

            if (catAnalysis.Categories.Count == 0)
            {
                return (findings, null, null, basisText);
            }

            const decimal materialityFloor = 100m;

            var decreases = catAnalysis.Categories
                .Where(c => c.ChangeAmount <= -materialityFloor)
                .OrderBy(c => c.ChangeAmount)
                .ToList();

            var increases = catAnalysis.Categories
                .Where(c => c.ChangeAmount >= materialityFloor)
                .OrderByDescending(c => c.ChangeAmount)
                .ToList();

            if (decreases.Count == 0 && increases.Count == 0)
            {
                return (findings, null, null, basisText);
            }

            var considerParts = new List<string>();

            if (decreases.Count > 0)
            {
                string decreaseList = string.Join(", ",
                    decreases.Select(c => $"{c.Category} (-₱{Math.Abs(c.ChangeAmount):N2})"));

                findings.Add($"Largest declines: {decreaseList}.");
            }

            if (increases.Count > 0)
            {
                string increaseList = string.Join(", ",
                    increases.Select(c => $"{c.Category} (+₱{c.ChangeAmount:N2})"));

                findings.Add(
                    decreases.Count > 0
                        ? $"Offset by growth in {increaseList}."
                        : $"Largest gains: {increaseList}.");
            }

            var smallBaseFlags = increases
                .Where(c =>
                    c.ChangePercent.HasValue &&
                    c.ChangePercent.Value >= 300m &&
                    c.PreviousSales > 0 &&
                    c.PreviousSales < 1000m)
                .ToList();

            foreach (var flag in smallBaseFlags)
            {
                findings.Add(
                    $"{flag.Category}'s +{flag.ChangePercent:N2}% is off a small base (₱{flag.PreviousSales:N2}) — " +
                    $"the ₱{flag.ChangeAmount:N2} absolute increase is the more reliable figure.");

                considerParts.Add(
                    $"Watch {flag.Category} over a few more periods before treating this percentage as a trend.");
            }

            var wipeouts = catAnalysis.Categories
                .Where(c => c.PreviousSales > 0 && c.CurrentSales == 0)
                .OrderByDescending(c => c.PreviousSales)
                .ToList();

            if (wipeouts.Count > 0)
            {
                string wipeoutList = string.Join(", ", wipeouts.Select(c => c.Category));

                findings.Add(
                    $"{(wipeouts.Count == 1 ? "One category" : $"{wipeouts.Count} categories")} dropped to " +
                    $"₱0 entirely — {wipeoutList} (a complete stoppage, not a gradual decline).");

                considerParts.Add(
                    $"Confirm with staff whether {(wipeouts.Count == 1 ? wipeouts[0].Category : "these categories")} " +
                    "are still being stocked, or if this reflects discontinued items.");
            }

            // =====================================================
            // INSIGHT — "so what" — filter/data-aware
            // =====================================================

            var insightParts = new List<string>();

            var allMoves = decreases.Concat(increases).ToList();
            var driver = allMoves.OrderByDescending(c => Math.Abs(c.ChangeAmount)).FirstOrDefault();

            if (decreases.Count > 0 && increases.Count > 0 && driver != null)
            {
                var opposing = allMoves
                    .Where(c => Math.Sign(c.ChangeAmount) != Math.Sign(driver.ChangeAmount))
                    .OrderByDescending(c => Math.Abs(c.ChangeAmount))
                    .FirstOrDefault();

                insightParts.Add(
                    opposing != null
                        ? $"Performance this period was shaped mainly by {driver.Category}, though offset partly by {opposing.Category}."
                        : $"Performance this period was shaped mainly by {driver.Category}.");
            }
            else if (increases.Count > 0 && decreases.Count == 0)
            {
                insightParts.Add(
                    increases.Count == 1
                        ? $"Growth this period came primarily from {increases[0].Category}."
                        : "Growth this period came from broad gains across categories rather than one standout.");
            }
            else if (decreases.Count > 0 && increases.Count == 0)
            {
                insightParts.Add(
                    decreases.Count == 1
                        ? $"The decline this period was concentrated in {decreases[0].Category}."
                        : "The decline this period was spread across multiple categories rather than one.");
            }

            if (wipeouts.Count > 0)
            {
                insightParts.Add(
                    "This shift is partly exaggerated by an apparent full stoppage in " +
                    $"{(wipeouts.Count == 1 ? wipeouts[0].Category : string.Join(", ", wipeouts.Select(c => c.Category)))} " +
                    "— worth confirming that's accurate before treating it as a real decline.");
            }
            else if (smallBaseFlags.Count > 0)
            {
                insightParts.Add(
                    $"The standout percentage in {smallBaseFlags[0].Category} reflects a low starting point " +
                    "more than a real surge.");
            }

            string? insight = insightParts.Count > 0 ? string.Join(" ", insightParts) : null;
            string? consider = considerParts.Count > 0 ? string.Join(" ", considerParts) : null;

            return (findings, insight, consider, basisText);
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

        private static (List<string> Findings, string? Insight, string? Consider, string Basis) BuildItemInsight(ItemAnalysisSummary itemAnalysis)
        {
            const string basisText = "Highest quantity sold vs. highest revenue per item";

            if (itemAnalysis.HighestQuantityItem == null)
            {
                return (new List<string> { "No item data available for the selected period." }, null, null, basisText);
            }

            var findingParts = new List<string>();
            var considerParts = new List<string>();

            if (itemAnalysis.HighestSalesItem != null &&
                itemAnalysis.HighestQuantityItem.ItemID != itemAnalysis.HighestSalesItem.ItemID)
            {
                var volumeLeader = itemAnalysis.HighestQuantityItem;
                var revenueLeader = itemAnalysis.HighestSalesItem;

                decimal revenueDiff = revenueLeader.CurrentSales - volumeLeader.CurrentSales;
                int unitDiff = volumeLeader.CurrentQuantity - revenueLeader.CurrentQuantity;

                decimal volumeLeaderAvgPrice = volumeLeader.CurrentQuantity > 0
                    ? volumeLeader.CurrentSales / volumeLeader.CurrentQuantity : 0;

                decimal revenueLeaderAvgPrice = revenueLeader.CurrentQuantity > 0
                    ? revenueLeader.CurrentSales / revenueLeader.CurrentQuantity : 0;

                findingParts.Add(
                    $"{volumeLeader.ItemName} sold in the greatest volume at {volumeLeader.CurrentQuantity} unit(s), " +
                    $"but {revenueLeader.ItemName} generated more revenue overall at ₱{revenueLeader.CurrentSales:N2} " +
                    $"(₱{revenueDiff:N2} more) despite selling {Math.Max(0, unitDiff)} fewer unit(s)" +
                    (revenueLeaderAvgPrice > volumeLeaderAvgPrice
                        ? $" — averaging about ₱{revenueLeaderAvgPrice:N2} per unit versus ₱{volumeLeaderAvgPrice:N2}."
                        : "."));

                considerParts.Add(
                    $"{volumeLeader.ItemName} brings in the most customers, while {revenueLeader.ItemName} " +
                    "brings in the most money — consider pairing them (a bundle, a combo, or shelf " +
                    $"placement together) to encourage buyers of {volumeLeader.ItemName} to also pick up " +
                    $"{revenueLeader.ItemName}.");
            }
            else
            {
                findingParts.Add(
                    $"{itemAnalysis.HighestQuantityItem.ItemName} led in both quantity sold " +
                    $"({itemAnalysis.HighestQuantityItem.CurrentQuantity} unit(s)) and revenue " +
                    $"(₱{itemAnalysis.HighestQuantityItem.CurrentSales:N2}), with no other item close on either metric.");
            }

            if (itemAnalysis.HasPreviousPeriod)
            {
                int comparableCount = itemAnalysis.AllItems.Count(i => i.QuantityChangePercent.HasValue);

                var biggestGain = itemAnalysis.IncreasedItems.FirstOrDefault();
                var biggestDrop = itemAnalysis.DecreasedItems.FirstOrDefault();

                if (comparableCount <= 1)
                {
                    var onlyComparable = biggestGain ?? biggestDrop;

                    if (onlyComparable != null)
                    {
                        findingParts.Add(
                            $"{onlyComparable.ItemName} was the only item with prior-period data " +
                            $"available for comparison, showing a " +
                            $"{(onlyComparable.QuantityChangePercent!.Value >= 0 ? "+" : "")}" +
                            $"{onlyComparable.QuantityChangePercent.Value:N2}% change in demand — with " +
                            "just one comparable item, this isn't yet a meaningful ranking.");

                        considerParts.Add(
                            "Not enough purchase history yet to act on this — revisit once more " +
                            "transactions come in.");
                    }
                }
                else if (biggestGain != null && biggestDrop != null && biggestGain.ItemID != biggestDrop.ItemID)
                {
                    findingParts.Add(
                        $"{biggestGain.ItemName} saw the largest jump in demand " +
                        $"(+{biggestGain.QuantityChangePercent!.Value:N2}%), while {biggestDrop.ItemName} pulled back the most " +
                        $"({biggestDrop.QuantityChangePercent!.Value:N2}%) compared with the previous period.");
                }
                else if (biggestGain != null)
                {
                    findingParts.Add(
                        $"{biggestGain.ItemName} recorded the largest increase in demand at " +
                        $"+{biggestGain.QuantityChangePercent!.Value:N2}% compared with the previous period.");
                }
                else if (biggestDrop != null)
                {
                    findingParts.Add(
                        $"{biggestDrop.ItemName} recorded the largest decline in demand at " +
                        $"{biggestDrop.QuantityChangePercent!.Value:N2}% compared with the previous period.");
                }
            }

            string? insight = null;

            if (itemAnalysis.HighestSalesItem != null &&
                itemAnalysis.HighestQuantityItem.ItemID != itemAnalysis.HighestSalesItem.ItemID)
            {
                insight =
                    $"{itemAnalysis.HighestQuantityItem.ItemName} draws the most transactions, but " +
                    $"{itemAnalysis.HighestSalesItem.ItemName} contributes more — unit sales alone would " +
                    "undercount its impact.";
            }
            else
            {
                insight =
                    $"{itemAnalysis.HighestQuantityItem.ItemName} isn't just popular — it's also the strongest " +
                    "earner, making it the one to prioritize keeping in stock.";
            }

            string? consider = considerParts.Count > 0 ? string.Join(" ", considerParts) : null;

            return (findingParts, insight, consider, basisText);
        }

        // =====================================================
        // C3. ITEM CONCENTRATION INSIGHT (for the full Item Performance table)
        // =====================================================

        private static (List<string> Findings, string? Insight, string? Consider, string Basis) BuildItemConcentrationInsight(ItemAnalysisSummary itemAnalysis)
        {
            const string basisText = "Revenue share of the top items vs. all items sold";

            if (itemAnalysis.AllItems == null || itemAnalysis.AllItems.Count == 0)
            {
                return (new List<string> { "No item data available for the selected period." }, null, null, basisText);
            }

            var byRevenue = itemAnalysis.AllItems.OrderByDescending(i => i.CurrentSales).ToList();
            decimal grandTotal = byRevenue.Sum(i => i.CurrentSales);

            var findingParts = new List<string>();
            var considerParts = new List<string>();
            string? insight = null;

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

                    findingParts.Add(
                        $"The top {topCount} item(s) by revenue — {itemNames} — accounted for ₱{topTotal:N2}, " +
                        $"or {topShare:N2}% of total sales across all {byRevenue.Count} items, leaving the remaining " +
                        $"{remaining} item(s) to share just {remainingShare:N2}%. " +
                        (topShare >= 50m
                            ? "This indicates item-level sales were heavily concentrated in a small number of products."
                            : "This indicates sales were relatively distributed across the item catalog."));

                    if (topShare >= 50m)
                    {
                        considerParts.Add(
                            "A small number of items carry most of your revenue — keep them well-stocked, " +
                            "and consider promoting or bundling the slower-moving items to spread that reliance.");

                        insight =
                            "A handful of items drive most of this period's revenue — losing any one would " +
                            "have an outsized effect.";
                    }
                    else
                    {
                        insight = "Revenue is fairly spread across the catalog.";
                    }
                }
                else
                {
                    findingParts.Add(
                        $"{itemNames} — the only {byRevenue.Count} item(s) in this period — together generated " +
                        $"₱{topTotal:N2} in total sales.");
                }
            }

            if (itemAnalysis.HasPreviousPeriod)
            {
                int comparableCount = itemAnalysis.AllItems.Count(i => i.QuantityChangePercent.HasValue);

                var biggestGain = itemAnalysis.IncreasedItems.FirstOrDefault();
                var biggestDrop = itemAnalysis.DecreasedItems.FirstOrDefault();

                if (comparableCount <= 1)
                {
                    var onlyComparable = biggestGain ?? biggestDrop;

                    if (onlyComparable != null)
                    {
                        findingParts.Add(
                            $"{onlyComparable.ItemName} was the only item with prior-period data " +
                            $"available for comparison, showing a " +
                            $"{(onlyComparable.QuantityChangePercent!.Value >= 0 ? "+" : "")}" +
                            $"{onlyComparable.QuantityChangePercent.Value:N2}% change in demand — with " +
                            "just one comparable item, this isn't yet a meaningful ranking.");

                        if (considerParts.Count == 0)
                        {
                            considerParts.Add(
                                "Not enough purchase history yet to act on this — revisit once more " +
                                "transactions come in.");
                        }
                    }
                }
                else if (biggestGain != null && biggestDrop != null && biggestGain.ItemID != biggestDrop.ItemID)
                {
                    findingParts.Add(
                        $"{biggestGain.ItemName} saw the largest jump in demand " +
                        $"(+{biggestGain.QuantityChangePercent!.Value:N2}%), while {biggestDrop.ItemName} pulled back the most " +
                        $"({biggestDrop.QuantityChangePercent!.Value:N2}%) compared with the previous period.");
                }
                else if (biggestGain != null)
                {
                    findingParts.Add(
                        $"{biggestGain.ItemName} recorded the largest increase in demand at " +
                        $"+{biggestGain.QuantityChangePercent!.Value:N2}% compared with the previous period.");
                }
                else if (biggestDrop != null)
                {
                    findingParts.Add(
                        $"{biggestDrop.ItemName} recorded the largest decline in demand at " +
                        $"{biggestDrop.QuantityChangePercent!.Value:N2}% compared with the previous period.");
                }
            }

            string? consider = considerParts.Count > 0 ? string.Join(" ", considerParts) : null;

            return (findingParts, insight, consider, basisText);
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

        private static (List<string> Findings, string? Insight, string? Consider, string Basis) BuildTrendInsight(
    List<History> sales,
    bool isSingleCategory)
        {
            const string basisText = "Daily sales totals within the selected period";

            var dailyTotals = sales
                .GroupBy(x => x.TransactionDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Total = g.Sum(x => x.SellingPrice * x.QuantitySold)
                })
                .OrderBy(x => x.Date)
                .ToList();

            if (dailyTotals.Count == 0)
            {
                return (new List<string> { "No sales trend data available for the selected period." }, null, null, basisText);
            }

            var highest = dailyTotals.OrderByDescending(x => x.Total).First();

            var findings = new List<string>();
            var considerParts = new List<string>();

            bool peakDayDriverFires = false;
            decimal peakDayCategoryShare = 0m;
            string peakDayCategoryName = "";

            var highestDaySales = sales.Where(x => x.TransactionDate.Date == highest.Date).ToList();
            decimal highestDayTotal = highestDaySales.Sum(x => x.SellingPrice * x.QuantitySold);

            if (highestDayTotal > 0)
            {
                var topCategoryOnPeakDay = highestDaySales
                    .GroupBy(x => x.Category)
                    .Select(g => new
                    {
                        Category = string.IsNullOrWhiteSpace(g.Key) ? "Unknown" : g.Key,
                        Total = g.Sum(x => x.SellingPrice * x.QuantitySold)
                    })
                    .OrderByDescending(x => x.Total)
                    .FirstOrDefault();

                if (topCategoryOnPeakDay != null)
                {
                    peakDayCategoryShare = Math.Round((topCategoryOnPeakDay.Total / highestDayTotal) * 100m, 2);
                    peakDayCategoryName = topCategoryOnPeakDay.Category;
                    peakDayDriverFires = !isSingleCategory && peakDayCategoryShare >= 50m;

                    findings.Add(
                        $"Peak day ({highest.Date:MMM dd, yyyy}, ₱{highest.Total:N2}) was driven mostly by " +
                        $"{topCategoryOnPeakDay.Category} — {peakDayCategoryShare:N2}% of that day's sales.");
                }
                else
                {
                    findings.Add($"Peak day: {highest.Date:MMM dd, yyyy} at ₱{highest.Total:N2}.");
                }
            }

            bool concentrationFires = false;
            int concentrationTopCount = 0;
            decimal concentrationTopShare = 0m;

            decimal grandTotal = dailyTotals.Sum(d => d.Total);

            if (dailyTotals.Count >= 3 && grandTotal > 0)
            {
                int topCount = Math.Min(3, dailyTotals.Count);
                var topDays = dailyTotals.OrderByDescending(d => d.Total).Take(topCount).ToList();
                decimal topShare = Math.Round((topDays.Sum(d => d.Total) / grandTotal) * 100m, 2);

                if (topShare >= 50m)
                {
                    concentrationFires = true;
                    concentrationTopCount = topCount;
                    concentrationTopShare = topShare;

                    findings.Add(
                        $"Sales are concentrated: the top {topCount} days alone made up {topShare:N2}% of the " +
                        "entire period's total.");

                    considerParts.Add(
                        "Sales are concentrated on a handful of days — worth checking whether promotions, " +
                        "events, or foot traffic on those days can be identified and repeated.");
                }
            }

            decimal beginningTotal = dailyTotals.First().Total;
            decimal endingTotal = dailyTotals.Last().Total;
            decimal changeAmount = endingTotal - beginningTotal;

            decimal? changePercent =
                beginningTotal != 0
                    ? Math.Round((changeAmount / beginningTotal) * 100m, 2)
                    : null;

            if (changePercent.HasValue && dailyTotals.Count > 1)
            {
                string direction =
                    changeAmount > 0 ? "rose" :
                    changeAmount < 0 ? "fell" : "stayed flat";

                findings.Add(
                    $"Overall, sales {direction} {Math.Abs(changePercent.Value):N2}% from the first to the " +
                    "last day in this period.");
            }

            if (findings.Count == 0)
            {
                findings.Add($"Peak day: {highest.Date:MMM dd, yyyy} at ₱{highest.Total:N2}.");
            }

            // =====================================================
            // INSIGHT — category-aware and concentration-aware
            // =====================================================

            string? insight = null;

            if (isSingleCategory)
            {
                if (concentrationFires)
                {
                    insight = "Sales were concentrated on specific days rather than spread evenly throughout the period.";
                }
            }
            else
            {
                if (peakDayDriverFires)
                {
                    insight = "One category's performance on a single day had an outsized effect on the whole period.";
                }
                else if (concentrationFires)
                {
                    insight = "Sales were concentrated on specific days rather than spread evenly throughout the period.";
                }
            }

            string? consider = considerParts.Count > 0 ? string.Join(" ", considerParts) : null;

            return (findings, insight, consider, basisText);
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

        private static (List<string> Findings, string? Insight, string? Consider, string Basis) BuildCompositionInsight(CompositionSummary composition)
        {
            string basisText = composition.IsSingleCategory
                ? "Item-level share of sales within the selected category"
                : "Category-level share of total sales";

            if (composition.Rows.Count == 0)
            {
                return (new List<string> { "No composition data available for the selected period." }, null, null, basisText);
            }

            string noun = composition.IsSingleCategory
                ? "sales in this category"
                : "total sales";

            string itemWord = composition.IsSingleCategory ? "item" : "category";

            var rows = composition.Rows;
            var top = rows[0];

            var findings = new List<string>();
            string? insight;

            if (rows.Count == 1)
            {
                findings.Add($"{top.Label} accounted for all {noun} this period.");

                insight = composition.IsSingleCategory
                    ? "No comparison possible — everything recorded falls under a single item."
                    : "No comparison possible — everything recorded falls under a single category.";

                return (findings, insight, null, basisText);
            }

            var second = rows[1];
            decimal gap = top.Percent - second.Percent;
            bool leaderClear = gap >= 10m;

            findings.Add(
                gap >= 0.01m
                    ? $"{top.Label} leads {second.Label} by {gap:N2} points ({top.Percent:N2}% vs. {second.Percent:N2}%)."
                    : $"{top.Label} and {second.Label} are running close ({top.Percent:N2}% vs. {second.Percent:N2}%).");

            int topCount = Math.Min(3, rows.Count);
            decimal topShare = rows.Take(topCount).Sum(r => r.Percent);
            bool concentrationFires = false;
            string? consider = null;

            if (rows.Count > topCount)
            {
                int remaining = rows.Count - topCount;
                decimal remainingShare = Math.Max(0, 100m - topShare);

                findings.Add(
                    $"Top {topCount} {(topCount == 1 ? itemWord : itemWord + "s")} = {topShare:N2}% of {noun}; " +
                    $"the other {remaining} share just {remainingShare:N2}%.");

                if (topShare >= 70m)
                {
                    concentrationFires = true;

                    consider = composition.IsSingleCategory
                        ? "Heavily concentrated in a few items — keep those well-stocked and consider " +
                          "promoting the slower movers."
                        : "Heavily concentrated in a few categories — worth reviewing whether the rest " +
                          "need more attention.";
                }
            }

            // =====================================================
            // INSIGHT — precedence: concentration first, then leader gap
            // =====================================================

            if (concentrationFires)
            {
                insight = composition.IsSingleCategory
                    ? "A small number of items carry most of the revenue here — exposed if any one slows down."
                    : "A small number of categories carry most of the revenue — exposed if any one slows down.";
            }
            else if (leaderClear)
            {
                insight = composition.IsSingleCategory
                    ? $"{top.Label} stands out within this category — results here lean on a small set of " +
                      "items rather than a broad catalog."
                    : $"{top.Label} is currently the main driver of overall results — performance is " +
                      "closely tied to it.";
            }
            else
            {
                insight = null;
            }

            return (findings, insight, consider, basisText);
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

                if (catAnalysis.HighestPerformingCategory != null &&
                    !composition.IsSingleCategory)
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