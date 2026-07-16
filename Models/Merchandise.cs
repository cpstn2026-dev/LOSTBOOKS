using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace LOSTBOOKS.Models
{
    public class Merchandise
    {
        [Key]
        public int MerchandiseID { get; set; }

        [Required]
        public required string MerchandiseName { get; set; }

        [Required]
        public required string Category { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal SellingPrice { get; set; }

       

        [Required]
        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal StoreSharePercentage { get; set; }

        public int ConsignorID { get; set; }

        [ForeignKey("ConsignorID")]
        public Consignor? Consignor { get; set; }
    }
}
