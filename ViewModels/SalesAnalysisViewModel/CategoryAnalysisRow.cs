namespace LOSTBOOKS.ViewModels.SalesAnalysisViewModel
{
    public class CategoryAnalysisRow
    {
        public string Category { get; set; } = "";

        public decimal CurrentSales {  get; set; }

        public decimal PreviousSales { get; set; }
        public decimal ChangeAmount {  get; set; }

        public decimal? ChangePercent { get; set; } 

    }
}
