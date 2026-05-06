// ViewModels/AdminBookingsViewModel.cs
namespace VenueBooking.ViewModels
{
    public class AdminBookingsViewModel
    {
        public int TotalBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int PendingBookings { get; set; }
        public int ThisMonthBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<AdminBookingListItemViewModel> Bookings { get; set; } = new();
    }

    public class AdminBookingListItemViewModel
    {
        public int BookingId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerInitials => GetInitialsSafe(CustomerName);
        public string VenueName { get; set; }
        public string OwnerName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string EventType { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; }

        private static string GetInitialsSafe(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "??";
            var parts = name.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && parts[0].Length > 0 && parts[1].Length > 0)
                return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
            if (parts.Length > 0 && parts[0].Length > 0)
                return parts[0].Length >= 2 ? parts[0].Substring(0, 2).ToUpper() : parts[0].ToUpper();
            return "??";
        }
    }
}