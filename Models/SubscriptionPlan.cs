using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VenueBooking.Models
{
    public class SubscriptionPlan
    {
        [Key]
        public int PlanId { get; set; }

        [Required]
        [StringLength(50)]
        public string PlanName { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Required]
        public int DurationInDays { get; set; }

        [Required]
        public int MaxVenuesAllowed { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public string Features { get; set; } // JSON string

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual ICollection<OwnerSubscription> Subscriptions { get; set; } = new List<OwnerSubscription>();
    }
}