using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VenueBooking.Models
{
    public class Message
    {
        [Key]
        public int MessageId { get; set; }

        [Required]
        [ForeignKey(nameof(Booking))]
        public int BookingId { get; set; }

        [Required]
        [ForeignKey(nameof(Sender))]
        public int SenderId { get; set; }

        [Required]
        [ForeignKey(nameof(Receiver))]
        public int ReceiverId { get; set; }

        [Required]
        public string Content { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime SentAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual Booking Booking { get; set; }
        public virtual User Sender { get; set; }
        public virtual User Receiver { get; set; }
    }
}   