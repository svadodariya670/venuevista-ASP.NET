using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VenueBooking.Models.Enums;

namespace VenueBooking.Models
{
    public class ThemePricing
    {
        [Key]
        public int PricingId { get; set; }

        [Required]
        [ForeignKey(nameof(Theme))]
        public int ThemeId { get; set; }

        [Required]
        public string EventType { get; set; } = EventTypes.Marriage;  // enum instead of string

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal PricePerDay { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual Theme Theme { get; set; }
    }
}