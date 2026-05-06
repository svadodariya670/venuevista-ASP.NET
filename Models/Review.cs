using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VenueBooking.Models
{
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }

        [Required]
        [ForeignKey(nameof(Venue))]
        public int VenueId { get; set; }

        [Required]
        [ForeignKey(nameof(Customer))]
        public int CustomerId { get; set; }

        [Required]
        [ForeignKey(nameof(Booking))]
        public int BookingId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        public string Comment { get; set; }

        public bool IsApproved { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual Venue Venue { get; set; }
        public virtual User Customer { get; set; }
        public virtual Booking Booking { get; set; }
    }
}