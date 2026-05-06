using System.ComponentModel.DataAnnotations;

namespace VenueBooking.ViewModels
{
    public class BookingRequestViewModel
    {
        public int VenueId { get; set; }
        public string VenueName { get; set; } = "";
        public string VenueImage { get; set; } = "";
        public string VenueCity { get; set; } = "";
        public int Capacity { get; set; }

        public List<ThemeViewModel> Themes { get; set; } = new();

        [Required(ErrorMessage = "Please select a date")]
        [DataType(DataType.Date)]
        public DateTime EventDate { get; set; }

        [Required(ErrorMessage = "Please select a theme")]
        public int ThemeId { get; set; }

        [Required(ErrorMessage = "Please select event type")]
        public string EventType { get; set; } = "";

        [Required(ErrorMessage = "Number of guests is required")]
        [Range(1, 10000)]
        public int GuestCount { get; set; }

        [StringLength(1000)]
        public string SpecialRequests { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = "";

        [Required]
        [EmailAddress]
        public string CustomerEmail { get; set; } = "";

        [Required]
        [Phone]
        public string CustomerPhone { get; set; } = "";
    }
}