using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VenueBooking.Models
{
    public class VenueImage
    {
        [Key]
        public int ImageId { get; set; }

        [Required]
        [ForeignKey(nameof(Venue))]
        public int VenueId { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; }

        [StringLength(200)]
        public string Caption { get; set; }

        public bool IsCover { get; set; } = false;

        public int DisplayOrder { get; set; } = 0;

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual Venue Venue { get; set; }
    }
}