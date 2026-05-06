using System;
using System.Collections.Generic;

namespace VenueBooking.ViewModels
{
    public class SubscriptionViewModel
    {
        public string CurrentPlanName { get; set; }
        public DateTime? NextBillingDate { get; set; }
        public bool IsActive { get; set; }
        public int DaysLeft { get; set; }
        public List<PlanViewModel> Plans { get; set; } = new();
        public List<PaymentHistoryViewModel> Payments { get; set; } = new();
    }

    public class PlanViewModel
    {
        public int PlanId { get; set; }
        public string PlanName { get; set; }
        public decimal Price { get; set; }
        public int DurationInDays { get; set; }
        public int MaxVenuesAllowed { get; set; }
        public string Description { get; set; }
        public List<string> FeaturesList { get; set; } = new();
        public bool IsCurrent { get; set; }
        public bool IsPopular { get; set; }
    }

    public class PaymentHistoryViewModel
    {
        public DateTime PaymentDate { get; set; }
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string ReceiptUrl { get; set; }
    }
}