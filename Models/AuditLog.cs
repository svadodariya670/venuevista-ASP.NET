using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VenueBooking.Models
{
    public class AuditLog
    {
        [Key]
        public int LogId { get; set; }

        [Required]
        [StringLength(50)]
        public string TableName { get; set; }

        [Required]
        public int RecordId { get; set; }

        [Required]
        [StringLength(20)]
        public string Action { get; set; } // INSERT, UPDATE, DELETE

        public string OldValues { get; set; } // JSON

        public string NewValues { get; set; } // JSON

        [ForeignKey(nameof(ChangedByUser))]
        public int? ChangedBy { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual User ChangedByUser { get; set; }
    }
}