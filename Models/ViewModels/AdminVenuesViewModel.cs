// ViewModels/AdminVenuesViewModel.cs (ADMIN SIDE - New)
namespace VenueBooking.ViewModels
{
    public class AdminVenuesViewModel
    {
        public int TotalVenues { get; set; }
        public int VerifiedVenues { get; set; }
        public int PendingVenues { get; set; }
        public int InactiveVenues { get; set; }
        public List<AdminVenueListItemViewModel> Venues { get; set; } = new();  // Different name
    }

    public class AdminVenueListItemViewModel  // Different name
    {
        public int VenueId { get; set; }
        public string VenueName { get; set; }
        public string VenueType { get; set; }
        public int Capacity { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public bool IsVerified { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string OwnerName { get; set; }
        public string OwnerEmail { get; set; }
    }
}