using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VenueBooking.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        [ForeignKey(nameof(Booking))]
        public int BookingId { get; set; }

        [Required]
        [ForeignKey(nameof(User))]
        public int UserId { get; set; } // Who made the payment

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }

        [StringLength(20)]
        public string PaymentMethod { get; set; } // CreditCard, UPI, NetBanking, etc.

        [StringLength(50)]
        public string TransactionId { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = VenueBooking.Models.Enums.PaymentStatus.Pending;

        [StringLength(100)]
        public string GatewayResponse { get; set; }

        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual Booking Booking { get; set; }
        public virtual User User { get; set; }
    }
}