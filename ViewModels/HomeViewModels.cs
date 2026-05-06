namespace VenueBooking.ViewModels
{
    public class HomePageViewModel
    {
        public List<VenueCardViewModel> FeaturedVenues { get; set; } = [];
        public HomeStatsViewModel Stats { get; set; } = new();
        public List<VenueTypeSummary> VenueTypes { get; set; } = [];
    }

    public class HomeStatsViewModel
    {
        public int TotalVenues { get; set; }
        public int TotalBookings { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalCities { get; set; }
    }

    public class VenueTypeSummary
    {
        public string VenueType { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class VenueCardViewModel
    {
        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string VenueType { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string CoverImageUrl { get; set; } = "/images/default-venue.jpg";
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public decimal StartingPrice { get; set; }
        public string Slug { get; set; } = string.Empty;
    }

    public class VenueSearchViewModel
    {
        public List<VenueCardViewModel> Venues { get; set; } = [];
        public string? City { get; set; }
        public string? VenueType { get; set; }
        public int? Guests { get; set; }
        public string? Date { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalResults { get; set; }
        public List<string> AvailableTypes { get; set; } = [];
        public List<string> AvailableCities { get; set; } = [];
    }
}