using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LOSTBOOKS.Models
{
    public class Product
    {
        [Key]
        public int ProductID { get; set; }

        [Required]
        public required string ProductName { get; set; }

        [Required]
        public required string Category { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal SellingPrice { get; set; }

    }
}
