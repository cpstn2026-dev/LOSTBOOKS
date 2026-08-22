using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LOSTBOOKS.Models
{
    public class History
    {
        [Key]
        public int HistoryID { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.Now;

        public string ItemID { get; set; } = string.Empty;

        public string ItemName { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public int QuantitySold { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal SellingPrice { get; set; }

        public int? ConsignorID { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? StoreSharePercentage { get; set; }

        public string PaymentType{ get; set; } = "";

        [NotMapped]
        public string TransactionID => $"TX-{HistoryID:D4}";
    }
}