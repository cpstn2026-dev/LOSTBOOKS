namespace LOSTBOOKS.ViewModels.SalesAnalysisViewModel
{
    public class ItemAnalysisRow
    {
        public string ItemID { get; set; } = "";

        public string ItemName { get; set; } = "";

        public string Category { get; set; } = "";

        public int CurrentQuantity { get; set; }

        public decimal CurrentSales { get; set; }

        public int PreviousQuantity { get; set; }

        public decimal PreviousSales { get; set; }

        public decimal? QuantityChangePercent { get; set; }

        public decimal? RevenueChangePercent { get; set; }
    }

    public class ItemAnalysisSummary
    {
        public ItemAnalysisRow? HighestQuantityItem { get; set; }

        public ItemAnalysisRow? HighestSalesItem { get; set; }

        public List<ItemAnalysisRow> AllItems { get; set; } =
            new List<ItemAnalysisRow>();

        public List<ItemAnalysisRow> IncreasedItems { get; set; } =
            new List<ItemAnalysisRow>();

        public List<ItemAnalysisRow> DecreasedItems { get; set; } =
            new List<ItemAnalysisRow>();

        public bool HasPreviousPeriod { get; set; }
    }
}