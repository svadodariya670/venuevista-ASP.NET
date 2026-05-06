// ViewModels/AdminAuditLogsViewModel.cs
namespace VenueBooking.ViewModels
{
    public class AdminAuditLogsViewModel
    {
        public int TotalLogs { get; set; }
        public int CreatedLogs { get; set; }
        public int UpdatedLogs { get; set; }
        public int DeletedLogs { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public List<AuditLogListItemViewModel> Logs { get; set; } = new();
    }

    public class AuditLogListItemViewModel
    {
        public int LogId { get; set; }
        public DateTime ChangedAt { get; set; }  // Fixed: was Timestamp
        public string UserName { get; set; }
        public string UserInitials => GetInitialsSafe(UserName);
        public string Action { get; set; }
        public string TableName { get; set; }
        public int RecordId { get; set; }
        public string Changes { get; set; }

        private static string GetInitialsSafe(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "??";
            var parts = name.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && parts[0].Length > 0 && parts[1].Length > 0)
                return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
            if (parts.Length > 0 && parts[0].Length > 0)
                return parts[0].Length >= 2 ? parts[0].Substring(0, 2).ToUpper() : parts[0].ToUpper();
            return "??";
        }
    }
}