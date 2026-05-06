// ViewModels for Messages page (can be placed in a separate folder, e.g., ViewModels/)
public class MessagesViewModel
{
    public List<ConversationViewModel> Conversations { get; set; }
    public int? SelectedBookingId { get; set; }
    public ConversationDetailViewModel SelectedConversation { get; set; }
}

public class ConversationViewModel
{
    public int BookingId { get; set; }
    public int OtherUserId { get; set; }
    public string OtherUserName { get; set; }
    public string OtherUserAvatar { get; set; }
    public string VenueName { get; set; }
    public string LastMessage { get; set; }
    public DateTime LastMessageTime { get; set; }
    public int UnreadCount { get; set; }
}

public class ConversationDetailViewModel
{
    public int BookingId { get; set; }
    public string OtherUserName { get; set; }
    public string OtherUserAvatar { get; set; }
    public bool IsOnline { get; set; } // optional – you can implement real online status later
    public string VenueName { get; set; }
    public List<MessageViewModel> Messages { get; set; }
}

public class MessageViewModel
{
    public int MessageId { get; set; }
    public int SenderId { get; set; }
    public string Content { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsFromCurrentUser { get; set; }
}