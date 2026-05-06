namespace VenueBooking.ViewModels
{
    public class AdminAmenitiesViewModel
    {
        public int TotalAmenities { get; set; }
        public int TotalCategories { get; set; }
        public int ChargeableAmenities { get; set; }
        public int VenuesUsingAmenities { get; set; }
        public List<string> Categories { get; set; } = new();
        public List<AmenityListItemViewModel> Amenities { get; set; } = new();
    }

    public class AmenityListItemViewModel
    {
        public int AmenityId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int VenueCount { get; set; }
    }
}