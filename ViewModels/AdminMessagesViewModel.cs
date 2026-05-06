namespace VenueBooking.ViewModels
{
    public class AdminMessagesViewModel
    {
        public int TotalMessages { get; set; }
        public int UnreadMessages { get; set; }
        public int TodayMessages { get; set; }
        public int UniqueConversations { get; set; }
        public List<AdminMessageListItemViewModel> Messages { get; set; } = new();
    }

    public class AdminMessageListItemViewModel
    {
        public int MessageId { get; set; }
        public int BookingId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderRole { get; set; } = string.Empty;
        public int ReceiverId { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime SentAt { get; set; }
        public string SenderInitials { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty;
    }

    public class AdminConversationViewModel
    {
        public int BookingId { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public List<AdminMessageListItemViewModel> Messages { get; set; } = new();
    }
}