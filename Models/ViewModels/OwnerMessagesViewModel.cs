// File: ViewModels/OwnerMessagesViewModel.cs
using System;
using System.Collections.Generic;

namespace VenueBooking.ViewModels
{
    // Main ViewModel for the Messages page
    public class OwnerMessagesViewModel
    {
        public List<OwnerConversationViewModel> Conversations { get; set; }
        public int? SelectedCustomerId { get; set; }
        public OwnerConversationDetailViewModel SelectedConversation { get; set; }

        public OwnerMessagesViewModel()
        {
            Conversations = new List<OwnerConversationViewModel>();
        }
    }

    // Conversation list item
    public class OwnerConversationViewModel
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
        public bool IsCustomerOnline { get; set; }
        public string VenueName { get; set; }
        public int? BookingId { get; set; }
    }

    // Detail view for selected conversation
    public class OwnerConversationDetailViewModel
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public bool IsCustomerOnline { get; set; }

        // Use fully qualified name to avoid conflicts
        public List<VenueBooking.ViewModels.MessageViewModel> Messages { get; set; }

        public bool HasBooking { get; set; }
        public string VenueName { get; set; }
        public int? BookingId { get; set; }
        public string EventType { get; set; }
        public DateTime? EventDate { get; set; }
        public string BookingStatus { get; set; }

        public OwnerConversationDetailViewModel()
        {
            Messages = new List<VenueBooking.ViewModels.MessageViewModel>();
        }
    }

    // Individual message item - explicitly in ViewModels namespace
    public class MessageViewModel
    {
        public int MessageId { get; set; }
        public int SenderId { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsFromCurrentUser { get; set; }
    }
}