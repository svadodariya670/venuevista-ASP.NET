// ViewModels/AdminUsersViewModel.cs
namespace VenueBooking.ViewModels
{
    public class AdminUsersViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalOwners { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalAdmins { get; set; }
        public int ActiveUsers { get; set; }
        public List<UserListItemViewModel> Users { get; set; } = new();
    }

    public class UserListItemViewModel
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public string Initials => GetInitialsSafe(Name);

        private static string GetInitialsSafe(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "??";

            var parts = name.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 2 && parts[0].Length > 0 && parts[1].Length > 0)
            {
                return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
            }

            if (parts.Length > 0 && parts[0].Length > 0)
            {
                return parts[0].Length >= 2
                    ? parts[0].Substring(0, 2).ToUpper()
                    : parts[0].ToUpper();
            }

            return "??";
        }
    }
}
