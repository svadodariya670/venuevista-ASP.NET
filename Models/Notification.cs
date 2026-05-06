using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VenueBooking.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        [ForeignKey(nameof(User))]
        public int UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        [StringLength(50)]
        public string Type { get; set; }

        public bool IsRead { get; set; } = false;

        [StringLength(500)]
        public string ActionUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual User User { get; set; }
    }
}