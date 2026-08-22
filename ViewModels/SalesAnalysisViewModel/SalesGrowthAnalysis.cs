namespace LOSTBOOKS.ViewModels.SalesAnalysisViewModel
{
    public class SalesGrowthAnalysis
    {
        public decimal CurrentTotal { get; set; }

        public decimal PreviousTotal { get; set; }

        public decimal ChangeAmount { get; set; }

        public decimal? ChangePercent { get; set; }

        public string Direction { get; set; } = "Unchanged";

        public bool HasPreviousPeriod { get; set; }
    }
}
