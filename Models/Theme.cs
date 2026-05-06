using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VenueBooking.Models
{
    public class Theme
    {
        [Key]
        public int ThemeId { get; set; }

        [Required]
        [ForeignKey(nameof(Venue))]
        public int VenueId { get; set; }

        [Required]
        [StringLength(100)]
        public string ThemeName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        // Only ONE theme per venue should be IsActive = true
        public bool IsActive { get; set; } = true;

        public bool IsDefault { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual Venue Venue { get; set; }
        public virtual ICollection<ThemeImage> Images { get; set; } = new List<ThemeImage>();
        public virtual ICollection<ThemePricing> Pricings { get; set; } = new List<ThemePricing>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}