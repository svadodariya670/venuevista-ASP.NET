using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VenueBooking.Models
{
    public class OwnerSubscription
    {
        [Key]
        public int SubscriptionId { get; set; }

        [Required]
        [ForeignKey(nameof(Owner))]
        public int OwnerId { get; set; }

        [Required]
        [ForeignKey(nameof(Plan))]
        public int PlanId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [StringLength(20)]
        public string PaymentStatus { get; set; } = VenueBooking.Models.Enums.PaymentStatus.Pending;

        [StringLength(100)]
        public string TransactionId { get; set; }

        public int MaxVenuesAllowed { get; set; }

        public int CurrentVenueCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual User Owner { get; set; }
        public virtual SubscriptionPlan Plan { get; set; }
    }
}