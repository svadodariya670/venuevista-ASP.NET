using System.ComponentModel.DataAnnotations;

namespace VenueBooking.ViewModels
{
    public class ThemePricingEditViewModel
    {
        public int PricingId { get; set; }

        [Required]
        public string EventType { get; set; }

        [Range(0, 99999999)]
        public decimal PricePerDay { get; set; }

        public bool IsActive { get; set; }
    }
}