// ViewModels/AdminPaymentsViewModel.cs
namespace VenueBooking.ViewModels
{
    public class AdminPaymentsViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal RefundedAmount { get; set; }
        public List<string> RevenueChartLabels { get; set; } = new();
        public List<decimal> RevenueChartData { get; set; } = new();
        public List<PaymentListItemViewModel> Payments { get; set; } = new();
    }

    public class PaymentListItemViewModel
    {
        public int PaymentId { get; set; }
        public string TransactionId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerInitials => GetInitialsSafe(CustomerName);
        public string PaymentFor { get; set; } // "Booking" or "Subscription"
        public int ReferenceId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
        public DateTime? PaidAt { get; set; }

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