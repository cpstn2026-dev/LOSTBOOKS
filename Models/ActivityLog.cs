using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LOSTBOOKS.Models
{
    public class ActivityLog
    {
        [Key]
        public int ActivityLogID { get; set; }
        public int UserID { get; set; }
        [ForeignKey("UserID")]
        public User? User { get; set; }
        public DateTime DateTime { get; set; } =
        DateTime.Now;
        [Required]
        public string Module { get; set; } =
        string.Empty;
        [Required]
        public string Action { get; set; } =
        string.Empty;
        public string Description { get; set; } =
        string.Empty;
        [NotMapped]
        public string ActivityLogCode => $"AL-{ActivityLogID:D6}";
    }
}
