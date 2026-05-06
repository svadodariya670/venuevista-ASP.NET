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
    [Authorize(Roles = UserRole.Customer)]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetUserId() =>
     int.Parse(
         User.FindFirst(ClaimTypes.NameIdentifier)?.Value
             ?? User.FindFirst("UserId")?.Value
             ?? "0"
     );

        // ==================== BROWSE VENUES ====================  

        public async Task<IActionResult> BrowseVenues(string? city, string? type, int? guests, string? search, int page = 1)
        {
            const int pageSize = 12;

            var query = _context.Venues
                .AsNoTracking()
                .Include(v => v.Images)
                .Include(v => v.Reviews)
                .Include(v => v.Themes).ThenInclude(t => t.Pricings)
                .Where(v => v.IsActive);
                // Temporarily removed strict filters — re-enable after data setup
                // .Where(v => v.IsVerified)
                // .Where(v => v.Themes.Any(t => t.IsActive && t.Pricings.Any(p => p.IsActive)));

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(v => v.VenueName.Contains(search) || v.Description.Contains(search));

            if (!string.IsNullOrWhiteSpace(city) && city != "All")
                query = query.Where(v => v.City == city);

            if (!string.IsNullOrWhiteSpace(type) && type != "All")
                query = query.Where(v => v.VenueType == type);

            if (guests.HasValue && guests.Value > 0)
                query = query.Where(v => v.Capacity >= guests.Value);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var venues = await query
                .OrderByDescending(v => v.Reviews.Where(r => r.IsApproved).Average(r => (double?)r.Rating) ?? 0)
                .ThenByDescending(v => v.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new VenueCardViewModel
                {
                    VenueId = v.VenueId,
                    VenueName = v.VenueName,
                    City = v.City,
                    State = v.State,
                    VenueType = v.VenueType,
                    Capacity = v.Capacity,
                    CoverImageUrl = v.Images.Where(i => i.IsCover).Select(i => i.ImageUrl).FirstOrDefault()
                                    ?? v.Images.Select(i => i.ImageUrl).FirstOrDefault()
                                    ?? "/images/default-venue.jpg",
                    AverageRating = v.Reviews.Where(r => r.IsApproved).Any()
                        ? Math.Round(v.Reviews.Where(r => r.IsApproved).Average(r => r.Rating), 1)
                        : 0,
                    ReviewCount = v.Reviews.Count(r => r.IsApproved),
                    StartingPrice = v.Themes
                        .Where(t => t.IsActive)
                        .SelectMany(t => t.Pricings.Where(p => p.IsActive))
                        .Min(p => (decimal?)p.PricePerDay) ?? 0,
                    Slug = v.Slug
                })
                .ToListAsync();

            // Also relax filters for dropdowns
            var venueBaseQuery = _context.Venues.Where(v => v.IsActive);

            var viewModel = new VenueSearchViewModel
            {
                Venues = venues,
                City = city,
                VenueType = type,
                Guests = guests,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalResults = totalCount,
                AvailableTypes = await venueBaseQuery
                    .Select(v => v.VenueType)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToListAsync(),
                AvailableCities = await venueBaseQuery
                    .Select(v => v.City)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync()
            };

            ViewBag.Search = search;
            return View(viewModel);
        }

        // GET: /Customer/VenueDetails/5
        public async Task<IActionResult> VenueDetails(int id)
        {
            var venue = await _context.Venues
                .AsNoTracking()
                .Include(v => v.Owner)
                .Include(v => v.Images.OrderBy(i => i.DisplayOrder))
                .Include(v => v.VenueAmenities).ThenInclude(va => va.Amenity)
                .Include(v => v.Themes.Where(t => t.IsActive))
                    .ThenInclude(t => t.Pricings.Where(p => p.IsActive))
                .Include(v => v.Themes.Where(t => t.IsActive))
                    .ThenInclude(t => t.Images)
                .Include(v => v.Reviews.Where(r => r.IsApproved))
                    .ThenInclude(r => r.Customer)
                .FirstOrDefaultAsync(v => v.VenueId == id && v.IsActive);
                // Removed: && v.IsVerified

            if (venue == null) return NotFound();

            var blockedDates = await _context.VenueBlockedDates
                .Where(b => b.VenueId == id && b.BlockDate >= DateTime.Now)
                .Select(b => b.BlockDate.ToString("yyyy-MM-dd"))
                .ToListAsync();

            var bookedDates = await _context.Bookings
                .Where(b => b.VenueId == id &&
                           b.Status == BookingStatus.Confirmed &&
                           b.StartDate >= DateTime.Now)
                .Select(b => b.StartDate.ToString("yyyy-MM-dd"))
                .ToListAsync();

            ViewBag.BlockedDates = blockedDates.Concat(bookedDates).Distinct().ToList();

            return View(venue);
        }

        // ==================== DASHBOARD ====================

        public async Task<IActionResult> Dashboard()
        {
            var userId = GetUserId();

            var bookings = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Venue).ThenInclude(v => v.Images)
                .Include(b => b.Theme)
                .Include(b => b.Payments)
                .Where(b => b.CustomerId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var viewModel = new CustomerDashboardViewModel
            {
                TotalBookings = bookings.Count,
                UpcomingBookings = bookings.Count(b =>
                    b.Status == BookingStatus.Confirmed && b.StartDate >= DateTime.Now),
                PendingBookings = bookings.Count(b => b.Status == BookingStatus.Pending),
                CompletedBookings = bookings.Count(b =>
                    b.Status == BookingStatus.Confirmed && b.EndDate < DateTime.Now),
                TotalSpent = bookings
                    .Where(b => b.Status != BookingStatus.Cancelled)
                    .Sum(b => b.TotalPrice),
                RecentBookings = bookings.Take(5).Select(b => new CustomerBookingListItem
                {
                    BookingId = b.BookingId,
                    VenueName = b.Venue?.VenueName ?? "Unknown",
                    VenueCity = b.Venue?.City ?? "",
                    CoverImageUrl = b.Venue?.Images.FirstOrDefault(i => i.IsCover)?.ImageUrl
                                    ?? b.Venue?.Images.FirstOrDefault()?.ImageUrl
                                    ?? "/images/default-venue.jpg",
                    ThemeName = b.Theme?.ThemeName ?? "Default",
                    EventType = b.EventType,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    TotalPrice = b.TotalPrice,
                    Status = b.Status,
                    PaymentStatus = b.Payments.OrderByDescending(p => p.PaidAt)
                        .FirstOrDefault()?.Status ?? PaymentStatus.Pending,
                    CreatedAt = b.CreatedAt
                }).ToList(),
                UnreadNotifications = await _context.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead),
                UnreadMessages = await _context.Messages
                    .CountAsync(m => m.ReceiverId == userId && !m.IsRead)
            };

            return View(viewModel);
        }

        // ==================== BOOKINGS ====================

        public async Task<IActionResult> Bookings(string status = "All")
        {
            var userId = GetUserId();

            var query = _context.Bookings
                .AsNoTracking()
                .Include(b => b.Venue).ThenInclude(v => v.Images)
                .Include(b => b.Theme)
                .Include(b => b.Payments)
                .Where(b => b.CustomerId == userId);

            if (status != "All")
                query = query.Where(b => b.Status == status);

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new CustomerBookingListItem
                {
                    BookingId = b.BookingId,
                    VenueName = b.Venue.VenueName,
                    VenueCity = b.Venue.City,
                    CoverImageUrl = b.Venue.Images.Where(i => i.IsCover).Select(i => i.ImageUrl).FirstOrDefault()
                                    ?? b.Venue.Images.Select(i => i.ImageUrl).FirstOrDefault()
                                    ?? "/images/default-venue.jpg",
                    ThemeName = b.Theme.ThemeName,
                    EventType = b.EventType,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    TotalPrice = b.TotalPrice,
                    Status = b.Status,
                    PaymentStatus = b.Payments.OrderByDescending(p => p.PaidAt)
                        .Select(p => p.Status).FirstOrDefault() ?? PaymentStatus.Pending,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            var viewModel = new CustomerBookingsViewModel
            {
                Bookings = bookings,
                CurrentFilter = status,
                TotalCount = await _context.Bookings.CountAsync(b => b.CustomerId == userId),
                PendingCount = await _context.Bookings.CountAsync(b => b.CustomerId == userId && b.Status == BookingStatus.Pending),
                ConfirmedCount = await _context.Bookings.CountAsync(b => b.CustomerId == userId && b.Status == BookingStatus.Confirmed),
                CancelledCount = await _context.Bookings.CountAsync(b => b.CustomerId == userId && b.Status == BookingStatus.Cancelled)
            };

            return View(viewModel);
        }

        // GET: /Customer/BookingDetails/5
        public async Task<IActionResult> BookingDetails(int id)
        {
            var userId = GetUserId();

            var booking = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Venue).ThenInclude(v => v.Owner)
                .Include(b => b.Venue).ThenInclude(v => v.Images)
                .Include(b => b.Theme).ThenInclude(t => t.Images)
                .Include(b => b.Payments)
                .Include(b => b.Review)
                .Include(b => b.Messages)
                .FirstOrDefaultAsync(b => b.BookingId == id && b.CustomerId == userId);

            if (booking == null) return NotFound();

            return View(booking);
        }

        // POST: /Customer/CreateBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBooking(CreateBookingViewModel model)
        {
            var userId = GetUserId();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fill in all required fields.";
                return RedirectToAction("VenueDetails", new { id = model.VenueId });
            }

            var venue = await _context.Venues
                .Include(v => v.Themes).ThenInclude(t => t.Pricings)
                .FirstOrDefaultAsync(v => v.VenueId == model.VenueId && v.IsActive);

            if (venue == null)
            {
                TempData["Error"] = "Venue not found or not available.";
                return RedirectToAction("BrowseVenues");
            }

            var theme = venue.Themes.FirstOrDefault(t => t.ThemeId == model.ThemeId && t.IsActive);
            if (theme == null)
            {
                TempData["Error"] = "Selected theme is not available.";
                return RedirectToAction("VenueDetails", new { id = model.VenueId });
            }

            // Pricing MUST exist — customer selected event type from pricing dropdown
            var pricing = theme.Pricings
                .FirstOrDefault(p => p.EventType == model.EventType && p.IsActive);

            if (pricing == null)
            {
                TempData["Error"] = "Selected event type pricing not found. Please try again.";
                return RedirectToAction("VenueDetails", new { id = model.VenueId });
            }

            var isBlocked = await _context.VenueBlockedDates
                .AnyAsync(b => b.VenueId == model.VenueId && b.BlockDate.Date == model.StartDate.Date);

            var isBooked = await _context.Bookings
                .AnyAsync(b => b.VenueId == model.VenueId &&
                              b.Status == BookingStatus.Confirmed &&
                              b.StartDate.Date == model.StartDate.Date);

            if (isBlocked || isBooked)
            {
                TempData["Error"] = "Selected date is not available. Please choose another date.";
                return RedirectToAction("VenueDetails", new { id = model.VenueId });
            }

            var totalDays = (model.EndDate - model.StartDate).Days + 1;
            if (totalDays < 1) totalDays = 1;

            // Final price = ThemePricing.PricePerDay × days
            decimal totalPrice = pricing.PricePerDay * totalDays;

            // Apply seasonal multiplier if applicable
            var seasonalPricing = await _context.SeasonalPricings
                .FirstOrDefaultAsync(sp => sp.VenueId == model.VenueId &&
                                          sp.IsActive &&
                                          model.StartDate >= sp.StartDate &&
                                          model.StartDate <= sp.EndDate);

            if (seasonalPricing != null)
            {
                totalPrice = totalPrice * seasonalPricing.PriceMultiplier;
            }

            var booking = new Booking
            {
                VenueId = model.VenueId,
                ThemeId = model.ThemeId,
                CustomerId = userId,
                EventType = model.EventType,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                TotalPrice = totalPrice,
                BookingSource = "Online",
                Status = BookingStatus.Pending,
                PaymentStatus = PaymentStatus.Pending,
                SpecialRequests = model.SpecialRequests ?? "",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            _context.Notifications.Add(new Notification
            {
                UserId = venue.OwnerId,
                Title = "New Booking Request!",
                Message = $"New booking for \"{venue.VenueName}\" — {model.EventType} on {model.StartDate:MMM dd, yyyy} to {model.EndDate:MMM dd, yyyy} ({totalDays} days). Total: ₹{totalPrice:N0}",
                Type = "Booking",
                IsRead = false,
                ActionUrl = $"/Owner/BookingDetails/{booking.BookingId}",
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Booking request submitted! The venue owner will review and confirm your booking.";
            return RedirectToAction("BookingDetails", new { id = booking.BookingId });
        }

        // ==================== PAYMENT ====================

        // Payment is handled by the venue owner only.
        // If a customer navigates to this URL, redirect them back.
        public IActionResult Payment(int id)
        {
            TempData["Info"] = "Payments are handled by the venue owner. Please contact them directly.";
            return RedirectToAction("BookingDetails", new { id });
        }

        // ==================== CANCEL BOOKING ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var userId = GetUserId();

            var booking = await _context.Bookings
                .Include(b => b.Venue)
                .FirstOrDefaultAsync(b => b.BookingId == id && b.CustomerId == userId);

            if (booking == null) return NotFound();

            if (booking.Status == BookingStatus.Cancelled)
            {
                TempData["Error"] = "Booking is already cancelled.";
                return RedirectToAction("BookingDetails", new { id });
            }

            if (booking.Status == BookingStatus.Confirmed && booking.StartDate <= DateTime.Now.AddDays(2))
            {
                TempData["Error"] = "Cannot cancel within 2 days of the event. Contact the venue owner.";
                return RedirectToAction("BookingDetails", new { id });
            }

            booking.Status = BookingStatus.Cancelled;
            booking.UpdatedAt = DateTime.Now;

            var blockedDate = await _context.VenueBlockedDates
                .FirstOrDefaultAsync(b => b.VenueId == booking.VenueId &&
                                         b.BlockDate.Date == booking.StartDate.Date);
            if (blockedDate != null)
                _context.VenueBlockedDates.Remove(blockedDate);

            await _context.SaveChangesAsync();

            _context.Notifications.Add(new Notification
            {
                UserId = booking.Venue.OwnerId,
                Title = "Booking Cancelled",
                Message = $"Booking #{booking.BookingId} for \"{booking.Venue.VenueName}\" on {booking.StartDate:MMM dd, yyyy} has been cancelled by the customer.",
                Type = "Booking",
                IsRead = false,
                ActionUrl = $"/Owner/BookingDetails/{booking.BookingId}",
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Booking cancelled successfully.";
            return RedirectToAction("Bookings");
        }

        // ==================== REVIEWS ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int bookingId, int rating, string comment)
        {
            var userId = GetUserId();

            var booking = await _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Review)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == userId);

            if (booking == null) return NotFound();

            if (booking.Review != null)
            {
                TempData["Error"] = "You have already reviewed this booking.";
                return RedirectToAction("BookingDetails", new { id = bookingId });
            }

            // FIX: Allow review for Confirmed (past date) OR Completed status
            var isEventOver = (booking.Status == BookingStatus.Confirmed && booking.EndDate < DateTime.Now)
                           || booking.Status == BookingStatus.Completed;

            if (!isEventOver)
            {
                TempData["Error"] = "You can only review completed bookings.";
                return RedirectToAction("BookingDetails", new { id = bookingId });
            }

            if (rating < 1 || rating > 5)
            {
                TempData["Error"] = "Rating must be between 1 and 5.";
                return RedirectToAction("BookingDetails", new { id = bookingId });
            }

            var review = new Review
            {
                VenueId = booking.VenueId,
                CustomerId = userId,
                BookingId = bookingId,
                Rating = rating,
                Comment = comment?.Trim() ?? "",
                IsApproved = false,
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Notify the venue owner
            _context.Notifications.Add(new Notification
            {
                UserId = booking.Venue.OwnerId,
                Title = "New Review Received!",
                Message = $"A customer left a {rating}-star review for \"{booking.Venue.VenueName}\". It will be visible after admin approval.",
                Type = "Review",
                IsRead = false,
                ActionUrl = "/Owner/Reviews",
                CreatedAt = DateTime.Now
            });

            // Notify admins
            var adminIds = await _context.Users
                .Where(u => u.Role == UserRole.Admin && u.IsActive)
                .Select(u => u.UserId)
                .ToListAsync();

            foreach (var adminId in adminIds)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = adminId,
                    Title = "Review Pending Approval",
                    Message = $"A {rating}-star review for \"{booking.Venue.VenueName}\" needs approval.",
                    Type = "Review",
                    IsRead = false,
                    ActionUrl = "/Admin/Reviews",
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Review submitted! It will be visible after admin approval.";
            return RedirectToAction("BookingDetails", new { id = bookingId });
        }

        // ==================== NOTIFICATIONS ====================

        public async Task<IActionResult> Notifications()
        {
            var userId = GetUserId();

            var notifications = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();

            return View(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            var userId = GetUserId();

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == userId);

            if (notification == null) return Json(new { success = false });

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllNotificationsRead()
        {
            var userId = GetUserId();

            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread) n.IsRead = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ==================== MESSAGES ====================

        public async Task<IActionResult> Messages(int? ownerId = null)
        {
            var userId = GetUserId();

            var messages = await _context.Messages
                .AsNoTracking()
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            var conversations = messages
                .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Select(g =>
                {
                    var last = g.First();
                    var otherPerson = last.SenderId == userId ? last.Receiver : last.Sender;
                    return new CustomerConversationViewModel
                    {
                        OtherUserId = g.Key,
                        OtherUserName = otherPerson?.Name ?? "Unknown",
                        LastMessage = last.Content,
                        LastMessageTime = last.SentAt,
                        UnreadCount = g.Count(m => m.ReceiverId == userId && !m.IsRead),
                        IsSelected = g.Key == ownerId
                    };
                })
                .OrderByDescending(c => c.LastMessageTime)
                .ToList();

            List<CustomerMessageViewModel> selectedMessages = [];
            string? selectedOwnerName = null;

            if (ownerId.HasValue)
            {
                // Verify the owner exists and is actually an Owner
                var owner = await _context.Users.FindAsync(ownerId.Value);
                if (owner == null || owner.Role != UserRole.Owner)
                {
                    TempData["Error"] = "Venue owner not found.";
                    return RedirectToAction("Messages");
                }

                selectedOwnerName = owner.Name ?? "Unknown";

                // Mark as read
                var unread = await _context.Messages
                    .Where(m => m.SenderId == ownerId.Value && m.ReceiverId == userId && !m.IsRead)
                    .ToListAsync();
                foreach (var m in unread) m.IsRead = true;
                if (unread.Any()) await _context.SaveChangesAsync();

                selectedMessages = await _context.Messages
                    .AsNoTracking()
                    .Where(m => (m.SenderId == userId && m.ReceiverId == ownerId.Value) ||
                               (m.SenderId == ownerId.Value && m.ReceiverId == userId))
                    .OrderBy(m => m.SentAt)
                    .Select(m => new CustomerMessageViewModel
                    {
                        MessageId = m.MessageId,
                        Content = m.Content,
                        SentAt = m.SentAt,
                        IsFromMe = m.SenderId == userId
                    })
                    .ToListAsync();

                // If this is a new conversation (no messages yet), add a placeholder
                // so the chat panel renders with the input box
                if (!conversations.Any(c => c.OtherUserId == ownerId.Value))
                {
                    conversations.Insert(0, new CustomerConversationViewModel
                    {
                        OtherUserId = ownerId.Value,
                        OtherUserName = selectedOwnerName,
                        LastMessage = "Start a conversation...",
                        LastMessageTime = DateTime.Now,
                        UnreadCount = 0,
                        IsSelected = true
                    });
                }
            }

            var viewModel = new CustomerMessagesPageViewModel
            {
                Conversations = conversations,
                SelectedOwnerId = ownerId,
                SelectedOwnerName = selectedOwnerName,
                Messages = selectedMessages
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage([FromBody] CustomerSendMessageRequest request)
        {
            var userId = GetUserId();

            if (string.IsNullOrWhiteSpace(request.Content))
                return Json(new { success = false, error = "Message cannot be empty." });

            var receiver = await _context.Users.FindAsync(request.ReceiverId);
            if (receiver == null)
                return Json(new { success = false, error = "Recipient not found." });

            if (receiver.Role != UserRole.Owner)
                return Json(new { success = false, error = "You can only message venue owners." });

            // Try to find a booking between this customer and the owner's venue
            var bookingId = request.BookingId ?? await _context.Bookings
                .Where(b => b.CustomerId == userId && b.Venue.OwnerId == request.ReceiverId)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => (int?)b.BookingId)
                .FirstOrDefaultAsync();

            // If no booking exists, create a placeholder booking for the inquiry
            if (bookingId == null || bookingId == 0)
            {
                // Find the owner's first active venue to link the inquiry
                var venue = await _context.Venues
                    .Where(v => v.OwnerId == request.ReceiverId && v.IsActive)
                    .FirstOrDefaultAsync();

                if (venue == null)
                    return Json(new { success = false, error = "This owner has no active venues." });

                // Find an active theme for this venue (required FK)
                var themeId = await _context.Themes
                    .Where(t => t.VenueId == venue.VenueId && t.IsActive)
                    .Select(t => t.ThemeId)
                    .FirstOrDefaultAsync();

                if (themeId == 0)
                {
                    // Use any theme for this venue
                    themeId = await _context.Themes
                        .Where(t => t.VenueId == venue.VenueId)
                        .Select(t => t.ThemeId)
                        .FirstOrDefaultAsync();
                }

                if (themeId == 0)
                    return Json(new { success = false, error = "Cannot message this owner yet. The venue has no themes configured." });

                var inquiryBooking = new Booking
                {
                    VenueId = venue.VenueId,
                    ThemeId = themeId,
                    CustomerId = userId,
                    EventType = "Inquiry",
                    StartDate = DateTime.Now.AddDays(30),
                    EndDate = DateTime.Now.AddDays(30),
                    TotalPrice = 0,
                    BookingSource = "Inquiry",
                    Status = BookingStatus.Contacted,
                    PaymentStatus = PaymentStatus.Pending,
                    SpecialRequests = "Pre-booking inquiry via messaging",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Bookings.Add(inquiryBooking);
                await _context.SaveChangesAsync();
                bookingId = inquiryBooking.BookingId;
            }

            var message = new Message
            {
                BookingId = bookingId.Value,
                SenderId = userId,
                ReceiverId = request.ReceiverId,
                Content = request.Content.Trim(),
                SentAt = DateTime.Now,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = new
                {
                    messageId = message.MessageId,
                    content = message.Content,
                    sentAt = message.SentAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    isFromMe = true
                }
            });
        }

        // ==================== SETTINGS ====================

        public async Task<IActionResult> Settings()
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Login", "Auth");

            var viewModel = new CustomerSettingsViewModel
            {
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(CustomerSettingsViewModel model)
        {
            var userId = GetUserId();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please correct the errors.";
                return View("Settings", model);
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (user.Email != model.Email)
            {
                var exists = await _context.Users
                    .AnyAsync(u => u.Email.ToLower() == model.Email.ToLower() && u.UserId != userId);
                if (exists)
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    return View("Settings", model);
                }
            }

            user.Name = model.Name;
            user.Email = model.Email;
            user.Phone = model.Phone ?? "";
            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("UserName", user.Name);
            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Settings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = GetUserId();

            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                TempData["Error"] = "All password fields are required.";
                return RedirectToAction("Settings");
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "New passwords do not match.";
                return RedirectToAction("Settings");
            }

            if (newPassword.Length < 6)
            {
                TempData["Error"] = "Password must be at least 6 characters.";
                return RedirectToAction("Settings");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (user.PasswordHash != currentPassword)
            {
                TempData["Error"] = "Current password is incorrect.";
                return RedirectToAction("Settings");
            }

            user.PasswordHash = newPassword;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password changed successfully!";
            return RedirectToAction("Settings");
        }

        // ==================== THEME ====================

        // GET: /Customer/CreateTheme
        public IActionResult CreateTheme()
        {
            return View();
        }

        // POST: /Customer/CreateTheme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTheme(CreateThemeViewModel model)
        {
            var userId = GetUserId();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fill in all required fields.";
                return View(model);
            }

            // === One active theme per venue: deactivate others ===
            if (model.IsActive)
            {
                var otherActiveThemes = await _context.Themes
                    .Where(t => t.VenueId == model.VenueId && t.IsActive)
                    .ToListAsync();
                foreach (var other in otherActiveThemes)
                {
                    other.IsActive = false;
                }
            }

            var theme = new Theme
            {
                VenueId = model.VenueId,
                ThemeName = model.ThemeName,
                Description = model.Description,
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now
            };

            _context.Themes.Add(theme);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Theme created successfully!";
            return RedirectToAction("ManageThemes");
        }

        // GET: /Customer/ManageThemes
        public async Task<IActionResult> ManageThemes()
        {
            var userId = GetUserId();

            var themes = await _context.Themes
                .AsNoTracking()
                .Include(t => t.Venue)
                .Where(t => t.Venue.OwnerId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(themes);
        }

        // GET: /Customer/EditTheme/5
        public async Task<IActionResult> EditTheme(int id)
        {
            var userId = GetUserId();

            var theme = await _context.Themes
                .Include(t => t.Venue)
                .FirstOrDefaultAsync(t => t.ThemeId == id && t.Venue.OwnerId == userId);

            if (theme == null) return NotFound();

            var model = new EditThemeViewModel
            {
                ThemeId = theme.ThemeId,
                VenueId = theme.VenueId,
                ThemeName = theme.ThemeName,
                Description = theme.Description,
                IsActive = theme.IsActive
            };

            return View(model);
        }

        // POST: /Customer/EditTheme/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTheme(EditThemeViewModel model)
        {
            var userId = GetUserId();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please correct the errors.";
                return View(model);
            }

            var theme = await _context.Themes
                .Include(t => t.Venue)
                .FirstOrDefaultAsync(t => t.ThemeId == model.ThemeId && t.Venue.OwnerId == userId);

            if (theme == null) return NotFound();

            // === One active theme per venue: deactivate others ===
            if (model.IsActive)
            {
                var otherActiveThemes = await _context.Themes
                    .Where(t => t.VenueId == model.VenueId && t.IsActive && t.ThemeId != model.ThemeId)
                    .ToListAsync();
                foreach (var other in otherActiveThemes)
                {
                    other.IsActive = false;
                }
            }

            theme.VenueId = model.VenueId;
            theme.ThemeName = model.ThemeName;
            theme.Description = model.Description;
            theme.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Theme updated successfully!";
            return RedirectToAction("ManageThemes");
        }

        // POST: /Customer/DeleteTheme/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTheme(int id)
        {
            var userId = GetUserId();

            var theme = await _context.Themes
                .Include(t => t.Venue)
                .FirstOrDefaultAsync(t => t.ThemeId == id && t.Venue.OwnerId == userId);

            if (theme == null) return NotFound();

            _context.Themes.Remove(theme);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Theme deleted successfully!";
            return RedirectToAction("ManageThemes");
        }

        // TEMPORARY DEBUG — Remove after testing
        [AllowAnonymous]
        public async Task<IActionResult> DebugVenues()
        {
            var allVenues = await _context.Venues
                .AsNoTracking()
                .Include(v => v.Themes)
                    .ThenInclude(t => t.Pricings)
                .Select(v => new
                {
                    v.VenueId,
                    v.VenueName,
                    v.IsActive,
                    v.IsVerified,
                    ThemeCount = v.Themes.Count,
                    ActiveThemeCount = v.Themes.Count(t => t.IsActive),
                    Themes = v.Themes.Select(t => new
                    {
                        t.ThemeId,
                        t.ThemeName,
                        t.IsActive,
                        PricingCount = t.Pricings.Count,
                        ActivePricingCount = t.Pricings.Count(p => p.IsActive),
                        Pricings = t.Pricings.Select(p => new
                        {
                            p.PricingId,
                            p.EventType,
                            p.PricePerDay,
                            p.IsActive
                        })
                    })
                })
                .ToListAsync();

            return Json(allVenues, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
    }

    public class CustomerSendMessageRequest
    {
        public int ReceiverId { get; set; }
        public int? BookingId { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}