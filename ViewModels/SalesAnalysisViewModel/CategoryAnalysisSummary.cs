namespace LOSTBOOKS.ViewModels.SalesAnalysisViewModel
{
    public class CategoryAnalysisSummary
    {
        public List<CategoryAnalysisRow> Categories { get; set; } = new List<CategoryAnalysisRow>();

        public String? HighestPerformingCategory { get; set; }

        public string? LargestIncreaseCategory {  get; set; }

        public decimal? LargestIncreasePercent { get; set; } 

        public string? LargestDecreaseCategory { get; set; }

        public decimal? LargestDecreasePercent { get; set; }

        public bool HasPreviousPeriod { get; set; }    
    }
}
