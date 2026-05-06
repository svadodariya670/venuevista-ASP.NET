using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VenueBooking.Data;
using VenueBooking.Models;
using VenueBooking.Models.Enums;
using VenueBooking.ViewModels;

namespace VenueBooking.Controllers
{
    [Authorize(Roles = UserRole.Admin)]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Stats
            var totalUsers = await _context.Users.CountAsync();
            var totalOwners = await _context.Users.CountAsync(u => u.Role == UserRole.Owner);
            var totalCustomers = await _context.Users.CountAsync(u => u.Role == UserRole.Customer);
            var totalVenues = await _context.Venues.CountAsync();
            var pendingVenues = await _context.Venues.CountAsync(v => !v.IsVerified);
            var totalBookings = await _context.Bookings.CountAsync();
            var totalRevenue = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Completed && p.PaidAt.HasValue)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;
            var pendingApprovals = await _context.Venues.CountAsync(v => !v.IsVerified)
                + await _context.Reviews.CountAsync(r => !r.IsApproved);
            var unreadMessages = await _context.Messages
                .CountAsync(m => !m.IsRead && m.ReceiverId == adminId);

            // Recent Bookings
            var recentBookingsRaw = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Customer)
                .Include(b => b.Venue)
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .Select(b => new
                {
                    b.BookingId,
                    CustomerName = b.Customer != null ? b.Customer.Name : "Unknown",
                    VenueName = b.Venue.VenueName,
                    b.StartDate,
                    b.TotalPrice,
                    b.Status
                })
                .ToListAsync();

            var recentBookings = recentBookingsRaw.Select(b => new RecentBookingViewModel
            {
                BookingId = b.BookingId,
                CustomerName = b.CustomerName,
                VenueName = b.VenueName,
                EventDate = b.StartDate,
                TotalPrice = b.TotalPrice,
                Status = b.Status,
                CustomerInitials = GetInitials(b.CustomerName)
            }).ToList();

            // Newest Users
            var newestUsersRaw = await _context.Users
                .AsNoTracking()
                .Where(u => u.Role != UserRole.Admin)
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .Select(u => new
                {
                    u.UserId,
                    u.Name,
                    u.Email,
                    u.Role,
                    u.CreatedAt
                })
                .ToListAsync();

            var newestUsers = newestUsersRaw.Select(u => new NewUserViewModel
            {
                UserId = u.UserId,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role,
                CreatedAt = u.CreatedAt,
                TimeAgo = GetTimeAgo(u.CreatedAt)
            }).ToList();

            var viewModel = new AdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalOwners = totalOwners,
                TotalCustomers = totalCustomers,
                TotalVenues = totalVenues,
                PendingVenues = pendingVenues,
                TotalBookings = totalBookings,
                TotalRevenue = totalRevenue,
                PendingApprovals = pendingApprovals,
                UnreadMessages = unreadMessages,
                RevenueLast7Days = await GetRevenueLast7Days(),
                BookingTypes = await GetBookingTypeDistribution(),
                RecentBookings = recentBookings,
                NewestUsers = newestUsers
            };

            return View(viewModel);
        }

        private async Task<List<RevenueDataPoint>> GetRevenueLast7Days()
        {
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Now.Date.AddDays(-i))
                .Reverse()
                .ToList();

            var revenueData = new List<RevenueDataPoint>();

            foreach (var date in last7Days)
            {
                var amount = await _context.Payments
                    .Where(p => p.Status == PaymentStatus.Completed &&
                               p.PaidAt.HasValue &&
                               p.PaidAt.Value.Date == date)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0;

                revenueData.Add(new RevenueDataPoint
                {
                    Day = date.ToString("ddd"),
                    Amount = amount
                });
            }

            return revenueData;
        }

        private async Task<List<BookingTypeDistribution>> GetBookingTypeDistribution()
        {
            var total = await _context.Bookings.CountAsync();
            if (total == 0) return new List<BookingTypeDistribution>();

            var types = await _context.Bookings
                .GroupBy(b => b.EventType)
                .Select(g => new BookingTypeDistribution
                {
                    EventType = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / total * 100
                })
                .ToListAsync();

            return types;
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "??";
            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2) return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            if (parts.Length == 1 && parts[0].Length >= 2) return parts[0][..2].ToUpper();
            if (parts.Length == 1 && parts[0].Length == 1) return $"{parts[0][0]}".ToUpper();
            return "??";
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var span = DateTime.Now - dateTime;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalHours < 1) return $"{(int)span.TotalMinutes} mins ago";
            if (span.TotalDays < 1) return $"{(int)span.TotalHours} hours ago";
            return $"{(int)span.TotalDays} days ago";
        }

        // ==================== USERS ====================

        public async Task<IActionResult> Users(string role = "All")
        {
            var query = _context.Users.AsNoTracking().AsQueryable();

            if (role != "All")
            {
                query = query.Where(u => u.Role == role);
            }

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new UserListItemViewModel
                {
                    UserId = u.UserId,
                    Name = u.Name,
                    Email = u.Email,
                    Phone = u.Phone,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                })
                .ToListAsync();

            var viewModel = new AdminUsersViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalOwners = await _context.Users.CountAsync(u => u.Role == UserRole.Owner),
                TotalCustomers = await _context.Users.CountAsync(u => u.Role == UserRole.Customer),
                TotalAdmins = await _context.Users.CountAsync(u => u.Role == UserRole.Admin),
                ActiveUsers = await _context.Users.CountAsync(u => u.IsActive),
                Users = users
            };

            return View(viewModel);
        }

        // ==================== VENUES ====================

        public async Task<IActionResult> Venues(string status = "All")
        {
            var query = _context.Venues
                .AsNoTracking()
                .Include(v => v.Owner)
                .AsQueryable();

            if (status == "Verified")
                query = query.Where(v => v.IsVerified && v.IsActive);
            else if (status == "Pending")
                query = query.Where(v => !v.IsVerified);
            else if (status == "Inactive")
                query = query.Where(v => !v.IsActive);

            var venues = await query
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new AdminVenueListItemViewModel
                {
                    VenueId = v.VenueId,
                    VenueName = v.VenueName,
                    VenueType = v.VenueType,
                    Capacity = v.Capacity,
                    City = v.City,
                    State = v.State,
                    Country = v.Country,
                    IsVerified = v.IsVerified,
                    IsActive = v.IsActive,
                    CreatedAt = v.CreatedAt,
                    OwnerName = v.Owner.Name,
                    OwnerEmail = v.Owner.Email
                })
                .ToListAsync();

            var viewModel = new AdminVenuesViewModel
            {
                TotalVenues = await _context.Venues.CountAsync(),
                VerifiedVenues = await _context.Venues.CountAsync(v => v.IsVerified && v.IsActive),
                PendingVenues = await _context.Venues.CountAsync(v => !v.IsVerified),
                InactiveVenues = await _context.Venues.CountAsync(v => !v.IsActive),
                Venues = venues
            };

            return View(viewModel);
        }

        // GET: /Admin/VenueDetails/5
        public async Task<IActionResult> VenueDetails(int id)
        {
            var venue = await _context.Venues
                .AsNoTracking()
                .Include(v => v.Owner)
                .Include(v => v.Images.OrderBy(i => i.DisplayOrder))
                .Include(v => v.VenueAmenities)
                    .ThenInclude(va => va.Amenity)
                .Include(v => v.Themes)
                    .ThenInclude(t => t.Pricings)
                .Include(v => v.Bookings)
                .FirstOrDefaultAsync(v => v.VenueId == id);

            if (venue == null) return NotFound();

            return View(venue);
        }

        // POST: /Admin/VerifyVenue — Approve a pending venue
        [HttpPost]
        public async Task<IActionResult> VerifyVenue(int id)
        {
            var venue = await _context.Venues
                .Include(v => v.Owner)
                .FirstOrDefaultAsync(v => v.VenueId == id);

            if (venue == null) return Json(new { success = false, error = "Venue not found." });

            venue.IsVerified = true;
            venue.IsActive = true;
            venue.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // Notify the owner
            _context.Notifications.Add(new Notification
            {
                UserId = venue.OwnerId,
                Title = "Venue Approved!",
                Message = $"Your venue \"{venue.VenueName}\" has been verified and is now live for customers.",
                Type = "Venue",
                IsRead = false,
                ActionUrl = $"/Owner/VenueDetails/{venue.VenueId}",
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: /Admin/RejectVenue — Reject a pending venue with reason
        [HttpPost]
        public async Task<IActionResult> RejectVenue([FromBody] RejectVenueRequest request)
        {
            var venue = await _context.Venues
                .Include(v => v.Owner)
                .FirstOrDefaultAsync(v => v.VenueId == request.VenueId);

            if (venue == null) return Json(new { success = false, error = "Venue not found." });

            venue.IsVerified = false;
            venue.IsActive = false;
            venue.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // Notify the owner with the rejection reason
            _context.Notifications.Add(new Notification
            {
                UserId = venue.OwnerId,
                Title = "Venue Rejected",
                Message = $"Your venue \"{venue.VenueName}\" was not approved. Reason: {request.Reason}",
                Type = "Venue",
                IsRead = false,
                ActionUrl = $"/Owner/EditVenue/{venue.VenueId}",
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: /Admin/ToggleVenueActive — Activate or deactivate a venue
        [HttpPost]
        public async Task<IActionResult> ToggleVenueActive(int id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null) return Json(new { success = false });

            venue.IsActive = !venue.IsActive;
            venue.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isActive = venue.IsActive });
        }

        // POST: /Admin/DeleteVenue
        [HttpPost]
        public async Task<IActionResult> DeleteVenue(int id)
        {
            var venue = await _context.Venues
                .Include(v => v.Bookings)
                .FirstOrDefaultAsync(v => v.VenueId == id);

            if (venue == null) return Json(new { success = false, error = "Venue not found." });

            // Prevent deletion if there are future confirmed bookings
            var hasActiveBookings = venue.Bookings.Any(b =>
                b.Status == BookingStatus.Confirmed && b.StartDate >= DateTime.Now);

            if (hasActiveBookings)
                return Json(new { success = false, error = "Cannot delete venue with active future bookings." });

            _context.Venues.Remove(venue);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        public class RejectVenueRequest
        {
            public int VenueId { get; set; }
            public string Reason { get; set; } = string.Empty;
        }

        // ==================== BOOKINGS ====================

        public async Task<IActionResult> Bookings(string status = "All")
        {
            var query = _context.Bookings
                .AsNoTracking()
                .Include(b => b.Customer)
                .Include(b => b.Venue)
                .ThenInclude(v => v.Owner)
                .Include(b => b.Payments)
                .AsQueryable();

            if (status != "All")
                query = query.Where(b => b.Status == status);

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .Take(50)
                .ToListAsync();

            var bookingList = bookings.Select(b => new AdminBookingListItemViewModel
            {
                BookingId = b.BookingId,
                CustomerName = b.Customer?.Name ?? "Unknown",
                CustomerEmail = b.Customer?.Email ?? "",
                VenueName = b.Venue?.VenueName ?? "Unknown",
                OwnerName = b.Venue?.Owner?.Name ?? "Unknown",
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                EventType = b.EventType,
                TotalPrice = b.TotalPrice,
                Status = b.Status,
                PaymentStatus = b.Payments.OrderByDescending(p => p.PaidAt).FirstOrDefault()?.Status ?? "Pending",
                CreatedAt = b.CreatedAt
            }).ToList();

            var viewModel = new AdminBookingsViewModel
            {
                TotalBookings = await _context.Bookings.CountAsync(),
                ConfirmedBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Confirmed),
                PendingBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Pending),
                ThisMonthBookings = await _context.Bookings.CountAsync(b => b.CreatedAt.Month == DateTime.Now.Month),
                TotalRevenue = await _context.Bookings.Where(b => b.Status != BookingStatus.Cancelled).SumAsync(b => (decimal?)b.TotalPrice) ?? 0,
                Bookings = bookingList
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBookingStatus([FromBody] UpdateStatusRequest request)
        {
            var booking = await _context.Bookings.FindAsync(request.BookingId);
            if (booking == null) return Json(new { success = false });

            booking.Status = request.Status;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        public class UpdateStatusRequest
        {
            public int BookingId { get; set; }
            public string Status { get; set; }
        }

        // ==================== SUBSCRIPTIONS ====================

        public async Task<IActionResult> SubscriptionPlans()
        {
            var plans = await _context.SubscriptionPlans
                .AsNoTracking()
                .Select(p => new SubscriptionPlanViewModel
                {
                    PlanId = p.PlanId,
                    PlanName = p.PlanName,
                    Price = p.Price,
                    DurationInDays = p.DurationInDays,
                    MaxVenuesAllowed = p.MaxVenuesAllowed,
                    Description = p.Description,
                    Features = p.Features,
                    IsActive = p.IsActive,
                    SubscriberCount = _context.OwnerSubscriptions.Count(s => s.PlanId == p.PlanId && s.IsActive)
                })
                .ToListAsync();

            var viewModel = new AdminSubscriptionPlansViewModel
            {
                TotalPlans = plans.Count,
                ActivePlans = plans.Count(p => p.IsActive),
                TotalSubscribers = plans.Sum(p => p.SubscriberCount),
                MonthlyRevenue = await _context.OwnerSubscriptions
                    .Where(s => s.IsActive && s.PaymentStatus == PaymentStatus.Completed)
                    .SumAsync(s => (decimal?)s.Plan.Price) ?? 0,
                Plans = plans
            };

            return View(viewModel);
        }

        // POST: /Admin/CreateSubscriptionPlan — Add a new subscription plan
        [HttpPost]
        public async Task<IActionResult> CreateSubscriptionPlan([FromBody] CreatePlanRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PlanName))
                return Json(new { success = false, error = "Plan name is required." });

            if (request.Price <= 0)
                return Json(new { success = false, error = "Price must be greater than zero." });

            if (request.DurationInDays <= 0)
                return Json(new { success = false, error = "Duration must be greater than zero." });

            if (request.MaxVenuesAllowed <= 0)
                return Json(new { success = false, error = "Max venues must be greater than zero." });

            var exists = await _context.SubscriptionPlans.AnyAsync(p => p.PlanName == request.PlanName.Trim());
            if (exists)
                return Json(new { success = false, error = "A plan with this name already exists." });

            var plan = new SubscriptionPlan
            {
                PlanName = request.PlanName.Trim(),
                Price = request.Price,
                DurationInDays = request.DurationInDays,
                MaxVenuesAllowed = request.MaxVenuesAllowed,
                Description = request.Description?.Trim() ?? "",
                Features = request.Features?.Trim() ?? "[]",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.SubscriptionPlans.Add(plan);
            await _context.SaveChangesAsync();

            return Json(new { success = true, planId = plan.PlanId });
        }

        // POST: /Admin/UpdateSubscriptionPlan — Edit an existing subscription plan
        [HttpPost]
        public async Task<IActionResult> UpdateSubscriptionPlan([FromBody] UpdatePlanRequest request)
        {
            var plan = await _context.SubscriptionPlans.FindAsync(request.PlanId);
            if (plan == null)
                return Json(new { success = false, error = "Plan not found." });

            if (string.IsNullOrWhiteSpace(request.PlanName))
                return Json(new { success = false, error = "Plan name is required." });

            if (request.Price <= 0)
                return Json(new { success = false, error = "Price must be greater than zero." });

            // Check for duplicate name (excluding current plan)
            var duplicate = await _context.SubscriptionPlans
                .AnyAsync(p => p.PlanName == request.PlanName.Trim() && p.PlanId != request.PlanId);
            if (duplicate)
                return Json(new { success = false, error = "A plan with this name already exists." });

            plan.PlanName = request.PlanName.Trim();
            plan.Price = request.Price;
            plan.DurationInDays = request.DurationInDays;
            plan.MaxVenuesAllowed = request.MaxVenuesAllowed;
            plan.Description = request.Description?.Trim() ?? plan.Description;
            plan.Features = request.Features?.Trim() ?? plan.Features;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: /Admin/DeleteSubscriptionPlan — Delete a subscription plan
        [HttpPost]
        public async Task<IActionResult> DeleteSubscriptionPlan(int id)
        {
            var plan = await _context.SubscriptionPlans
                .Include(p => p.Subscriptions)
                .FirstOrDefaultAsync(p => p.PlanId == id);

            if (plan == null)
                return Json(new { success = false, error = "Plan not found." });

            // Prevent deletion if owners have active subscriptions for this plan
            var hasActiveSubscribers = plan.Subscriptions.Any(s => s.IsActive && s.EndDate >= DateTime.Now);
            if (hasActiveSubscribers)
                return Json(new { success = false, error = "Cannot delete plan with active subscribers. Deactivate it instead." });

            _context.SubscriptionPlans.Remove(plan);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> TogglePlanStatus(int id)
        {
            var plan = await _context.SubscriptionPlans.FindAsync(id);
            if (plan == null) return Json(new { success = false });

            plan.IsActive = !plan.IsActive;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        public class CreatePlanRequest
        {
            public string PlanName { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public int DurationInDays { get; set; }
            public int MaxVenuesAllowed { get; set; }
            public string Description { get; set; } = string.Empty;
            public string Features { get; set; } = "[]";
        }

        public class UpdatePlanRequest
        {
            public int PlanId { get; set; }
            public string PlanName { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public int DurationInDays { get; set; }
            public int MaxVenuesAllowed { get; set; }
            public string Description { get; set; } = string.Empty;
            public string Features { get; set; } = "[]";
        }

        // ==================== PAYMENTS ====================

        public async Task<IActionResult> Payments()
        {
            var payments = await _context.Payments
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.Booking)
                .OrderByDescending(p => p.PaidAt)
                .Take(100)
                .Select(p => new PaymentListItemViewModel
                {
                    PaymentId = p.PaymentId,
                    TransactionId = p.TransactionId,
                    CustomerName = p.User.Name,
                    CustomerEmail = p.User.Email,
                    PaymentFor = p.BookingId > 0 ? "Booking" : "Subscription",
                    ReferenceId = p.BookingId > 0 ? p.BookingId : p.PaymentId,
                    Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    Status = p.Status,
                    PaidAt = p.PaidAt
                })
                .ToListAsync();

            var last30Days = Enumerable.Range(0, 30)
                .Select(i => DateTime.Now.Date.AddDays(-i))
                .Reverse()
                .ToList();

            var revenueData = new List<decimal>();
            foreach (var date in last30Days)
            {
                var amount = await _context.Payments
                    .Where(p => p.Status == PaymentStatus.Completed &&
                               p.PaidAt.HasValue &&
                               p.PaidAt.Value.Date == date)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0;
                revenueData.Add(amount);
            }

            var viewModel = new AdminPaymentsViewModel
            {
                TotalRevenue = await _context.Payments
                    .Where(p => p.Status == PaymentStatus.Completed)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0,
                TotalTransactions = await _context.Payments.CountAsync(),
                PendingAmount = await _context.Payments
                    .Where(p => p.Status == PaymentStatus.Pending)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0,
                RefundedAmount = await _context.Payments
                    .Where(p => p.Status == PaymentStatus.Refunded)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0,
                RevenueChartLabels = last30Days.Select(d => d.ToString("MMM dd")).ToList(),
                RevenueChartData = revenueData,
                Payments = payments
            };

            return View(viewModel);
        }

        // ==================== REVIEWS ====================

        public async Task<IActionResult> Reviews()
        {
            var reviews = await _context.Reviews
                .AsNoTracking()
                .Include(r => r.Customer)
                .Include(r => r.Venue)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewListItemViewModel
                {
                    ReviewId = r.ReviewId,
                    VenueId = r.VenueId,
                    VenueName = r.Venue.VenueName,
                    CustomerId = r.CustomerId,
                    CustomerName = r.Customer.Name,
                    CustomerAvatar = null,
                    BookingId = r.BookingId,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    IsApproved = r.IsApproved,
                    CreatedAt = r.CreatedAt,
                    ReplyId = null,
                    ReplyContent = null,
                    RepliedAt = null
                })
                .ToListAsync();

            var ratingDist = await _context.Reviews
                .Where(r => r.IsApproved)
                .GroupBy(r => r.Rating)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Rating, x => x.Count);

            var viewModel = new AdminReviewsViewModel
            {
                TotalReviews = await _context.Reviews.CountAsync(),
                ApprovedReviews = await _context.Reviews.CountAsync(r => r.IsApproved),
                PendingReviews = await _context.Reviews.CountAsync(r => !r.IsApproved),
                AverageRating = await _context.Reviews.Where(r => r.IsApproved).AverageAsync(r => (double?)r.Rating) ?? 0,
                RatingDistribution = ratingDist,
                Reviews = reviews
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return Json(new { success = false });

            review.IsApproved = true;
            await _context.SaveChangesAsync();

            // Notify the customer that their review is now live
            _context.Notifications.Add(new Notification
            {
                UserId = review.CustomerId,
                Title = "Review Published!",
                Message = "Your review has been approved and is now visible to others.",
                Type = "Review",
                IsRead = false,
                ActionUrl = $"/Customer/BookingDetails/{review.BookingId}",
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return Json(new { success = false });

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ==================== AUDIT LOGS ====================

        public async Task<IActionResult> AuditLogs(int page = 1, int pageSize = 50)
        {
            var query = _context.AuditLogs
                .AsNoTracking()
                .Include(a => a.ChangedByUser)
                .OrderByDescending(a => a.ChangedAt);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var logs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuditLogListItemViewModel
                {
                    LogId = a.LogId,
                    ChangedAt = a.ChangedAt,
                    UserName = a.ChangedByUser.Name,
                    Action = a.Action,
                    TableName = a.TableName,
                    RecordId = a.RecordId,
                    Changes = a.NewValues != null ? "Modified" : (a.Action == "INSERT" ? "Created" : "Deleted")
                })
                .ToListAsync();

            var viewModel = new AdminAuditLogsViewModel
            {
                TotalLogs = totalCount,
                CreatedLogs = await _context.AuditLogs.CountAsync(a => a.Action == "INSERT"),
                UpdatedLogs = await _context.AuditLogs.CountAsync(a => a.Action == "UPDATE"),
                DeletedLogs = await _context.AuditLogs.CountAsync(a => a.Action == "DELETE"),
                CurrentPage = page,
                TotalPages = totalPages,
                Logs = logs
            };

            return View(viewModel);
        }

        // ==================== THEMES ====================

        public async Task<IActionResult> Themes()
        {
            var themes = await _context.Themes
                .AsNoTracking()
                .Include(t => t.Venue)
                .Include(t => t.Images)
                .Include(t => t.Pricings)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new ThemeListItemViewModel
                {
                    ThemeId = t.ThemeId,
                    ThemeName = t.ThemeName,
                    Description = t.Description,
                    IsActive = t.IsActive,
                    IsDefault = t.IsDefault,
                    VenueName = t.Venue.VenueName,
                    CoverImageUrl = t.Images.Where(i => i.IsCover).Select(i => i.ImageUrl).FirstOrDefault()
                                    ?? t.Images.Select(i => i.ImageUrl).FirstOrDefault()
                                    ?? "/images/default-theme.jpg",
                    // Fix: return null instead of 0 when no pricings exist
                    Price = t.Pricings.Where(p => p.IsActive).Any()
                            ? t.Pricings.Where(p => p.IsActive).Min(p => p.PricePerDay)
                            : (decimal?)null,
                    EventType = t.Pricings.Where(p => p.IsActive)
                                .OrderBy(p => p.PricePerDay)
                                .Select(p => p.EventType)
                                .FirstOrDefault() ?? "",
                    // Load all active pricings
                    Pricings = t.Pricings.Where(p => p.IsActive).Select(p => new ThemePricingDisplay
                    {
                        EventType = p.EventType,
                        PricePerDay = p.PricePerDay
                    }).ToList()
                })
                .ToListAsync();

            foreach (var t in themes)
            {
                if (string.IsNullOrEmpty(t.CoverImageUrl)) t.CoverImageUrl = "/images/default-theme.jpg";
            }

            var viewModel = new AdminThemesViewModel
            {
                TotalThemes = await _context.Themes.CountAsync(),
                ActiveThemes = await _context.Themes.CountAsync(t => t.IsActive),
                TotalCategories = await _context.Themes.Select(t => t.Venue.VenueType).Distinct().CountAsync(),
                VenuesUsingThemes = await _context.Themes.Select(t => t.VenueId).Distinct().CountAsync(),
                Categories = await _context.Themes.Select(t => t.Venue.VenueType).Distinct().ToListAsync(),
                Themes = themes
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTheme(int id)
        {
            var theme = await _context.Themes.FindAsync(id);
            if (theme == null) return Json(new { success = false });

            _context.Themes.Remove(theme);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ==================== SETTINGS ====================

        public IActionResult Settings()
        {
            var viewModel = new AdminSettingsViewModel
            {
                PlatformName = "Venue Vista",
                SupportEmail = "support@venuevista.com",
                Timezone = "UTC",
                MaintenanceMode = false,
                SmtpHost = "smtp.gmail.com",
                SmtpPort = 587,
                SmtpEncryption = "TLS",
                StripeEnabled = true,
                PayPalEnabled = false
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SaveGeneralSettings(AdminSettingsViewModel model)
        {
            TempData["Success"] = "Settings saved successfully!";
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        public async Task<IActionResult> SaveEmailSettings(AdminSettingsViewModel model)
        {
            TempData["Success"] = "Email settings saved!";
            return RedirectToAction(nameof(Settings));
        }

        // ==================== AMENITIES ====================

        public async Task<IActionResult> Amenities()
        {
            var amenities = await _context.Amenities
                .AsNoTracking()
                .Select(a => new AmenityListItemViewModel
                {
                    AmenityId = a.AmenityId,
                    Name = a.Name,
                    Icon = a.Icon,
                    Category = a.Category,
                    VenueCount = a.VenueAmenities.Count
                })
                .OrderBy(a => a.Category)
                .ThenBy(a => a.Name)
                .ToListAsync();

            var viewModel = new AdminAmenitiesViewModel
            {
                TotalAmenities = amenities.Count,
                TotalCategories = amenities.Select(a => a.Category).Where(c => !string.IsNullOrEmpty(c)).Distinct().Count(),
                ChargeableAmenities = await _context.VenueAmenities.CountAsync(va => va.IsChargeable),
                VenuesUsingAmenities = await _context.VenueAmenities.Select(va => va.VenueId).Distinct().CountAsync(),
                Categories = amenities.Select(a => a.Category).Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c).ToList(),
                Amenities = amenities
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAmenity([FromBody] CreateAmenityRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Json(new { success = false, error = "Name is required." });

            var exists = await _context.Amenities.AnyAsync(a => a.Name == request.Name);
            if (exists)
                return Json(new { success = false, error = "An amenity with this name already exists." });

            var amenity = new Amenity
            {
                Name = request.Name.Trim(),
                Icon = request.Icon?.Trim() ?? "check_circle",
                Category = request.Category?.Trim() ?? "General"
            };

            _context.Amenities.Add(amenity);
            await _context.SaveChangesAsync();

            return Json(new { success = true, amenityId = amenity.AmenityId });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAmenity([FromBody] UpdateAmenityRequest request)
        {
            var amenity = await _context.Amenities.FindAsync(request.AmenityId);
            if (amenity == null)
                return Json(new { success = false, error = "Amenity not found." });

            if (string.IsNullOrWhiteSpace(request.Name))
                return Json(new { success = false, error = "Name is required." });

            amenity.Name = request.Name.Trim();
            amenity.Icon = request.Icon?.Trim() ?? amenity.Icon;
            amenity.Category = request.Category?.Trim() ?? amenity.Category;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAmenity(int id)
        {
            var amenity = await _context.Amenities
                .Include(a => a.VenueAmenities)
                .FirstOrDefaultAsync(a => a.AmenityId == id);

            if (amenity == null)
                return Json(new { success = false, error = "Amenity not found." });

            if (amenity.VenueAmenities.Any())
                return Json(new { success = false, error = $"Cannot delete. This amenity is used by {amenity.VenueAmenities.Count} venue(s)." });

            _context.Amenities.Remove(amenity);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        public class CreateAmenityRequest
        {
            public string Name { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
        }

        public class UpdateAmenityRequest
        {
            public int AmenityId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
        }

        // ==================== OWNER SUBSCRIPTIONS ====================

        public async Task<IActionResult> OwnerSubscriptions(string status = "All")
        {
            var query = _context.OwnerSubscriptions
                .AsNoTracking()
                .Include(s => s.Owner)
                .Include(s => s.Plan)
                .AsQueryable();

            if (status == "Active")
                query = query.Where(s => s.IsActive && s.EndDate >= DateTime.Now);
            else if (status == "Expired")
                query = query.Where(s => s.EndDate < DateTime.Now);
            else if (status == "Inactive")
                query = query.Where(s => !s.IsActive);

            var subscriptions = await query
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new OwnerSubscriptionListItemViewModel
                {
                    SubscriptionId = s.SubscriptionId,
                    OwnerName = s.Owner.Name,
                    OwnerEmail = s.Owner.Email,
                    PlanName = s.Plan.PlanName,
                    PlanPrice = s.Plan.Price,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    PaymentStatus = s.PaymentStatus,
                    TransactionId = s.TransactionId,
                    MaxVenuesAllowed = s.MaxVenuesAllowed,
                    CurrentVenueCount = s.CurrentVenueCount,
                    IsActive = s.IsActive
                })
                .ToListAsync();

            var viewModel = new AdminOwnerSubscriptionsViewModel
            {
                TotalSubscriptions = await _context.OwnerSubscriptions.CountAsync(),
                ActiveSubscriptions = await _context.OwnerSubscriptions.CountAsync(s => s.IsActive && s.EndDate >= DateTime.Now),
                ExpiredSubscriptions = await _context.OwnerSubscriptions.CountAsync(s => s.EndDate < DateTime.Now),
                TotalRevenue = await _context.OwnerSubscriptions
                    .Where(s => s.PaymentStatus == PaymentStatus.Completed)
                    .Include(s => s.Plan)
                    .SumAsync(s => (decimal?)s.Plan.Price) ?? 0,
                Subscriptions = subscriptions
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleSubscriptionStatus(int id)
        {
            var subscription = await _context.OwnerSubscriptions.FindAsync(id);
            if (subscription == null) return Json(new { success = false });

            subscription.IsActive = !subscription.IsActive;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isActive = subscription.IsActive });
        }

        [HttpPost]
        public async Task<IActionResult> RefundPayment(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return Json(new { success = false, error = "Payment not found." });

            if (payment.Status != PaymentStatus.Completed)
                return Json(new { success = false, error = "Only completed payments can be refunded." });

            payment.Status = PaymentStatus.Refunded;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ==================== MESSAGES ====================

        public async Task<IActionResult> Messages(string filter = "All")
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var query = _context.Messages
                .AsNoTracking()
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Include(m => m.Booking)
                .AsQueryable();

            if (filter == "Unread")
                query = query.Where(m => !m.IsRead && m.ReceiverId == adminId);
            else if (filter == "Received")
                query = query.Where(m => m.ReceiverId == adminId);
            else if (filter == "Sent")
                query = query.Where(m => m.SenderId == adminId);

            var messagesRaw = await query
                .OrderByDescending(m => m.SentAt)
                .Take(100)
                .Select(m => new
                {
                    m.MessageId,
                    m.BookingId,
                    m.SenderId,
                    SenderName = m.Sender.Name,
                    SenderEmail = m.Sender.Email,
                    SenderRole = m.Sender.Role,
                    m.ReceiverId,
                    ReceiverName = m.Receiver.Name,
                    m.Content,
                    m.IsRead,
                    m.SentAt
                })
                .ToListAsync();

            var messages = messagesRaw.Select(m => new AdminMessageListItemViewModel
            {
                MessageId = m.MessageId,
                BookingId = m.BookingId,
                SenderId = m.SenderId,
                SenderName = m.SenderName,
                SenderEmail = m.SenderEmail,
                SenderRole = m.SenderRole,
                ReceiverId = m.ReceiverId,
                ReceiverName = m.ReceiverName,
                Content = m.Content,
                IsRead = m.IsRead,
                SentAt = m.SentAt,
                SenderInitials = GetInitials(m.SenderName),
                TimeAgo = GetTimeAgo(m.SentAt)
            }).ToList();

            var viewModel = new AdminMessagesViewModel
            {
                TotalMessages = await _context.Messages.CountAsync(m => m.SenderId == adminId || m.ReceiverId == adminId),
                UnreadMessages = await _context.Messages.CountAsync(m => !m.IsRead && m.ReceiverId == adminId),
                TodayMessages = await _context.Messages.CountAsync(m =>
                    (m.SenderId == adminId || m.ReceiverId == adminId) &&
                    m.SentAt.Date == DateTime.Now.Date),
                UniqueConversations = await _context.Messages
                    .Where(m => m.SenderId == adminId || m.ReceiverId == adminId)
                    .Select(m => m.BookingId)
                    .Distinct()
                    .CountAsync(),
                Messages = messages
            };

            return View(viewModel);
        }

        // GET: /Admin/Conversation/5 — View all messages for a booking
        public async Task<IActionResult> Conversation(int id)
        {
            var messages = await _context.Messages
                .AsNoTracking()
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.BookingId == id)
                .OrderBy(m => m.SentAt)
                .Select(m => new AdminMessageListItemViewModel
                {
                    MessageId = m.MessageId,
                    BookingId = m.BookingId,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.Name,
                    SenderEmail = m.Sender.Email,
                    SenderRole = m.Sender.Role,
                    ReceiverId = m.ReceiverId,
                    ReceiverName = m.Receiver.Name,
                    Content = m.Content,
                    IsRead = m.IsRead,
                    SentAt = m.SentAt
                })
                .ToListAsync();

            var booking = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Venue)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();

            // Mark unread messages addressed to admin as read
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var unread = await _context.Messages
                .Where(m => m.BookingId == id && m.ReceiverId == adminId && !m.IsRead)
                .ToListAsync();
            foreach (var m in unread) m.IsRead = true;
            if (unread.Count > 0) await _context.SaveChangesAsync();

            var viewModel = new AdminConversationViewModel
            {
                BookingId = id,
                VenueName = booking.Venue?.VenueName ?? "Unknown",
                CustomerName = booking.Customer?.Name ?? "Unknown",
                OwnerName = booking.Venue?.Owner?.Name ?? "Unknown",
                Messages = messages
            };

            return View(viewModel);
        }

        // POST: /Admin/SendMessage — Admin sends a reply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
                return Json(new { success = false, error = "Message content is required." });

            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var booking = await _context.Bookings.FindAsync(request.BookingId);
            if (booking == null)
                return Json(new { success = false, error = "Booking not found." });

            var receiver = await _context.Users.FindAsync(request.ReceiverId);
            if (receiver == null)
                return Json(new { success = false, error = "Recipient not found." });

            var message = new Message
            {
                BookingId = request.BookingId,
                SenderId = adminId,
                ReceiverId = request.ReceiverId,
                Content = request.Content.Trim(),
                IsRead = false,
                SentAt = DateTime.Now
            };

            _context.Messages.Add(message);

            // Notify the receiver
            _context.Notifications.Add(new Notification
            {
                UserId = request.ReceiverId,
                Title = "New Message from Admin",
                Message = $"You have a new message regarding Booking #{request.BookingId}.",
                Type = "Message",
                IsRead = false,
                ActionUrl = receiver.Role == UserRole.Customer
                    ? $"/Customer/BookingDetails/{request.BookingId}"
                    : $"/Owner/BookingDetails/{request.BookingId}",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Json(new { success = true, messageId = message.MessageId });
        }

        // POST: /Admin/MarkMessageRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkMessageRead(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message == null) return Json(new { success = false });

            message.IsRead = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: /Admin/DeleteMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message == null) return Json(new { success = false });

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        public class SendMessageRequest
        {
            public int BookingId { get; set; }
            public int ReceiverId { get; set; }
            public string Content { get; set; } = string.Empty;
        }
    }
}