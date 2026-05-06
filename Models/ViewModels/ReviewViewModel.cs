using System.ComponentModel.DataAnnotations;

namespace VenueBooking.ViewModels
{
    public class ReviewListItemViewModel
    {
        public int ReviewId { get; set; }
        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerAvatar { get; set; }
        public int BookingId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo => GetTimeAgo(CreatedAt);

        // Owner Reply
        public int? ReplyId { get; set; }
        public string? ReplyContent { get; set; }
        public DateTime? RepliedAt { get; set; }

        private static string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalDays > 365)
                return $"{(int)(timeSpan.TotalDays / 365)}y ago";
            if (timeSpan.TotalDays > 30)
                return $"{(int)(timeSpan.TotalDays / 30)}mo ago";
            if (timeSpan.TotalDays > 1)
                return $"{(int)timeSpan.TotalDays}d ago";
            if (timeSpan.TotalHours > 1)
                return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalMinutes > 1)
                return $"{(int)timeSpan.TotalMinutes}m ago";

            return "Just now";
        }
    }

    public class ReviewReplyViewModel
    {
        [Required]
        public int ReviewId { get; set; }

        [Required(ErrorMessage = "Reply content is required")]
        [StringLength(1000, ErrorMessage = "Reply cannot exceed 1000 characters")]
        public string ReplyContent { get; set; } = string.Empty;
    }

    public class ReviewsDashboardViewModel
    {
        public List<ReviewListItemViewModel> Reviews { get; set; } = new();
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int PendingReplyCount { get; set; }
        public int? FilterRating { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}