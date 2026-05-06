namespace VenueBooking.ViewModels
{
    public class ThemeListItemViewModel
    {
        public int ThemeId { get; set; }
        public string ThemeName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string CoverImageUrl { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public string EventType { get; set; } = string.Empty;

        // All active pricings for this theme
        public List<ThemePricingDisplay> Pricings { get; set; } = new();
    }

    public class ThemePricingDisplay
    {
        public string EventType { get; set; } = string.Empty;
        public decimal PricePerDay { get; set; }
    }
}