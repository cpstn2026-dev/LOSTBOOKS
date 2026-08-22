namespace LOSTBOOKS.ViewModels.SalesAnalysisViewModel
{
    public class TrendAnalysis
    {
        public DateTime? HighestSalesDate { get; set; }

        public decimal HighestSalesDateTotal { get; set; }

        public DateTime? LowestSalesDate { get; set; }

        public decimal LowestSalesDateTotal { get; set; }

        public decimal BeginningTotal { get; set; }

        public decimal EndingTotal { get; set; }

        public decimal ChangeAmount { get; set; }

        public decimal? ChangePercent { get; set; }

        public bool HasData { get; set; }
    }
}
