using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace VenueBooking.ViewModels
{
    public class CreateThemeViewModel
    {
        public int VenueId { get; set; }
        public int ThemeId { get; set; }
        public string? ThemeName { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }

        // Controller expects uploaded files
        public List<IFormFile> Images { get; set; } = new();

        // Controller expects objects with EventType, PricePerDay and IsActive
        public List<ThemePricingInputModel> Pricings { get; set; } = new();
    }

    public class ThemePricingInputModel
    {
        public string EventType { get; set; } = string.Empty;
        public decimal PricePerDay { get; set; }
        public bool IsActive { get; set; } = true;
    }
}