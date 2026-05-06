using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using VenueBooking.Data;
using VenueBooking.Models;
using VenueBooking.Models.Enums;
using VenueBooking.Services;
using VenueBooking.ViewModels;

namespace VenueBooking.Controllers
{
    [Authorize(Roles = UserRole.Owner)]
    public class OwnerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OwnerController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly RazorpayService _razorpay;

        public OwnerController(ApplicationDbContext context, ILogger<OwnerController> logger, IWebHostEnvironment webHostEnvironment, RazorpayService razorpay)
        {
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _razorpay = razorpay;
        }

        private int GetCurrentUserId()
        {
            var userIdStr = User.FindFirst("UserId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId) || userId == 0)
            {
                throw new UnauthorizedAccessException("Invalid user session.");
            }
            return userId;
        }

        private string NormalizeImageUrl(string imageUrl, string defaultUrl = "/images/default-theme.jpg")
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return defaultUrl;

            var url = imageUrl.StartsWith("/") ? imageUrl : "/" + imageUrl;
            var physical = Path.Combine(_webHostEnvironment.WebRootPath, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(physical))
                return url;

            var candidates = new[]
            {
                url.Replace("/uploads/themes/", "/upload/theme/"),
                url.Replace("/uploads/themes/", "/uploads/theme/"),
                url.Replace("/uploads/themes/", "/upload/themes/"),
                url.Replace("/uploads/theme/", "/uploads/themes/"),
                url.Replace("/upload/theme/", "/uploads/themes/"),
                "/uploads/themes/" + Path.GetFileName(url),
                "/upload/theme/" + Path.GetFileName(url),
                "/uploads/theme/" + Path.GetFileName(url),
                url.Replace("/uploads/venues/", "/upload/venue/"),
                url.Replace("/uploads/venues/", "/uploads/venue/"),
                url.Replace("/uploads/venues/", "/upload/venues/"),
                url.Replace("/uploads/venue/", "/uploads/venues/"),
                url.Replace("/upload/venue/", "/uploads/venues/"),
                "/uploads/venues/" + Path.GetFileName(url),
                "/upload/venue/" + Path.GetFileName(url),
                "/uploads/venue/" + Path.GetFileName(url)
            };

            foreach (var alt in candidates.Distinct())
            {
                var p = Path.Combine(_webHostEnvironment.WebRootPath, alt.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(p))
                    return alt;
            }

            return defaultUrl;
        }

        private async Task LoadOwnerStats(int userId)
        {
            ViewBag.VenueCount = await _context.Venues.CountAsync(v => v.OwnerId == userId);
            ViewBag.PendingBookingCount = await _context.Bookings
                .CountAsync(b => b.Venue.OwnerId == userId && b.Status == BookingStatus.Pending);
            ViewBag.UnreadMessageCount = await _context.Messages
                .CountAsync(m => m.ReceiverId == userId && !m.IsRead);

            var subscription = await _context.OwnerSubscriptions
                .FirstOrDefaultAsync(s => s.OwnerId == userId && s.IsActive && s.EndDate > DateTime.Now);

            ViewBag.SubscriptionDaysLeft = subscription != null
                ? (int)(subscription.EndDate - DateTime.Now).TotalDays
                : 0;
        }

        // ==================== DASHBOARD ====================

        public async Task<IActionResult> Dashboard()
        {
            var userId = GetCurrentUserId();

            var owner = await _context.Users
                .Include(u => u.Subscriptions)
                    .ThenInclude(s => s.Plan)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (owner == null) return RedirectToAction("Login", "Auth");

            var activeSubscription = owner.Subscriptions?
                .FirstOrDefault(s => s.IsActive && s.EndDate > DateTime.Now);

            var venues = await _context.Venues
                .Where(v => v.OwnerId == userId && v.IsActive)
                .ToListAsync();

            var venueIds = venues.Select(v => v.VenueId).ToList();

            var bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Venue)
                .Where(b => venueIds.Contains(b.VenueId))
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .ToListAsync();

            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var monthlyRevenue = await _context.Payments
                .Where(p => venueIds.Contains(p.Booking.VenueId) &&
                           p.Status == PaymentStatus.Completed &&
                           p.PaidAt.HasValue &&
                           p.PaidAt.Value.Month == currentMonth &&
                           p.PaidAt.Value.Year == currentYear)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var lastMonthRevenue = await _context.Payments
                .Where(p => venueIds.Contains(p.Booking.VenueId) &&
                           p.Status == PaymentStatus.Completed &&
                           p.PaidAt.HasValue &&
                           p.PaidAt.Value.Month == (currentMonth == 1 ? 12 : currentMonth - 1) &&
                           p.PaidAt.Value.Year == (currentMonth == 1 ? currentYear - 1 : currentYear))
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var revenueGrowth = lastMonthRevenue > 0
                ? (int)(((monthlyRevenue - lastMonthRevenue) / lastMonthRevenue) * 100)
                : 0;

            var reviews = await _context.Reviews
                .Where(r => venueIds.Contains(r.VenueId) && r.IsApproved)
                .ToListAsync();

            await LoadOwnerStats(userId);

            var viewModel = new OwnerDashboardViewModel
            {
                OwnerName = owner.Name?.Split(' ')[0] ?? "Owner",
                TotalVenues = venues.Count,
                NewVenuesThisMonth = venues.Count(v => v.CreatedAt.Month == currentMonth && v.CreatedAt.Year == currentYear),
                ActiveBookings = bookings.Count(b => b.Status == BookingStatus.Confirmed && b.StartDate >= DateTime.Now),
                PendingBookings = bookings.Count(b => b.Status == BookingStatus.Pending),
                TotalRevenue = monthlyRevenue,
                RevenueGrowth = revenueGrowth,
                AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0,
                TotalReviews = reviews.Count,
                SubscriptionPlanName = activeSubscription?.Plan?.PlanName ?? "Free",
                CurrentVenueCount = venues.Count,
                MaxVenuesAllowed = activeSubscription?.MaxVenuesAllowed ?? 1,
                VenueUsagePercentage = activeSubscription != null && activeSubscription.MaxVenuesAllowed > 0
                    ? Math.Min(venues.Count * 100.0 / activeSubscription.MaxVenuesAllowed, 100)
                    : 0,
                SubscriptionDaysLeft = (int)(ViewBag.SubscriptionDaysLeft ?? 0),
                RecentBookings = bookings.Select(b => new BookingListItemViewModel
                {
                    BookingId = b.BookingId,
                    CustomerName = b.Customer?.Name ?? "Unknown",
                    VenueName = b.Venue?.VenueName ?? "Unknown",
                    EventType = b.EventType,
                    StartDate = b.StartDate,
                    TotalPrice = b.TotalPrice,
                    Status = b.Status
                }).ToList()
            };

            return View(viewModel);
        }

        // ==================== VENUES ====================

        public async Task<IActionResult> Venues()
        {
            var userId = GetCurrentUserId();

            var subscription = await _context.OwnerSubscriptions
                .Include(s => s.Plan)
                .Where(s => s.OwnerId == userId && s.IsActive && s.EndDate > DateTime.Now)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync();

            var maxVenues = subscription?.MaxVenuesAllowed ?? 1;
            var planName = subscription?.Plan?.PlanName ?? "Free";
            var daysLeft = subscription != null ? (subscription.EndDate - DateTime.Now).Days : 0;

            var venues = await _context.Venues
                .Where(v => v.OwnerId == userId)
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new OwnerVenueListItemViewModel
                {
                    VenueId = v.VenueId,
                    VenueName = v.VenueName,
                    City = v.City,
                    State = v.State,
                    Capacity = v.Capacity,
                    IsActive = v.IsActive,
                    IsVerified = v.IsVerified,
                    CoverImageUrl = v.Images.FirstOrDefault(i => i.IsCover) != null
                        ? v.Images.FirstOrDefault(i => i.IsCover)!.ImageUrl
                        : v.Images.FirstOrDefault() != null
                            ? v.Images.FirstOrDefault()!.ImageUrl
                            : "/images/default-venue.jpg",
                    CreatedAt = v.CreatedAt,
                    ThemeCount = v.Themes.Count,
                    BookingCount = v.Bookings.Count
                })
                .ToListAsync();

            await LoadOwnerStats(userId);

            var viewModel = new MyVenuesViewModel
            {
                Venues = venues,
                TotalVenues = venues.Count,
                MaxVenuesAllowed = maxVenues,
                CurrentPlanName = planName,
                SubscriptionDaysLeft = daysLeft
            };

            return View(viewModel);
        }

        public async Task<IActionResult> CreateVenue()
        {
            var userId = GetCurrentUserId();

            var subscription = await _context.OwnerSubscriptions
                .Where(s => s.OwnerId == userId && s.IsActive && s.EndDate > DateTime.Now)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync();

            var currentVenueCount = await _context.Venues.CountAsync(v => v.OwnerId == userId);
            var maxVenues = subscription?.MaxVenuesAllowed ?? 1;

            if (currentVenueCount >= maxVenues)
            {
                TempData["Error"] = $"You have reached your limit of {maxVenues} venues. Please upgrade your subscription.";
                return RedirectToAction("Subscription");
            }

            var amenities = await _context.Amenities
                .OrderBy(a => a.Category)
                .ThenBy(a => a.Name)
                .ToListAsync();

            ViewBag.Amenities = amenities;
            ViewBag.VenueTypes = new List<string>
            {
                VenueTypes.MarriageHall, VenueTypes.PartyPlot, VenueTypes.BanquetHall,
                VenueTypes.ConferenceHall, VenueTypes.Resort, VenueTypes.FarmHouse,
                VenueTypes.CommunityCenter, VenueTypes.Hotel, VenueTypes.Restaurant, VenueTypes.Other
            };

            await LoadOwnerStats(userId);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVenue(CreateVenueViewModel model)
        {
            var userId = GetCurrentUserId();

            if (!ModelState.IsValid)
            {
                ViewBag.Amenities = await _context.Amenities.OrderBy(a => a.Name).ToListAsync();
                ViewBag.VenueTypes = new List<string>
                {
                    VenueTypes.MarriageHall, VenueTypes.PartyPlot, VenueTypes.BanquetHall,
                    VenueTypes.ConferenceHall, VenueTypes.Resort, VenueTypes.FarmHouse,
                    VenueTypes.CommunityCenter, VenueTypes.Hotel, VenueTypes.Restaurant, VenueTypes.Other
                };
                await LoadOwnerStats(userId);
                return View(model);
            }

            var subscription = await _context.OwnerSubscriptions
                .Where(s => s.OwnerId == userId && s.IsActive && s.EndDate > DateTime.Now)
                .FirstOrDefaultAsync();

            var currentVenueCount = await _context.Venues.CountAsync(v => v.OwnerId == userId);
            if (currentVenueCount >= (subscription?.MaxVenuesAllowed ?? 1))
            {
                TempData["Error"] = "Venue limit reached. Please upgrade your subscription.";
                return RedirectToAction("Subscription");
            }

            var venue = new Venue
            {
                OwnerId = userId,
                VenueName = model.VenueName,
                Address = model.Address,
                City = model.City,
                State = model.State,
                Country = model.Country,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                Capacity = model.Capacity,
                Description = model.Description,
                ContactPhone = model.ContactPhone,
                VenueType = model.VenueType,
                Slug = model.VenueName.ToLower().Replace(" ", "-") + "-" + Guid.NewGuid().ToString()[..8],
                IsActive = true,
                IsVerified = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Venues.Add(venue);
            await _context.SaveChangesAsync();

            if (model.SelectedAmenities != null && model.SelectedAmenities.Any())
            {
                foreach (var amenityId in model.SelectedAmenities)
                {
                    _context.VenueAmenities.Add(new VenueAmenity
                    {
                        VenueId = venue.VenueId,
                        AmenityId = amenityId,
                        IsChargeable = model.AmenityPrices != null && model.AmenityPrices.ContainsKey(amenityId) && model.AmenityPrices[amenityId].HasValue,
                        Price = model.AmenityPrices != null && model.AmenityPrices.ContainsKey(amenityId) ? model.AmenityPrices[amenityId] : null,
                        Notes = string.Empty // ← FIX: database column is NOT NULL
                    });
                }
                await _context.SaveChangesAsync();
            }

            if (model.Images != null && model.Images.Any())
            {
                var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "venues");
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                bool isFirstImage = true;
                int displayOrder = 0;

                foreach (var image in model.Images.Take(7))
                {
                    if (image.Length > 0 && image.Length <= 5 * 1024 * 1024)
                    {
                        var extension = Path.GetExtension(image.FileName).ToLower();
                        if (extension is ".jpg" or ".jpeg" or ".png" or ".webp")
                        {
                            var fileName = $"{venue.VenueId}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString()[..8]}{extension}";
                            var filePath = Path.Combine(uploadPath, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }

                            _context.VenueImages.Add(new VenueImage
                            {
                                VenueId = venue.VenueId,
                                ImageUrl = $"/uploads/venues/{fileName}",
                                IsCover = isFirstImage,
                                DisplayOrder = displayOrder,
                                UploadedAt = DateTime.Now,
                                Caption = string.Empty
                            });

                            isFirstImage = false;
                            displayOrder++;
                        }
                    }
                }
                await _context.SaveChangesAsync();
            }

            if (subscription != null)
            {
                subscription.CurrentVenueCount = currentVenueCount + 1;
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Venue created successfully! It will be visible after admin verification.";
            return RedirectToAction("Venues");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVenueStatus(int id)
        {
            var userId = GetCurrentUserId();
            var venue = await _context.Venues.FirstOrDefaultAsync(v => v.VenueId == id && v.OwnerId == userId);
            if (venue == null) return NotFound();

            venue.IsActive = !venue.IsActive;
            venue.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isActive = venue.IsActive });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVenue(int id)
        {
            var userId = GetCurrentUserId();
            var venue = await _context.Venues
                .Include(v => v.Bookings)
                .FirstOrDefaultAsync(v => v.VenueId == id && v.OwnerId == userId);

            if (venue == null) return NotFound();

            if (venue.Bookings.Any(b => b.Status == BookingStatus.Confirmed && b.StartDate >= DateTime.Now))
            {
                TempData["Error"] = "Cannot delete venue with active future bookings.";
                return RedirectToAction("Venues");
            }

            var images = await _context.VenueImages.Where(i => i.VenueId == id).ToListAsync();
            foreach (var image in images)
            {
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, image.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                _context.VenueImages.Remove(image);
            }

            _context.Venues.Remove(venue);
            await _context.SaveChangesAsync();

            var subscription = await _context.OwnerSubscriptions.FirstOrDefaultAsync(s => s.OwnerId == userId && s.IsActive);
            if (subscription != null)
            {
                subscription.CurrentVenueCount = await _context.Venues.CountAsync(v => v.OwnerId == userId);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Venue deleted successfully!";
            return RedirectToAction("Venues");
        }

        // ==================== EDIT VENUE ====================

        public async Task<IActionResult> EditVenue(int id)
        {
            var userId = GetCurrentUserId();
            var venue = await _context.Venues
                .Include(v => v.Images)
                .Include(v => v.VenueAmenities)
                .FirstOrDefaultAsync(v => v.VenueId == id && v.OwnerId == userId);

            if (venue == null) return NotFound();

            var viewModel = new EditVenueViewModel
            {
                VenueId = venue.VenueId,
                VenueName = venue.VenueName,
                Address = venue.Address,
                City = venue.City,
                State = venue.State,
                Country = venue.Country,
                Latitude = venue.Latitude,
                Longitude = venue.Longitude,
                Capacity = venue.Capacity,
                Description = venue.Description,
                ContactPhone = venue.ContactPhone,
                VenueType = venue.VenueType,
                IsActive = venue.IsActive,
                ExistingImages = venue.Images.OrderBy(i => i.DisplayOrder).Select(i => new ExistingImageViewModel
                {
                    ImageId = i.ImageId,
                    ImageUrl = i.ImageUrl,
                    IsCover = i.IsCover,
                    DisplayOrder = i.DisplayOrder
                }).ToList(),
                SelectedAmenities = venue.VenueAmenities.Select(va => va.AmenityId).ToList(),
                AmenityPrices = venue.VenueAmenities.ToDictionary(va => va.AmenityId, va => va.Price)
            };

            ViewBag.Amenities = await _context.Amenities.OrderBy(a => a.Name).ToListAsync();
            ViewBag.VenueTypes = new List<string>
            {
                VenueTypes.MarriageHall, VenueTypes.PartyPlot, VenueTypes.BanquetHall,
                VenueTypes.ConferenceHall, VenueTypes.Resort, VenueTypes.FarmHouse,
                VenueTypes.CommunityCenter, VenueTypes.Hotel, VenueTypes.Restaurant, VenueTypes.Other
            };

            await LoadOwnerStats(userId);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVenue(int id, EditVenueViewModel model)
        {
            if (id != model.VenueId) return BadRequest();

            var userId = GetCurrentUserId();
            var venue = await _context.Venues
                .Include(v => v.Images)
                .Include(v => v.VenueAmenities)
                .FirstOrDefaultAsync(v => v.VenueId == id && v.OwnerId == userId);

            if (venue == null) return NotFound();

            if (!ModelState.IsValid)
            {
                model.ExistingImages = venue.Images.OrderBy(i => i.DisplayOrder).Select(i => new ExistingImageViewModel
                {
                    ImageId = i.ImageId,
                    ImageUrl = i.ImageUrl,
                    IsCover = i.IsCover,
                    DisplayOrder = i.DisplayOrder
                }).ToList();
                ViewBag.Amenities = await _context.Amenities.OrderBy(a => a.Name).ToListAsync();
                ViewBag.VenueTypes = new List<string> { VenueTypes.MarriageHall, VenueTypes.PartyPlot, VenueTypes.BanquetHall, VenueTypes.ConferenceHall, VenueTypes.Resort, VenueTypes.FarmHouse, VenueTypes.CommunityCenter, VenueTypes.Hotel, VenueTypes.Restaurant, VenueTypes.Other };
                await LoadOwnerStats(userId);
                return View(model);
            }

            var currentImageCount = venue.Images.Count;
            var imagesToDeleteCount = model.ImagesToDelete?.Count ?? 0;
            var newImagesCount = model.NewImages?.Count(i => i.Length > 0) ?? 0;
            var finalImageCount = currentImageCount - imagesToDeleteCount + newImagesCount;

            if (finalImageCount > 7 || finalImageCount == 0)
            {
                ModelState.AddModelError("", finalImageCount > 7
                    ? $"Maximum 7 images allowed. Final count would be {finalImageCount}."
                    : "At least one image is required.");
                model.ExistingImages = venue.Images.OrderBy(i => i.DisplayOrder).Select(i => new ExistingImageViewModel
                {
                    ImageId = i.ImageId,
                    ImageUrl = i.ImageUrl,
                    IsCover = i.IsCover,
                    DisplayOrder = i.DisplayOrder
                }).ToList();
                ViewBag.Amenities = await _context.Amenities.OrderBy(a => a.Name).ToListAsync();
                ViewBag.VenueTypes = new List<string> { VenueTypes.MarriageHall, VenueTypes.PartyPlot, VenueTypes.BanquetHall, VenueTypes.ConferenceHall, VenueTypes.Resort, VenueTypes.FarmHouse, VenueTypes.CommunityCenter, VenueTypes.Hotel, VenueTypes.Restaurant, VenueTypes.Other };
                await LoadOwnerStats(userId);
                return View(model);
            }

            venue.VenueName = model.VenueName;
            venue.Address = model.Address;
            venue.City = model.City;
            venue.State = model.State;
            venue.Country = model.Country;
            venue.Latitude = model.Latitude;
            venue.Longitude = model.Longitude;
            venue.Capacity = model.Capacity;
            venue.Description = model.Description;
            venue.ContactPhone = model.ContactPhone;
            venue.VenueType = model.VenueType;
            venue.IsActive = model.IsActive;
            venue.UpdatedAt = DateTime.Now;

            if (model.ImagesToDelete != null && model.ImagesToDelete.Any())
            {
                foreach (var image in venue.Images.Where(i => model.ImagesToDelete.Contains(i.ImageId)).ToList())
                {
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, image.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                    _context.VenueImages.Remove(image);
                }
            }

            if (model.NewImages != null && model.NewImages.Any())
            {
                var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "venues");
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
                var maxOrder = venue.Images.Any() ? venue.Images.Max(i => i.DisplayOrder) + 1 : 0;
                foreach (var image in model.NewImages.Take(7))
                {
                    if (image.Length > 0 && image.Length <= 5 * 1024 * 1024)
                    {
                        var ext = Path.GetExtension(image.FileName).ToLower();
                        if (ext is ".jpg" or ".jpeg" or ".png" or ".webp")
                        {
                            var fileName = $"{venue.VenueId}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString()[..8]}{ext}";
                            using var stream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create);
                            await image.CopyToAsync(stream);
                            _context.VenueImages.Add(new VenueImage
                            {
                                VenueId = venue.VenueId,
                                ImageUrl = $"/uploads/venues/{fileName}",
                                IsCover = false,
                                DisplayOrder = maxOrder++,
                                UploadedAt = DateTime.Now,
                                Caption = string.Empty
                            });
                        }
                    }
                }
            }

            var remainingImages = await _context.VenueImages.Where(i => i.VenueId == venue.VenueId).ToListAsync();
            if (model.NewCoverImageId > 0)
            {
                foreach (var img in remainingImages) img.IsCover = (img.ImageId == model.NewCoverImageId);
            }
            else if (model.NewCoverImageId == -1 && model.NewImages != null && model.NewImages.Any())
            {
                var newImage = remainingImages.OrderByDescending(i => i.UploadedAt).FirstOrDefault();
                if (newImage != null) foreach (var img in remainingImages) img.IsCover = (img.ImageId == newImage.ImageId);
            }
            else if (!remainingImages.Any(i => i.IsCover) && remainingImages.Any())
            {
                remainingImages.First().IsCover = true;
            }

            _context.VenueAmenities.RemoveRange(venue.VenueAmenities);
            if (model.SelectedAmenities != null)
            {
                foreach (var amenityId in model.SelectedAmenities)
                {
                    _context.VenueAmenities.Add(new VenueAmenity
                    {
                        VenueId = venue.VenueId,
                        AmenityId = amenityId,
                        IsChargeable = model.AmenityPrices != null && model.AmenityPrices.ContainsKey(amenityId) && model.AmenityPrices[amenityId].HasValue,
                        Price = model.AmenityPrices != null && model.AmenityPrices.ContainsKey(amenityId) ? model.AmenityPrices[amenityId] : null,
                        Notes = string.Empty // ← FIX: database column is NOT NULL
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Venue updated successfully!";
            return RedirectToAction("Venues");
        }

        // ==================== THEMES ====================

        public async Task<IActionResult> Themes()
        {
            var userId = GetCurrentUserId();

            var themes = await _context.Themes
                .AsNoTracking()
                .Where(t => t.Venue.OwnerId == userId)
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
                    Price = t.Pricings.Where(p => p.IsActive).Any()
                            ? t.Pricings.Where(p => p.IsActive).Min(p => p.PricePerDay)
                            : (decimal?)null,
                    EventType = t.Pricings.Where(p => p.IsActive)
                                .OrderBy(p => p.PricePerDay)
                                .Select(p => p.EventType)
                                .FirstOrDefault() ?? "",
                    Pricings = t.Pricings.Where(p => p.IsActive).Select(p => new ThemePricingDisplay
                    {
                        EventType = p.EventType,
                        PricePerDay = p.PricePerDay
                    }).ToList()
                }).ToListAsync();

            foreach (var t in themes)
                t.CoverImageUrl = NormalizeImageUrl(t.CoverImageUrl);

            ViewBag.Venues = await _context.Venues.Where(v => v.OwnerId == userId)
                .Select(v => new { v.VenueId, v.VenueName }).ToListAsync();
            await LoadOwnerStats(userId);
            return View(themes);
        }

        public async Task<IActionResult> CreateTheme()
        {
            var userId = GetCurrentUserId();
            var venues = await _context.Venues.Where(v => v.OwnerId == userId && v.IsActive)
                .Select(v => new { v.VenueId, v.VenueName }).ToListAsync();

            if (!venues.Any())
            {
                TempData["Error"] = "You need to create a venue first before adding themes.";
                return RedirectToAction("Venues");
            }

            ViewBag.Venues = venues;
            await LoadOwnerStats(userId);
            return View(new CreateThemeViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTheme(CreateThemeViewModel model)
        {
            var userId = GetCurrentUserId();
            var venue = await _context.Venues.FirstOrDefaultAsync(v => v.VenueId == model.VenueId && v.OwnerId == userId);
            if (venue == null) ModelState.AddModelError("VenueId", "Invalid venue selected.");

            var validPricings = model.Pricings?.Where(p => p.PricePerDay > 0).ToList() ?? [];
            if (validPricings.Count == 0)
                ModelState.AddModelError("Pricings", "At least one pricing entry with a price is required.");

            if (!ModelState.IsValid)
            {
                ViewBag.Venues = await _context.Venues.Where(v => v.OwnerId == userId && v.IsActive)
                    .Select(v => new { v.VenueId, v.VenueName }).ToListAsync();
                await LoadOwnerStats(userId);
                return View(model);
            }

            // Use a transaction so partial data isn't left behind on error
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (model.IsActive)
                {
                    var otherActiveThemes = await _context.Themes
                        .Where(t => t.VenueId == model.VenueId && t.IsActive)
                        .ToListAsync();
                    foreach (var other in otherActiveThemes)
                    {
                        other.IsActive = false;
                    }
                    await _context.SaveChangesAsync();
                }

                var theme = new Theme
                {
                    VenueId = model.VenueId,
                    ThemeName = model.ThemeName ?? "Untitled Theme",
                    Description = model.Description ?? string.Empty, // ← FIX: DB column is NOT NULL
                    IsActive = model.IsActive,
                    IsDefault = model.IsDefault,
                    CreatedAt = DateTime.Now
                };
                _context.Themes.Add(theme);
                await _context.SaveChangesAsync();

                foreach (var p in validPricings)
                {
                    _context.ThemePricings.Add(new ThemePricing
                    {
                        ThemeId = theme.ThemeId,
                        EventType = p.EventType ?? string.Empty,
                        PricePerDay = p.PricePerDay,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    });
                }
                await _context.SaveChangesAsync();

                if (model.Images != null && model.Images.Any())
                {
                    var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "themes");
                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
                    bool isFirst = true; int order = 0;
                    foreach (var image in model.Images.Take(10))
                    {
                        if (image.Length > 0 && image.Length <= 5 * 1024 * 1024)
                        {
                            var ext = Path.GetExtension(image.FileName).ToLower();
                            if (ext is ".jpg" or ".jpeg" or ".png" or ".webp")
                            {
                                var fileName = $"{theme.ThemeId}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
                                using var stream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create);
                                await image.CopyToAsync(stream);
                                _context.ThemeImages.Add(new ThemeImage
                                {
                                    ThemeId = theme.ThemeId,
                                    ImageUrl = $"/uploads/themes/{fileName}",
                                    Caption = string.Empty, // ← FIX: DB column is NOT NULL
                                    IsCover = isFirst,
                                    DisplayOrder = order++,
                                    UploadedAt = DateTime.Now
                                });
                                isFirst = false;
                            }
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                TempData["Success"] = "Theme created successfully!";
                return RedirectToAction("Themes");
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error creating theme for owner {OwnerId}", userId);
                TempData["Error"] = "A database error occurred while creating the theme. Please try again.";
                ViewBag.Venues = await _context.Venues.Where(v => v.OwnerId == userId && v.IsActive)
                    .Select(v => new { v.VenueId, v.VenueName }).ToListAsync();
                await LoadOwnerStats(userId);
                return View(model);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Unexpected error creating theme for owner {OwnerId}", userId);
                TempData["Error"] = "An unexpected error occurred. Please try again.";
                ViewBag.Venues = await _context.Venues.Where(v => v.OwnerId == userId && v.IsActive)
                    .Select(v => new { v.VenueId, v.VenueName }).ToListAsync();
                await LoadOwnerStats(userId);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleThemeStatus(int id)
        {
            var userId = GetCurrentUserId();
            var theme = await _context.Themes.Include(t => t.Venue)
                .FirstOrDefaultAsync(t => t.ThemeId == id && t.Venue.OwnerId == userId);
            if (theme == null) return Json(new { success = false });

            // If activating this theme, deactivate all others for the same venue
            if (!theme.IsActive)
            {
                var otherActiveThemes = await _context.Themes
                    .Where(t => t.VenueId == theme.VenueId && t.IsActive && t.ThemeId != id)
                    .ToListAsync();
                foreach (var other in otherActiveThemes)
                {
                    other.IsActive = false;
                }
                theme.IsActive = true;
            }
            else
            {
                theme.IsActive = false;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, isActive = theme.IsActive });
        }

        // ==================== BOOKINGS ====================

        public async Task<IActionResult> Bookings()
        {
            var userId = GetCurrentUserId();
            var bookings = await _context.Bookings
                .Include(b => b.Customer).Include(b => b.Venue).Include(b => b.Theme)
                .Where(b => b.Venue.OwnerId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            await LoadOwnerStats(userId);
            return View(bookings);
        }

        public async Task<IActionResult> BookingDetails(int id)
        {
            var userId = GetCurrentUserId();
            var booking = await _context.Bookings
                .Include(b => b.Customer).Include(b => b.Venue).ThenInclude(v => v.Images)
                .Include(b => b.Theme).Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.BookingId == id && b.Venue.OwnerId == userId);
            if (booking == null) return NotFound();

            await LoadOwnerStats(userId);
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(int id)
        {
            var userId = GetCurrentUserId();
            var booking = await _context.Bookings.Include(b => b.Venue)
                .FirstOrDefaultAsync(b => b.BookingId == id && b.Venue.OwnerId == userId);
            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Contacted)
            {
                TempData["Error"] = "Booking cannot be confirmed.";
                return RedirectToAction("Bookings");
            }

            booking.Status = BookingStatus.Confirmed;
            booking.UpdatedAt = DateTime.Now;

            if (!await _context.VenueBlockedDates.AnyAsync(b => b.VenueId == booking.VenueId && b.BlockDate.Date == booking.StartDate.Date))
            {
                _context.VenueBlockedDates.Add(new VenueBlockedDate
                {
                    VenueId = booking.VenueId,
                    BlockDate = booking.StartDate,
                    Reason = $"Booked: {booking.EventType}",
                    CreatedBy = userId,
                    CreatedAt = DateTime.Now
                });
            }
            await _context.SaveChangesAsync();

            if (booking.CustomerId.HasValue)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = booking.CustomerId.Value,
                    Title = "Booking Confirmed!",
                    Message = $"Your booking #{booking.BookingId} for {booking.Venue.VenueName} on {booking.StartDate:MMM dd, yyyy} has been confirmed.",
                    Type = "Booking",
                    IsRead = false,
                    ActionUrl = $"/Customer/BookingDetails/{booking.BookingId}",
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Booking confirmed and date blocked.";
            return RedirectToAction("Bookings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectBooking(int id, string rejectionReason)
        {
            var userId = GetCurrentUserId();
            var booking = await _context.Bookings.Include(b => b.Venue)
                .FirstOrDefaultAsync(b => b.BookingId == id && b.Venue.OwnerId == userId);
            if (booking == null) return NotFound();

            booking.Status = BookingStatus.Rejected;
            booking.SpecialRequests = $"REJECTED: {rejectionReason}\n\nOriginal Request: {booking.SpecialRequests}";
            booking.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            if (booking.CustomerId.HasValue)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = booking.CustomerId.Value,
                    Title = "Booking Rejected",
                    Message = $"Your booking #{booking.BookingId} for {booking.Venue.VenueName} has been rejected. Reason: {rejectionReason}",
                    Type = "Booking",
                    IsRead = false,
                    ActionUrl = $"/Customer/BookingDetails/{booking.BookingId}",
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Booking rejected.";
            return RedirectToAction("Bookings");
        }

        // ==================== SUBSCRIPTION ====================

        public async Task<IActionResult> Subscription()
        {
            var userId = GetCurrentUserId();
            var currentSubscription = await _context.OwnerSubscriptions.Include(s => s.Plan)
                .Where(s => s.OwnerId == userId && s.IsActive && s.EndDate > DateTime.Now)
                .OrderByDescending(s => s.EndDate).FirstOrDefaultAsync();

            var plans = await _context.SubscriptionPlans.Where(p => p.IsActive).OrderBy(p => p.Price).ToListAsync();

            var subscriptionPayments = await _context.OwnerSubscriptions.Include(s => s.Plan)
                .Where(s => s.OwnerId == userId && s.PaymentStatus == PaymentStatus.Completed)
                .OrderByDescending(s => s.StartDate)
                .Select(s => new PaymentHistoryViewModel
                {
                    PaymentDate = s.StartDate,
                    TransactionId = s.TransactionId ?? "N/A",
                    Amount = s.Plan.Price,
                    Status = s.PaymentStatus,
                    ReceiptUrl = "#"
                }).ToListAsync();

            var viewModel = new SubscriptionViewModel
            {
                CurrentPlanName = currentSubscription?.Plan?.PlanName ?? "Free",
                NextBillingDate = currentSubscription?.EndDate,
                IsActive = currentSubscription != null,
                DaysLeft = currentSubscription != null ? (currentSubscription.EndDate - DateTime.Now).Days : 0,
                Plans = plans.Select(p => new PlanViewModel
                {
                    PlanId = p.PlanId,
                    PlanName = p.PlanName,
                    Price = p.Price,
                    DurationInDays = p.DurationInDays,
                    MaxVenuesAllowed = p.MaxVenuesAllowed,
                    Description = p.Description,
                    FeaturesList = ParseFeatures(p.Features),
                    IsCurrent = currentSubscription?.PlanId == p.PlanId,
                    IsPopular = p.PlanName.Contains("Professional", StringComparison.OrdinalIgnoreCase)
                }).ToList(),
                Payments = subscriptionPayments
            };

            await LoadOwnerStats(userId);
            return View(viewModel);
        }

        private List<string> ParseFeatures(string featuresJson)
        {
            if (string.IsNullOrEmpty(featuresJson)) return [];
            try { return JsonSerializer.Deserialize<List<string>>(featuresJson) ?? []; }
            catch { return []; }
        }

        // ==================== SUBSCRIPTION PAYMENT (RAZORPAY) ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PurchaseSubscription(int planId)
        {
            var userId = GetCurrentUserId();
            var plan = await _context.SubscriptionPlans.FindAsync(planId);

            if (plan == null || !plan.IsActive)
            {
                TempData["Error"] = "Selected plan is not available.";
                return RedirectToAction("Subscription");
            }

            var existing = await _context.OwnerSubscriptions
                .FirstOrDefaultAsync(s => s.OwnerId == userId && s.IsActive && s.EndDate > DateTime.Now);

            if (existing != null)
            {
                TempData["Error"] = $"You already have an active subscription (expires {existing.EndDate:MMM dd, yyyy}).";
                return RedirectToAction("Subscription");
            }

            // Clean up old pending/failed subscriptions to avoid duplicates
            var stalePending = await _context.OwnerSubscriptions
                .Where(s => s.OwnerId == userId
                         && !s.IsActive
                         && (s.PaymentStatus == PaymentStatus.Pending || s.PaymentStatus == PaymentStatus.Failed))
                .ToListAsync();

            if (stalePending.Any())
            {
                _logger.LogInformation("Removing {Count} stale pending subscriptions for owner {OwnerId}", stalePending.Count, userId);
                _context.OwnerSubscriptions.RemoveRange(stalePending);
                await _context.SaveChangesAsync();
            }

            // Create a new pending subscription record
            var subscription = new OwnerSubscription
            {
                OwnerId = userId,
                PlanId = plan.PlanId,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(plan.DurationInDays),
                PaymentStatus = PaymentStatus.Pending,
                TransactionId = string.Empty,
                MaxVenuesAllowed = plan.MaxVenuesAllowed,
                CurrentVenueCount = await _context.Venues.CountAsync(v => v.OwnerId == userId),
                IsActive = false,
                CreatedAt = DateTime.Now
            };

            _context.OwnerSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created pending subscription {SubscriptionId} for owner {OwnerId}, plan {PlanName}",
                subscription.SubscriptionId, userId, plan.PlanName);

            return RedirectToAction("SubscriptionPayment", new { subscriptionId = subscription.SubscriptionId });
        }

        public async Task<IActionResult> SubscriptionPayment(int subscriptionId)
        {
            var userId = GetCurrentUserId();
            var subscription = await _context.OwnerSubscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId && s.OwnerId == userId);

            if (subscription == null)
            {
                TempData["Error"] = "Subscription not found.";
                return RedirectToAction("Subscription");
            }

            if (subscription.PaymentStatus == PaymentStatus.Completed)
            {
                TempData["Info"] = "This subscription is already paid.";
                return RedirectToAction("Subscription");
            }

            try
            {
                var receipt = $"SUB-{subscription.SubscriptionId}-{DateTime.Now:yyyyMMddHHmmss}";
                var order = await _razorpay.CreateOrderAsync(subscription.Plan.Price, "INR", receipt);

                if (order == null || !order.ContainsKey("id"))
                {
                    _logger.LogError("Razorpay CreateOrderAsync returned null/missing 'id' for subscription {SubscriptionId}", subscriptionId);
                    TempData["Error"] = "Failed to create payment order. Please try again.";
                    return RedirectToAction("Subscription");
                }

                var razorpayOrderId = order["id"].ToString()!;
                _logger.LogInformation("Razorpay order {OrderId} created for subscription {SubscriptionId}", razorpayOrderId, subscriptionId);

                ViewBag.RazorpayKeyId = _razorpay.KeyId;
                ViewBag.RazorpayOrderId = razorpayOrderId;
                ViewBag.OrderAmount = (int)(subscription.Plan.Price * 100);
                var owner = await _context.Users.FindAsync(userId);
                ViewBag.OwnerName = owner?.Name ?? "Owner";
                ViewBag.OwnerEmail = owner?.Email ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Razorpay order for subscription {SubscriptionId}", subscriptionId);
                TempData["Error"] = "Payment gateway error. Please try again later.";
                return RedirectToAction("Subscription");
            }

            await LoadOwnerStats(userId);
            return View(subscription);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmSubscriptionPayment(
            int subscriptionId,
            string razorpay_payment_id,
            string razorpay_order_id,
            string razorpay_signature)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation(
                "ConfirmSubscriptionPayment — SubscriptionId={SubscriptionId}, PaymentId={PaymentId}, OrderId={OrderId}",
                subscriptionId, razorpay_payment_id ?? "(null)", razorpay_order_id ?? "(null)");

            // Validate callback parameters
            if (string.IsNullOrWhiteSpace(razorpay_payment_id) ||
                string.IsNullOrWhiteSpace(razorpay_order_id) ||
                string.IsNullOrWhiteSpace(razorpay_signature))
            {
                _logger.LogWarning("Missing Razorpay callback parameters for subscription {SubscriptionId}", subscriptionId);
                TempData["Error"] = "Payment data incomplete. Please try again.";
                return RedirectToAction("Subscription");
            }

            var subscription = await _context.OwnerSubscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId && s.OwnerId == userId);

            if (subscription == null)
            {
                _logger.LogWarning("Subscription {SubscriptionId} not found for owner {OwnerId}", subscriptionId, userId);
                TempData["Error"] = "Subscription not found.";
                return RedirectToAction("Subscription");
            }

            if (subscription.PaymentStatus == PaymentStatus.Completed)
            {
                _logger.LogInformation("Subscription {SubscriptionId} already completed — ignoring duplicate", subscriptionId);
                TempData["Info"] = "Payment already completed.";
                return RedirectToAction("Subscription");
            }

            // Verify the payment signature
            var isValid = _razorpay.VerifyPaymentSignature(razorpay_order_id, razorpay_payment_id, razorpay_signature);

            if (!isValid)
            {
                _logger.LogWarning("Invalid Razorpay signature for subscription {SubscriptionId}", subscriptionId);
                subscription.PaymentStatus = PaymentStatus.Failed;
                await _context.SaveChangesAsync();

                TempData["Error"] = "Payment verification failed. If money was deducted, it will be refunded automatically.";
                return RedirectToAction("Subscription");
            }

            _logger.LogInformation("Razorpay signature verified for subscription {SubscriptionId}. Activating...", subscriptionId);

            // ✅ Payment verified — update ONLY the OwnerSubscriptions table
            // Do Not insert into Payments table (that's for customer booking payments only)
            subscription.PaymentStatus = PaymentStatus.Completed;
            subscription.TransactionId = razorpay_payment_id;
            subscription.IsActive = true;
            subscription.StartDate = DateTime.Now;
            subscription.EndDate = DateTime.Now.AddDays(subscription.Plan.DurationInDays);

            // Notify the owner
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = "Subscription Activated!",
                Message = $"Your \"{subscription.Plan.PlanName}\" subscription is now active until {subscription.EndDate:MMM dd, yyyy}.",
                Type = "Subscription",
                IsRead = false,
                ActionUrl = "/Owner/Subscription",
                CreatedAt = DateTime.Now
            });

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Subscription {SubscriptionId} activated. Plan={PlanName}, Expires={EndDate}, TxnId={TxnId}",
                    subscriptionId, subscription.Plan.PlanName, subscription.EndDate, razorpay_payment_id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error activating subscription {SubscriptionId}", subscriptionId);
                TempData["Error"] = "A database error occurred. Please contact support.";
                return RedirectToAction("Subscription");
            }

            TempData["Success"] = $"Payment successful! Your \"{subscription.Plan.PlanName}\" plan is now active.";
            return RedirectToAction("Subscription");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubscriptionPaymentFailed(int subscriptionId)
        {
            var userId = GetCurrentUserId();

            _logger.LogWarning("SubscriptionPaymentFailed for subscription {SubscriptionId}, owner {OwnerId}", subscriptionId, userId);

            var subscription = await _context.OwnerSubscriptions
                .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId && s.OwnerId == userId);

            if (subscription != null && subscription.PaymentStatus == PaymentStatus.Pending)
            {
                subscription.PaymentStatus = PaymentStatus.Failed;
                await _context.SaveChangesAsync();
            }

            TempData["Error"] = "Payment was cancelled or failed. Please try again.";
            return RedirectToAction("Subscription");
        }

        // ==================== REVIEWS ====================

        public async Task<IActionResult> Reviews(int? rating = null, int page = 1)
        {
            var userId = GetCurrentUserId();
            const int pageSize = 10;

            var venueIds = await _context.Venues.Where(v => v.OwnerId == userId).Select(v => v.VenueId).ToListAsync();
            if (!venueIds.Any())
            {
                await LoadOwnerStats(userId);
                return View(new ReviewsDashboardViewModel { Reviews = [], AverageRating = 0, TotalReviews = 0, PendingReplyCount = 0 });
            }

            var query = _context.Reviews.Include(r => r.Venue).Include(r => r.Customer).Include(r => r.Booking)
                .Where(r => venueIds.Contains(r.VenueId) && r.IsApproved);
            if (rating.HasValue && rating.Value >= 1 && rating.Value <= 5) query = query.Where(r => r.Rating == rating.Value);

            var allReviews = await _context.Reviews.Where(r => venueIds.Contains(r.VenueId) && r.IsApproved).ToListAsync();
            var totalCount = await query.CountAsync();

            var reviews = await query.OrderByDescending(r => r.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(r => new ReviewListItemViewModel
                {
                    ReviewId = r.ReviewId,
                    VenueId = r.VenueId,
                    VenueName = r.Venue.VenueName,
                    CustomerId = r.CustomerId,
                    CustomerName = r.Customer.Name,
                    BookingId = r.BookingId,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    IsApproved = r.IsApproved,
                    CreatedAt = r.CreatedAt,
                    ReplyContent = r.Booking.Messages.Where(m => m.SenderId == userId).OrderByDescending(m => m.SentAt).Select(m => m.Content).FirstOrDefault(),
                    RepliedAt = r.Booking.Messages.Where(m => m.SenderId == userId).OrderByDescending(m => m.SentAt).Select(m => (DateTime?)m.SentAt).FirstOrDefault()
                }).ToListAsync();

            var viewModel = new ReviewsDashboardViewModel
            {
                Reviews = reviews,
                AverageRating = Math.Round(allReviews.Any() ? allReviews.Average(r => r.Rating) : 0, 1),
                TotalReviews = allReviews.Count,
                PendingReplyCount = reviews.Count(r => string.IsNullOrEmpty(r.ReplyContent)),
                FilterRating = rating,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            await LoadOwnerStats(userId);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyToReview(ReviewReplyViewModel model)
        {
            if (!ModelState.IsValid) { TempData["Error"] = "Please enter a valid reply."; return RedirectToAction("Reviews"); }
            var userId = GetCurrentUserId();
            var review = await _context.Reviews.Include(r => r.Venue).Include(r => r.Booking)
                .FirstOrDefaultAsync(r => r.ReviewId == model.ReviewId);
            if (review == null || review.Venue.OwnerId != userId) return NotFound();

            _context.Messages.Add(new Message
            {
                BookingId = review.BookingId,
                SenderId = userId,
                ReceiverId = review.CustomerId,
                Content = model.ReplyContent,
                SentAt = DateTime.Now,
                IsRead = false
            });
            _context.Notifications.Add(new Notification
            {
                UserId = review.CustomerId,
                Title = "New Reply to Your Review",
                Message = $"The owner of {review.Venue.VenueName} replied to your review.",
                Type = "Review",
                IsRead = false,
                ActionUrl = $"/Customer/BookingDetails/{review.BookingId}",
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Reply posted successfully!";
            return RedirectToAction("Reviews");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReply(int reviewId, string replyContent)
        {
            var userId = GetCurrentUserId();
            var review = await _context.Reviews.Include(r => r.Venue).Include(r => r.Booking).ThenInclude(b => b.Messages)
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId);
            if (review == null || review.Venue.OwnerId != userId) return NotFound();

            var existingMessage = review.Booking.Messages.Where(m => m.SenderId == userId).OrderByDescending(m => m.SentAt).FirstOrDefault();
            if (existingMessage != null)
            {
                existingMessage.Content = replyContent;
                existingMessage.SentAt = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Reply updated successfully!";
            }
            return RedirectToAction("Reviews");
        }

        // ==================== SETTINGS ====================

        public async Task<IActionResult> Settings()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Login", "Auth");

            var viewModel = new OwnerSettingsViewModel
            {
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
                IsVerified = true
            };

            await LoadOwnerStats(userId);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(OwnerSettingsViewModel model)
        {
            var userId = GetCurrentUserId();
            if (!ModelState.IsValid) { TempData["Error"] = "Please correct the errors."; await LoadOwnerStats(userId); return View("Settings", model); }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (user.Email != model.Email)
            {
                if (await _context.Users.AnyAsync(u => u.Email.ToLower() == model.Email.ToLower() && u.UserId != userId))
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    await LoadOwnerStats(userId);
                    return View("Settings", model);
                }
            }

            user.Name = model.Name; user.Email = model.Email; user.Phone = model.Phone ?? string.Empty;
            await _context.SaveChangesAsync();
            HttpContext.Session.SetString("UserName", user.Name);

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Settings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(OwnerSettingsViewModel model)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(model.CurrentPassword) || string.IsNullOrEmpty(model.NewPassword) || string.IsNullOrEmpty(model.ConfirmPassword))
            { TempData["Error"] = "All password fields are required."; return RedirectToAction("Settings"); }
            if (model.NewPassword != model.ConfirmPassword) { TempData["Error"] = "New passwords do not match."; return RedirectToAction("Settings"); }
            if (model.NewPassword.Length < 6) { TempData["Error"] = "Password must be at least 6 characters."; return RedirectToAction("Settings"); }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();
            if (user.PasswordHash != model.CurrentPassword) { TempData["Error"] = "Current password is incorrect."; return RedirectToAction("Settings"); }

            user.PasswordHash = model.NewPassword;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Password changed successfully!";
            return RedirectToAction("Settings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateAccount()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (await _context.Bookings.AnyAsync(b => b.Venue.OwnerId == userId && b.Status != BookingStatus.Cancelled && b.EndDate >= DateTime.Now))
            {
                TempData["Error"] = "Cannot deactivate account with active bookings.";
                return RedirectToAction("Settings");
            }

            user.IsActive = false;
            await _context.SaveChangesAsync();
            HttpContext.Session.Clear();
            TempData["Success"] = "Your account has been deactivated.";
            return RedirectToAction("Index", "Home");
        }

        // ==================== MESSAGES ====================

        [HttpGet]
        public async Task<IActionResult> Messages()
        {
            var ownerId = GetCurrentUserId();
            var viewModel = await BuildMessagesViewModel(ownerId, null);
            await LoadOwnerStats(ownerId);
            return View("Messages", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Conversation(int customerId)
        {
            var ownerId = GetCurrentUserId();

            var hasConversation = await _context.Messages
                .AnyAsync(m => (m.SenderId == customerId && m.ReceiverId == ownerId) ||
                              (m.SenderId == ownerId && m.ReceiverId == customerId));

            if (!hasConversation)
            {
                var customerExists = await _context.Users.AnyAsync(u => u.UserId == customerId && u.Role == UserRole.Customer);
                if (!customerExists) { TempData["Error"] = "Customer not found."; return RedirectToAction("Messages"); }
            }

            var viewModel = await BuildMessagesViewModel(ownerId, customerId);
            await LoadOwnerStats(ownerId);
            return View("Messages", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send([FromBody] SendMessageRequest request)
        {
            var ownerId = GetCurrentUserId();
            var receiver = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.ReceiverId && u.Role == UserRole.Customer);
            if (receiver == null) return Json(new { success = false, error = "Customer not found." });

            var bookingId = await _context.Bookings
                .Where(b => b.CustomerId == request.ReceiverId && b.Venue.OwnerId == ownerId)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => (int?)b.BookingId)
                .FirstOrDefaultAsync();

            if (bookingId == null || bookingId == 0)
                return Json(new { success = false, error = "No booking found with this customer." });

            var message = new Message
            {
                BookingId = bookingId.Value,
                SenderId = ownerId,
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
                message = new { messageId = message.MessageId, senderId = message.SenderId, content = message.Content, sentAt = message.SentAt.ToString("yyyy-MM-ddTHH:mm:ss"), isFromCurrentUser = true }
            });
        }

        private async Task<OwnerMessagesViewModel> BuildMessagesViewModel(int ownerId, int? selectedCustomerId)
        {
            var conversations = await GetConversationsForOwner(ownerId);
            var viewModel = new OwnerMessagesViewModel { Conversations = conversations, SelectedCustomerId = selectedCustomerId };
            if (selectedCustomerId.HasValue)
                viewModel.SelectedConversation = await GetConversationDetail(ownerId, selectedCustomerId.Value);
            return viewModel;
        }

        private async Task<List<OwnerConversationViewModel>> GetConversationsForOwner(int ownerId)
        {
            var messages = await _context.Messages.AsNoTracking().Include(m => m.Sender).Include(m => m.Receiver)
                .Where(m => m.SenderId == ownerId || m.ReceiverId == ownerId).OrderByDescending(m => m.SentAt).ToListAsync();

            return messages
                .GroupBy(m => m.SenderId == ownerId ? m.ReceiverId : m.SenderId)
                .Select(g =>
                {
                    var last = g.First();
                    var other = last.SenderId == ownerId ? last.Receiver : last.Sender;
                    return new OwnerConversationViewModel
                    {
                        CustomerId = g.Key,
                        CustomerName = other?.Name ?? "Unknown",
                        LastMessage = last.Content,
                        LastMessageTime = last.SentAt,
                        UnreadCount = g.Count(m => m.ReceiverId == ownerId && !m.IsRead),
                        IsCustomerOnline = false,
                        VenueName = GetVenueNameForConversation(g.Key, ownerId),
                        BookingId = last.BookingId
                    };
                })
                .OrderByDescending(c => c.LastMessageTime).ToList();
        }

        private string GetVenueNameForConversation(int customerId, int ownerId)
        {
            return _context.Bookings.AsNoTracking().Include(b => b.Venue)
                .Where(b => b.CustomerId == customerId && b.Venue.OwnerId == ownerId)
                .OrderByDescending(b => b.CreatedAt).FirstOrDefault()?.Venue?.VenueName ?? "General Inquiry";
        }

        private async Task<OwnerConversationDetailViewModel?> GetConversationDetail(int ownerId, int customerId)
        {
            var customer = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == customerId && u.Role == UserRole.Customer);
            if (customer == null) return null;

            var unread = await _context.Messages.Where(m => m.SenderId == customerId && m.ReceiverId == ownerId && !m.IsRead).ToListAsync();
            foreach (var msg in unread) msg.IsRead = true;
            await _context.SaveChangesAsync();

            var messageList = await _context.Messages.AsNoTracking()
                .Where(m => (m.SenderId == ownerId && m.ReceiverId == customerId) || (m.SenderId == customerId && m.ReceiverId == ownerId))
                .OrderBy(m => m.SentAt)
                .Select(m => new VenueBooking.ViewModels.MessageViewModel
                { MessageId = m.MessageId, SenderId = m.SenderId, Content = m.Content, SentAt = m.SentAt, IsFromCurrentUser = m.SenderId == ownerId })
                .ToListAsync();

            var relatedBooking = await _context.Bookings.AsNoTracking().Include(b => b.Venue)
                .Where(b => b.CustomerId == customerId && b.Venue.OwnerId == ownerId)
                .OrderByDescending(b => b.CreatedAt).FirstOrDefaultAsync();

            return new OwnerConversationDetailViewModel
            {
                CustomerId = customerId,
                CustomerName = customer.Name,
                IsCustomerOnline = false,
                Messages = messageList,
                HasBooking = relatedBooking != null,
                VenueName = relatedBooking?.Venue?.VenueName,
                BookingId = relatedBooking?.BookingId,
                EventType = relatedBooking?.EventType,
                EventDate = relatedBooking?.StartDate,
                BookingStatus = relatedBooking?.Status
            };
        }

        // Replace the RecordPayment method in Controllers/OwnerController.cs with this updated implementation.
        // This removes literal '₹' characters in C# strings (which can render as '?') and uses the Unicode codepoint
        // (char)0x20B9 so the rupee symbol displays reliably.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordPayment(int bookingId, decimal amount, string paymentMethod, string? transactionId, string? notes)
        {
            var userId = GetCurrentUserId();

            var booking = await _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.Venue.OwnerId == userId);

            if (booking == null) return NotFound();

            if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Rejected)
            {
                TempData["Error"] = "Cannot record payment for a cancelled or rejected booking.";
                return RedirectToAction("BookingDetails", new { id = bookingId });
            }

            if (amount <= 0)
            {
                TempData["Error"] = "Payment amount must be greater than zero.";
                return RedirectToAction("BookingDetails", new { id = bookingId });
            }

            var totalPaid = booking.Payments
                .Where(p => p.Status == PaymentStatus.Completed)
                .Sum(p => p.Amount);

            var remaining = booking.TotalPrice - totalPaid;

            if (amount > remaining)
            {
                // Use (char)0x20B9 for the rupee symbol (reliable across encodings)
                TempData["Error"] = $"Amount exceeds the remaining balance of {(char)0x20B9}{remaining:N0}.";
                return RedirectToAction("BookingDetails", new { id = bookingId });
            }

            var txnId = string.IsNullOrWhiteSpace(transactionId)
                ? $"OWN-{DateTime.Now:yyyyMMddHHmmss}-{bookingId}"
                : transactionId.Trim();

            var payment = new Payment
            {
                BookingId = bookingId,
                UserId = booking.CustomerId ?? userId,
                Amount = amount,
                PaymentMethod = paymentMethod ?? "Cash",
                TransactionId = txnId,
                Status = PaymentStatus.Completed,
                GatewayResponse = $"Recorded by owner. {(string.IsNullOrWhiteSpace(notes) ? "" : "Notes: " + notes.Trim())}",
                PaidAt = DateTime.Now,
                CreatedAt = DateTime.Now
            };

            _context.Payments.Add(payment);

            var newTotalPaid = totalPaid + amount;
            if (newTotalPaid >= booking.TotalPrice)
                booking.PaymentStatus = PaymentStatus.Completed;
            else
                booking.PaymentStatus = PaymentStatus.Partial;

            booking.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            if (booking.CustomerId.HasValue)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = booking.CustomerId.Value,
                    Title = "Payment Recorded",
                    Message = $"A payment of {(char)0x20B9}{amount:N0} has been recorded for your booking #{bookingId} at {booking.Venue.VenueName} via {paymentMethod}.",
                    Type = "Payment",
                    IsRead = false,
                    ActionUrl = $"/Customer/BookingDetails/{bookingId}",
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"Payment of {(char)0x20B9}{amount:N0} recorded successfully!";
            return RedirectToAction("BookingDetails", new { id = bookingId });
        }
        // Replace the existing BlockDates, AddBlockDate, DeleteBlockedDate and BlockedDatesJson methods
        // with the updated implementations below (inside the OwnerController class).

        // ==================== BLOCKED DATES (OWNER) ====================
        // Unified GET: /Owner/BlockDates and /Owner/BlockDates?venueId=123
        public async Task<IActionResult> BlockDates(int? venueId)
        {
            var ownerId = GetCurrentUserId();

            // Load owner's venues for the selector (always provide to the view)
            var venues = await _context.Venues
                .AsNoTracking()
                .Where(v => v.OwnerId == ownerId)
                .OrderBy(v => v.VenueName)
                .Select(v => new { v.VenueId, v.VenueName })
                .ToListAsync();

            ViewBag.Venues = venues;

            // If no venue selected yet, show selector page (or redirect if owner has exactly one venue)
            if (!venueId.HasValue)
            {
                if (venues.Count == 1)
                {
                    // Owner has exactly one venue: redirect to its page for convenience
                    return RedirectToAction(nameof(BlockDates), new { venueId = venues[0].VenueId });
                }

                var emptyVm = new OwnerBlockDatesViewModel
                {
                    VenueId = 0,
                    VenueName = string.Empty,
                    BlockedDates = new List<OwnerBlockedDateItem>()
                };

                await LoadOwnerStats(ownerId);
                return View(emptyVm);
            }

            // Owner requested a specific venue - ensure owner owns it
            var venue = await _context.Venues
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.VenueId == venueId.Value && v.OwnerId == ownerId);

            if (venue == null)
            {
                TempData["Error"] = "Venue not found or access denied.";
                return RedirectToAction("Venues");
            }

            // Only return blocked dates from today onwards
            var fromDate = DateTime.Today;
            var blocked = await _context.VenueBlockedDates
                .AsNoTracking()
                .Where(b => b.VenueId == venueId.Value && b.BlockDate >= fromDate)
                .OrderByDescending(b => b.BlockDate)
                .Select(b => new OwnerBlockedDateItem
                {
                    Id = b.BlockId,
                    BlockDate = b.BlockDate,
                    Reason = b.Reason ?? string.Empty
                })
                .ToListAsync();

            var vm = new OwnerBlockDatesViewModel
            {
                VenueId = venue.VenueId,
                VenueName = venue.VenueName,
                BlockedDates = blocked
            };

            await LoadOwnerStats(ownerId);
            return View(vm);
        }

        // POST: /Owner/AddBlockDate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBlockDate(int venueId, string startDate, string? endDate, string? reason)
        {
            var ownerId = GetCurrentUserId();

            var venue = await _context.Venues.FirstOrDefaultAsync(v => v.VenueId == venueId && v.OwnerId == ownerId);
            if (venue == null) return NotFound();

            // Validate Start Date
            if (string.IsNullOrWhiteSpace(startDate) || !DateTime.TryParse(startDate, out var start))
            {
                TempData["Error"] = "Please select a valid start date.";
                return RedirectToAction(nameof(BlockDates), new { venueId });
            }

            // Determine End Date (if empty or invalid, block only the start date)
            DateTime end = start;
            if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out var parsedEnd))
            {
                if (parsedEnd.Date >= start.Date)
                {
                    end = parsedEnd;
                }
            }

            // Prevent blocking dates in the past
            if (start.Date < DateTime.Today)
            {
                TempData["Error"] = "Cannot block dates in the past.";
                return RedirectToAction(nameof(BlockDates), new { venueId });
            }

            // Generate a list of all dates in the range
            var allDates = Enumerable.Range(0, (end.Date - start.Date).Days + 1)
                                     .Select(d => start.Date.AddDays(d))
                                     .ToList();

            // Check which of these dates are already blocked in the database
            var existingDates = await _context.VenueBlockedDates
                                              .Where(b => b.VenueId == venueId && allDates.Contains(b.BlockDate.Date))
                                              .Select(b => b.BlockDate.Date)
                                              .ToListAsync();

            // Filter out existing dates so we don't insert duplicates
            var datesToBlock = allDates.Except(existingDates).ToList();

            if (!datesToBlock.Any())
            {
                TempData["Error"] = "The selected date(s) are already blocked.";
                return RedirectToAction(nameof(BlockDates), new { venueId });
            }

            // Add the new dates to the database
            foreach (var date in datesToBlock)
            {
                _context.VenueBlockedDates.Add(new VenueBlockedDate
                {
                    VenueId = venueId,
                    BlockDate = date,
                    Reason = reason?.Trim() ?? "Owner blocked date",
                    CreatedBy = ownerId,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            // Provide a dynamic success message based on whether 1 or multiple dates were blocked
            TempData["Success"] = $"Blocked {(datesToBlock.Count > 1 ? $"{datesToBlock.Count} dates" : datesToBlock.First().ToString("MMM dd, yyyy"))} successfully.";
            return RedirectToAction(nameof(BlockDates), new { venueId });
        }

        // POST: /Owner/DeleteBlockedDate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBlockedDate(int id)
        {
            var ownerId = GetCurrentUserId();

            var blocked = await _context.VenueBlockedDates
                .Include(b => b.Venue)
                .FirstOrDefaultAsync(b => b.BlockId == id && b.Venue.OwnerId == ownerId);

            if (blocked == null) return NotFound();

            var venueId = blocked.VenueId;
            _context.VenueBlockedDates.Remove(blocked);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Unblocked {blocked.BlockDate:MMM dd, yyyy}.";
            return RedirectToAction(nameof(BlockDates), new { venueId });
        }

        // JSON: /Owner/BlockedDatesJson?venueId=123
        // Returns { id, date, reason } entries. Only future (>= today) dates are returned to match UI.
        [HttpGet]
        public async Task<IActionResult> BlockedDatesJson(int venueId)
        {
            var ownerId = GetCurrentUserId();

            var venue = await _context.Venues.AsNoTracking().FirstOrDefaultAsync(v => v.VenueId == venueId);
            if (venue == null) return NotFound();

            var fromDate = DateTime.Today;
            var list = await _context.VenueBlockedDates.AsNoTracking()
                .Where(b => b.VenueId == venueId && b.BlockDate >= fromDate)
                .OrderBy(b => b.BlockDate)
                .Select(b => new { id = b.BlockId, date = b.BlockDate.ToString("yyyy-MM-dd"), reason = b.Reason })
                .ToListAsync();

            return Json(list);
        }
    }

    public class SendMessageRequest
    {
        public int ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}