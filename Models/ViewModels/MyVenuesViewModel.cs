// ViewModels/MyVenuesViewModel.cs (OWNER SIDE - Keep as is)
namespace VenueBooking.ViewModels
{
    public class MyVenuesViewModel
    {
        public List<OwnerVenueListItemViewModel> Venues { get; set; }  // Renamed
        public int TotalVenues { get; set; }
        public int MaxVenuesAllowed { get; set; }
        public bool CanAddMoreVenues => TotalVenues < MaxVenuesAllowed;
        public string CurrentPlanName { get; set; }
        public int SubscriptionDaysLeft { get; set; }
    }

    public class OwnerVenueListItemViewModel  // Renamed from VenueListItemViewModel
    {
        public int VenueId { get; set; }
        public string VenueName { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public int Capacity { get; set; }
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
        public string CoverImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ThemeCount { get; set; }
        public int BookingCount { get; set; }
    }
}