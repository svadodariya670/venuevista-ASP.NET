using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VenueBooking.Models
{
    public class VenueAmenity
    {
        [Key]
        public int VenueAmenityId { get; set; }

        [Required]
        [ForeignKey(nameof(Venue))]
        public int VenueId { get; set; }

        [Required]
        [ForeignKey(nameof(Amenity))]
        public int AmenityId { get; set; }

        public bool IsChargeable { get; set; } = false;

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Price { get; set; }

        [StringLength(255)]
        public string Notes { get; set; }

        // Navigation
        public virtual Venue Venue { get; set; }
        public virtual Amenity Amenity { get; set; }
    }
}