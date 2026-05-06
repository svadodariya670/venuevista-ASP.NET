using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VenueBooking.Models
{
    public class VenueBlockedDate
    {
        [Key]
        public int BlockId { get; set; }

        [Required]
        [ForeignKey(nameof(Venue))]
        public int VenueId { get; set; }

        [Required]
        public DateTime BlockDate { get; set; }

        [StringLength(255)]
        public string Reason { get; set; }

        [ForeignKey(nameof(Creator))]
        public int? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual Venue Venue { get; set; }
        public virtual User Creator { get; set; }
    }
}