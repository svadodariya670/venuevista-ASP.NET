using System;
using System.Collections.Generic;

namespace VenueBooking.ViewModels
{
    public class OwnerBlockedDateItem
    {
        public int Id { get; set; }
        public DateTime BlockDate { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class OwnerBlockDatesViewModel
    {
        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public List<OwnerBlockedDateItem> BlockedDates { get; set; } = new();
    }
}