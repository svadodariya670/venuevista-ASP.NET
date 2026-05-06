using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VenueBooking.Models.Enums;

namespace VenueBooking.Models
{
    /// <summary>
    /// Represents a user in the system (Customer, Owner, or Admin).
    /// Tracks profile information, authentication data, and account status.
    /// </summary>
    public class User
    {
        [Key]
        public int UserId { get; set; }

        /// <summary>
        /// Full name of the user.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Unique email address, used for login and notifications.
        /// </summary>
        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; }

        /// <summary>
        /// Hashed password for secure authentication.
        /// </summary>
        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; }

        /// <summary>
        /// Contact phone number.
        /// </summary>
        [StringLength(20)]
        public string Phone { get; set; }

        /// <summary>
        /// User role (e.g., Customer, Owner, Admin). Defaults to Customer.
        /// </summary>
        [Required]
        public string Role { get; set; } = UserRole.Customer;

        /// <summary>
        /// Indicates if the account is currently enabled and allowed to log in.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Date and time when the user account was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Timestamp of the user's most recent login.
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        // ===== FORM ONLY PROPERTIES (NOT STORED IN DATABASE) =====

        /// <summary>
        /// Plain-text password used only during registration or password change forms.
        /// </summary>
        [NotMapped]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        /// <summary>
        /// Confirmation field to ensure the user typed their password correctly.
        /// </summary>
        [NotMapped]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }

        /// <summary>
        /// Tracks user's acceptance of terms and conditions during registration.
        /// </summary>
        [NotMapped]
        public bool AcceptTerms { get; set; }

        // ===== NAVIGATION PROPERTIES =====

        public virtual ICollection<Venue> Venues { get; set; } = new List<Venue>();
        public virtual ICollection<OwnerSubscription> Subscriptions { get; set; } = new List<OwnerSubscription>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
        public virtual ICollection<VenueBlockedDate> BlockedDatesCreated { get; set; } = new List<VenueBlockedDate>();
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
