using System;
using System.Collections.Generic;

namespace VenueBooking.ViewModels
{
    public class AdminReviewsViewModel
    {
        public int TotalReviews { get; set; }
        public int ApprovedReviews { get; set; }
        public int PendingReviews { get; set; }
        public double AverageRating { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; } = new();
        public List<ReviewListItemViewModel> Reviews { get; set; } = new();
    }
}