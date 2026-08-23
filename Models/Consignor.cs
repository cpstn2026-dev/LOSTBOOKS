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

        // REMOVED: HomeAddress, GcashNumber, BankName,
        // BankAccountNumber, AccountName
        // Reason: Sales reports are communicated manually by
        // management. Only Name / Contact / Email are needed
        // for record purposes.

        // NEW: for Deactivate instead of hard Delete
        public bool IsActive { get; set; } = true;

        public ICollection<Book>? Books { get; set; }
        public ICollection<Merchandise>? Merchandises { get; set; }
    }
}