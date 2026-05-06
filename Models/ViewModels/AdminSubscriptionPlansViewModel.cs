// ViewModels/AdminSubscriptionPlansViewModel.cs
namespace VenueBooking.ViewModels
{
    public class AdminSubscriptionPlansViewModel
    {
        public int TotalPlans { get; set; }
        public int ActivePlans { get; set; }
        public int TotalSubscribers { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public List<SubscriptionPlanViewModel> Plans { get; set; } = new();
    }

    public class SubscriptionPlanViewModel
    {
        public int PlanId { get; set; }
        public string PlanName { get; set; }
        public decimal Price { get; set; }
        public int DurationInDays { get; set; }
        public int MaxVenuesAllowed { get; set; }
        public string Description { get; set; }
        public string Features { get; set; }
        public bool IsActive { get; set; }
        public int SubscriberCount { get; set; }
    }
}