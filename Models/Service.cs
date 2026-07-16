using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LOSTBOOKS.Models
{
    public class Service
    {
        [Key]
        public int ServiceID { get; set; }

        [Required]
        public required string ServiceType { get; set; }

        [Required]
        public required string Size { get; set; }        

        [Required]
        public int NumberOfPages { get; set; }

        [Required]
        public required string CoverFinish { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

    }
}
