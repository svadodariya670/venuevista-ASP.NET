// ViewModels/AdminSettingsViewModel.cs
namespace VenueBooking.ViewModels
{
    public class AdminSettingsViewModel
    {
        // General
        public string PlatformName { get; set; } = "Venue Vista";
        public string SupportEmail { get; set; } = "support@venuevista.com";
        public string Timezone { get; set; } = "UTC";
        public bool MaintenanceMode { get; set; }

        // Email
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string SmtpEncryption { get; set; } = "TLS";
        public string SmtpUsername { get; set; } = "";
        public string SmtpPassword { get; set; } = "";

        // Payment
        public bool StripeEnabled { get; set; } = true;
        public bool PayPalEnabled { get; set; } = false;

        // Security
        public bool RequireTwoFactor { get; set; }
        public bool LoginNotifications { get; set; } = true;
        public int SessionTimeoutMinutes { get; set; } = 30;

        // Notifications
        public bool NotifyNewBooking { get; set; } = true;
        public bool NotifyNewUser { get; set; }
        public bool NotifyPayment { get; set; } = true;
    }
}