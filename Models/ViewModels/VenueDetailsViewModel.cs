using System;
using System.Collections.Generic;

namespace VenueBooking.ViewModels
{
    public class VenueDetailsViewModel
    {
        public int VenueId { get; set; }
        public string VenueName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int Capacity { get; set; }
        public string Description { get; set; }
        public string ContactPhone { get; set; }
        public string VenueType { get; set; }
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Images
        public List<VenueImageViewModel> Images { get; set; } = new();

        // Themes with pricing
        public List<ThemeDetailsViewModel> Themes { get; set; } = new();
    }

    public class VenueImageViewModel
    {
        public int ImageId { get; set; }
        public string ImageUrl { get; set; }
        public string Caption { get; set; }
        public bool IsCover { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class ThemeDetailsViewModel
    {
        public int ThemeId { get; set; }
        public string ThemeName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }

        // Pricing information (take first active pricing, or any)
        public decimal? Price { get; set; }
        public string EventType { get; set; }
    }
}