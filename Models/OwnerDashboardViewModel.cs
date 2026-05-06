namespace VenueBooking.ViewModels
{
    public class OwnerDashboardViewModel
    {
        public string OwnerName { get; set; }
        public int TotalVenues { get; set; }
        public int NewVenuesThisMonth { get; set; }
        public int ActiveBookings { get; set; }
        public int PendingBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public int RevenueGrowth { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public string SubscriptionPlanName { get; set; }
        public int CurrentVenueCount { get; set; }
        public int MaxVenuesAllowed { get; set; }
        public double VenueUsagePercentage { get; set; }
        public int SubscriptionDaysLeft { get; set; }
        public List<BookingListItemViewModel> RecentBookings { get; set; }
    }

    public class BookingListItemViewModel
    {
        public int BookingId { get; set; }
        public string CustomerName { get; set; }
        public string VenueName { get; set; }
        public string EventType { get; set; }
        public DateTime StartDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
    }
}