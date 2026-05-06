using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VenueBooking.Models
{
    public class Venue
    {
        [Key]
        public int VenueId { get; set; }

        [Required]
        [ForeignKey(nameof(Owner))]
        public int OwnerId { get; set; }

        [Required]
        [StringLength(150)]
        public string VenueName { get; set; }

        [Required]
        [StringLength(255)]
        public string Address { get; set; }

        [Required]
        [StringLength(100)]
        public string City { get; set; }

        [StringLength(100)]
        public string State { get; set; }

        [StringLength(100)]
        public string Country { get; set; } = "India";

        [Column(TypeName = "decimal(9,6)")]
        public decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(9,6)")]
        public decimal? Longitude { get; set; }

        [Required]
        public int Capacity { get; set; }

        public string Description { get; set; }

        [StringLength(20)]
        public string ContactPhone { get; set; }

        [StringLength(50)]
        public string VenueType { get; set; }

        [StringLength(150)]
        public string Slug { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsVerified { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // ===== NAVIGATION =====

        public virtual User Owner { get; set; }

        public virtual ICollection<VenueImage> Images { get; set; } = new List<VenueImage>();
        public virtual ICollection<VenueAmenity> VenueAmenities { get; set; } = new List<VenueAmenity>();
        public virtual ICollection<Theme> Themes { get; set; } = new List<Theme>();
        public virtual ICollection<SeasonalPricing> SeasonalPricings { get; set; } = new List<SeasonalPricing>();
        public virtual ICollection<VenueBlockedDate> BlockedDates { get; set; } = new List<VenueBlockedDate>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}