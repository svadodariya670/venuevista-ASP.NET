namespace VenueBooking.ViewModels
{
    public class AdminOwnerSubscriptionsViewModel
    {
        public int TotalSubscriptions { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int ExpiredSubscriptions { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<OwnerSubscriptionListItemViewModel> Subscriptions { get; set; } = new();
    }

    public class OwnerSubscriptionListItemViewModel
    {
        public int SubscriptionId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public decimal PlanPrice { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public int MaxVenuesAllowed { get; set; }
        public int CurrentVenueCount { get; set; }
        public bool IsActive { get; set; }
        public bool IsExpired => EndDate < DateTime.Now;
        public int DaysRemaining => IsExpired ? 0 : (EndDate - DateTime.Now).Days;
    }
}