using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LOSTBOOKS.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }
        [Required]
        public string FullName { get; set; } =
        string.Empty;
        [Required]
        public string Username { get; set; } =
        string.Empty;
        [Required]
        public string PasswordHash { get; set; } =
        string.Empty;
        // "Staff" or "Manager"
        [Required]
        public string Role { get; set; } = "Staff";
        // "Active" or "Inactive"
        [Required]
        public string Status { get; set; } = "Active";

        // True right after a Manager-issued temp password reset —
        // forces the "Set New Password" screen before anything else works.
        public bool MustChangePassword { get; set; } = false;
        [NotMapped]
        public string UserCode => $"U-{UserID:D4}";
    }
}