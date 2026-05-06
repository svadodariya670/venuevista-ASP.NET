using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace VenueBooking.ViewModels
{
    public class EditThemeViewModel
    {
        public int ThemeId { get; set; }

        [Required]
        public int VenueId { get; set; }

        [Required(ErrorMessage = "Theme name is required")]
        [StringLength(150)]
        public string ThemeName { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }

        // Pricing list
        public List<ThemePricingEditViewModel> Pricings { get; set; } = new();

        // Existing images shown on page
        public List<ExistingImageViewModel> ExistingImages { get; set; } = new();

        // Images selected for deletion
        public List<int> ImagesToDelete { get; set; } = new();

        // New uploaded images
        public List<IFormFile> NewImages { get; set; } = new();
    }
}