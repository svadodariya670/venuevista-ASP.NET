using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VenueBooking.Models.Enums;

namespace VenueBooking.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required]
        [ForeignKey(nameof(Venue))]
        public int VenueId { get; set; }

        [Required]
        [ForeignKey(nameof(Theme))]
        public int ThemeId { get; set; }

        [ForeignKey(nameof(Customer))]
        public int? CustomerId { get; set; }

        [Required]
        [StringLength(50)]
        public string EventType { get; set; }  // Use EventTypes constants

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [NotMapped]
        public int TotalDays => (EndDate - StartDate).Days + 1;

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalPrice { get; set; }

        [StringLength(20)]
        public string BookingSource { get; set; } = "Online";

        [StringLength(20)]
        public string Status { get; set; } = BookingStatus.Pending;

        [StringLength(20)]
        public string PaymentStatus { get; set; } = VenueBooking.Models.Enums.PaymentStatus.Pending;

        public string SpecialRequests { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual Venue Venue { get; set; }
        public virtual Theme Theme { get; set; }
        public virtual User Customer { get; set; }
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
        public virtual Review Review { get; set; }
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}