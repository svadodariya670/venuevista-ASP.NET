namespace VenueBooking.ViewModels
{
    public class CustomerDashboardViewModel
    {
        public int TotalBookings { get; set; }
        public int UpcomingBookings { get; set; }
        public int PendingBookings { get; set; }
        public int CompletedBookings { get; set; }
        public decimal TotalSpent { get; set; }
        public List<CustomerBookingListItem> RecentBookings { get; set; } = [];
        public int UnreadNotifications { get; set; }
        public int UnreadMessages { get; set; }
    }

    public class CustomerBookingListItem
    {
        public int BookingId { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string VenueCity { get; set; } = string.Empty;
        public string CoverImageUrl { get; set; } = "/images/default-venue.jpg";
        public string ThemeName { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CustomerBookingsViewModel
    {
        public List<CustomerBookingListItem> Bookings { get; set; } = [];
        public string CurrentFilter { get; set; } = "All";
        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int ConfirmedCount { get; set; }
        public int CancelledCount { get; set; }
    }

    public class CreateBookingViewModel
    {
        public int VenueId { get; set; }
        public int ThemeId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? SpecialRequests { get; set; }
    }

    public class CustomerConversationViewModel
    {
        public int OtherUserId { get; set; }
        public string OtherUserName { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
        public bool IsSelected { get; set; }
    }

    public class CustomerMessageViewModel
    {
        public int MessageId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsFromMe { get; set; }
    }

    public class CustomerMessagesPageViewModel
    {
        public List<CustomerConversationViewModel> Conversations { get; set; } = [];
        public int? SelectedOwnerId { get; set; }
        public string? SelectedOwnerName { get; set; }
        public List<CustomerMessageViewModel> Messages { get; set; } = [];
    }

    public class CustomerSettingsViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }
}