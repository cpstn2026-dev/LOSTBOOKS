using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LOSTBOOKS.Models
{
    public class Service
    {
        public int ServiceID { get; set; }
        public string CustomerName { get; set; }

        public string ContactNumber { get; set; }

        public string ServiceType { get; set; }

        public string Size { get; set; }

        public int NumberOfPages { get; set; }

        public string CoverFinish { get; set; }

        public string Status { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? AssessedPrice { get; set; }
    }
}