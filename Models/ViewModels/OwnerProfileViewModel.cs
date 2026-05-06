using System.ComponentModel.DataAnnotations;

namespace VenueBooking.ViewModels
{
    public class OwnerSettingsViewModel
    {
        // Profile Info
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }

        // Read-only info
        public string Role { get; set; } = "Owner";
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsVerified { get; set; }

        // Password Change
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
        public string? ConfirmPassword { get; set; }

        // Notification Preferences
        public bool EmailNotifications { get; set; } = true;
        public bool BookingAlerts { get; set; } = true;
        public bool ReviewAlerts { get; set; } = true;
        public bool MessageAlerts { get; set; } = true;
        public bool MarketingEmails { get; set; } = false;
    }
}