namespace VenueBooking.ViewModels
{
    public class ThemeViewModel
    {
        public int ThemeId { get; set; }
        public string? ThemeName { get; set; }
        public string? Description { get; set; }
        public decimal? PricePerDay { get; set; }
        public string? ImageUrl { get; set; }
    }
}