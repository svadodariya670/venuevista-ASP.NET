namespace VenueBooking.ViewModels
{
    public class BrowseVenuesViewModel
    {
        public List<VenueBrowseItemViewModel> Venues { get; set; } = new List<VenueBrowseItemViewModel>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        // Filters
        public string SearchCity { get; set; }
        public string VenueType { get; set; }
        public int? MinCapacity { get; set; }
        public int? MaxCapacity { get; set; }
        public string SortBy { get; set; } = "newest";
    }

    public class VenueBrowseItemViewModel
    {
        public int VenueId { get; set; }
        public string VenueName { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string VenueType { get; set; }
        public int Capacity { get; set; }
        public string CoverImageUrl { get; set; }
        public decimal StartingPrice { get; set; }
        public string ActiveThemeName { get; set; }
        public bool IsVerified { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }
}