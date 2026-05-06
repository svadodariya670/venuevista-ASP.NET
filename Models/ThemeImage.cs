using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VenueBooking.Models
{
    public class ThemeImage
    {
        [Key]
        public int ThemeImageId { get; set; }

        [Required]
        [ForeignKey(nameof(Theme))]
        public int ThemeId { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; }

        [StringLength(200)]
        public string Caption { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual Theme Theme { get; set; }

        public bool IsCover { get; set; } = false;
    }
}