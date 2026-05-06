using System.ComponentModel.DataAnnotations;

namespace VenueBooking.Models
{
    public class Amenity
    {
        [Key]
        public int AmenityId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(50)]
        public string Icon { get; set; }

        [StringLength(50)]
        public string Category { get; set; }

        // Navigation
        public virtual ICollection<VenueAmenity> VenueAmenities { get; set; } = new List<VenueAmenity>();
    }
}