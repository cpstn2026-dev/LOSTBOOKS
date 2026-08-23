namespace LOSTBOOKS.ViewModels.SalesAnalysisViewModel
{
    public class CompositionSummary
    {
        // false = showing Books/Products/Merchandise/Services against each other
        // true  = a specific category was selected, showing the items inside it
        public bool IsSingleCategory { get; set; }

        public string ChartTitle { get; set; } = "Category Performance";

        public List<CompositionRow> Rows { get; set; } = new List<CompositionRow>();
    }
}
