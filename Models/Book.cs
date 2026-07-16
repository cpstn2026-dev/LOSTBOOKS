using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LOSTBOOKS.Models
{
    public class Book
    {
        [Key]
        public int BookID { get; set; }

        [Required]
        public required string ISBN { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public required string Author { get; set; }

        [Required]
        public required string Condition { get; set; }

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
