// ViewModels/AdminDashboardViewModel.cs
namespace VenueBooking.ViewModels
{
    public class AdminDashboardViewModel
    {
        // Stats Cards
        public int TotalUsers { get; set; }
        public int TotalOwners { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalVenues { get; set; }
        public int PendingVenues { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingApprovals { get; set; }
        public int UnreadMessages { get; set; }

        // Charts Data
        public List<RevenueDataPoint> RevenueLast7Days { get; set; } = new();
        public List<BookingTypeDistribution> BookingTypes { get; set; } = new();

        // Recent Data
        public List<RecentBookingViewModel> RecentBookings { get; set; } = new();
        public List<NewUserViewModel> NewestUsers { get; set; } = new();
        public List<PendingVenueViewModel> PendingVenuesList { get; set; } = new();
    }

    public class RevenueDataPoint
    {
        public string Day { get; set; }
        public decimal Amount { get; set; }
    }

    public class BookingTypeDistribution
    {
        public string EventType { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class RecentBookingViewModel
    {
        public int BookingId { get; set; }
        public string CustomerName { get; set; }
        public string VenueName { get; set; }
        public DateTime EventDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
        public string CustomerInitials { get; set; }
    }

    public class NewUserViewModel
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; }
    }

    public class PendingVenueViewModel
    {
        public int VenueId { get; set; }
        public string VenueName { get; set; }
        public string OwnerName { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}