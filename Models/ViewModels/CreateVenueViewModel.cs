using System.ComponentModel.DataAnnotations;

namespace VenueBooking.ViewModels
{
    public class CreateVenueViewModel
    {
        [Required(ErrorMessage = "Venue name is required")]
        [StringLength(150, ErrorMessage = "Name cannot exceed 150 characters")]
        public string VenueName { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [StringLength(255, ErrorMessage = "Address cannot exceed 255 characters")]
        public string Address { get; set; }

        [Required(ErrorMessage = "City is required")]
        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        public string City { get; set; }

        [StringLength(100, ErrorMessage = "State cannot exceed 100 characters")]
        public string State { get; set; }

        [StringLength(100)]
        public string Country { get; set; } = "India";

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        [Required(ErrorMessage = "Capacity is required")]
        [Range(1, 10000, ErrorMessage = "Capacity must be between 1 and 10000")]
        public int Capacity { get; set; }

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; }

        [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string ContactPhone { get; set; }

        [Required(ErrorMessage = "Venue type is required")]
        [StringLength(50)]
        public string VenueType { get; set; }

        // Images
        [Required(ErrorMessage = "At least one image is required")]
        public List<IFormFile> Images { get; set; }

        // Amenities - list of selected amenity IDs
        public List<int> SelectedAmenities { get; set; } = new List<int>();

        // Amenity prices (optional)
        public Dictionary<int, decimal?> AmenityPrices { get; set; } = new Dictionary<int, decimal?>();
    }
}