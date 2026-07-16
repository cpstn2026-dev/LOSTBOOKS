using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;


namespace LOSTBOOKS.Models
{
    public class Consignor
    {
        [Key]
        public int ConsignorID { get; set; }

        [Required(ErrorMessage = "Consignor Name is required")]
        public string ConsignorName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact Number is required")]
        [Phone(ErrorMessage = "Invalid contact number")]
        public string ContactNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string EmailAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Home Address is required")]
        public string HomeAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "GCash Number is required")]
        public string GcashNumber { get; set; } = string.Empty;

        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? AccountName { get; set; }

        public ICollection<Book>? Books { get; set; }
        public ICollection<Merchandise>? Merchandises { get; set; }
    }
}
