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

        [Required]
        public string Email { get; set; } = string.Empty;

        // Set when a "Forgot Password" email is requested; cleared once used.
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }

        // Used only by the console-based Emergency Recovery fallback
        // (accounts with no working email). Separate from the email token
        // above so the two paths never interfere with each other.
        public string? EmergencyRecoveryToken { get; set; }
        public DateTime? EmergencyRecoveryTokenExpiry { get; set; }
        [NotMapped]
        public string UserCode => $"U-{UserID:D4}";
    }
}