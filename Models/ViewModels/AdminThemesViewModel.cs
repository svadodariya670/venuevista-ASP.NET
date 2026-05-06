// ViewModels/AdminThemesViewModel.cs
namespace VenueBooking.ViewModels
{
    public class AdminThemesViewModel
    {
        public int TotalThemes { get; set; }
        public int ActiveThemes { get; set; }
        public int TotalCategories { get; set; }
        public int VenuesUsingThemes { get; set; }
        public List<string> Categories { get; set; } = new();
        public List<ThemeListItemViewModel> Themes { get; set; } = new(); // Uses your existing class
    }
}