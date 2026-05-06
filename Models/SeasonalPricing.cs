using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VenueBooking.Models
{
    public class SeasonalPricing
    {
        [Key]
        public int SeasonalPricingId { get; set; }

        [Required]
        [ForeignKey(nameof(Venue))]
        public int VenueId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal PriceMultiplier { get; set; } = 1.00m;

        [StringLength(100)]
        public string Reason { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual Venue Venue { get; set; }
    }
}