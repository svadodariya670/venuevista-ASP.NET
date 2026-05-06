using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VenueBooking.Data;
using VenueBooking.Models.Enums;
using VenueBooking.ViewModels;

namespace VenueBooking.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult FAQ()
        {
            
            return View();
        }

        public async Task<IActionResult> Index()
        {
            var featuredVenues = await _context.Venues
                .AsNoTracking()
                .Include(v => v.Images)
                .Include(v => v.Reviews)
                .Where(v => v.IsActive && v.IsVerified)
                .Where(v => v.Themes.Any(t => t.IsActive && t.Pricings.Any(p => p.IsActive)))
                .OrderByDescending(v => v.Reviews.Where(r => r.IsApproved).Average(r => (double?)r.Rating) ?? 0)
                .Take(6)
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

            var stats = new HomeStatsViewModel
            {
                TotalVenues = await _context.Venues.CountAsync(v => v.IsActive && v.IsVerified
                    && v.Themes.Any(t => t.IsActive && t.Pricings.Any(p => p.IsActive))),
                TotalBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Confirmed),
                TotalCustomers = await _context.Users.CountAsync(u => u.Role == UserRole.Customer),
                TotalCities = await _context.Venues
                    .Where(v => v.IsActive && v.IsVerified)
                    .Where(v => v.Themes.Any(t => t.IsActive && t.Pricings.Any(p => p.IsActive)))
                    .Select(v => v.City)
                    .Distinct()
                    .CountAsync()
            };

            var viewModel = new HomePageViewModel
            {
                FeaturedVenues = featuredVenues,
                Stats = stats,
                VenueTypes = await _context.Venues
                    .Where(v => v.IsActive && v.IsVerified)
                    .Where(v => v.Themes.Any(t => t.IsActive && t.Pricings.Any(p => p.IsActive)))
                    .GroupBy(v => v.VenueType)
                    .Select(g => new VenueTypeSummary
                    {
                        VenueType = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // GET: /Home/Search?city=Ahmedabad&type=MarriageHall&guests=200
        public async Task<IActionResult> Search(string city, string type, int? guests, string date, int page = 1)
        {
            const int pageSize = 12;

            var query = _context.Venues
                .AsNoTracking()
                .Include(v => v.Images)
                .Include(v => v.Reviews)
                .Include(v => v.Themes).ThenInclude(t => t.Pricings)
                .Where(v => v.IsActive && v.IsVerified)
                .Where(v => v.Themes.Any(t => t.IsActive && t.Pricings.Any(p => p.IsActive)));

            if (!string.IsNullOrWhiteSpace(city))
                query = query.Where(v => v.City.Contains(city) || v.State.Contains(city));

            if (!string.IsNullOrWhiteSpace(type) && type != "All")
                query = query.Where(v => v.VenueType == type);

            if (guests.HasValue && guests.Value > 0)
                query = query.Where(v => v.Capacity >= guests.Value);

            if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var eventDate))
            {
                var blockedVenueIds = await _context.VenueBlockedDates
                    .Where(b => b.BlockDate.Date == eventDate.Date)
                    .Select(b => b.VenueId)
                    .ToListAsync();

                query = query.Where(v => !blockedVenueIds.Contains(v.VenueId));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var venues = await query
                .OrderByDescending(v => v.Reviews.Where(r => r.IsApproved).Average(r => (double?)r.Rating) ?? 0)
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


            var viewModel = new VenueSearchViewModel
            {
                Venues = venues,
                City = city,
                VenueType = type,
                Guests = guests,
                Date = date,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalResults = totalCount,
                AvailableTypes = await _context.Venues
                    .Where(v => v.IsActive && v.IsVerified)
                    .Where(v => v.Themes.Any(t => t.IsActive && t.Pricings.Any(p => p.IsActive)))
                    .Select(v => v.VenueType)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToListAsync(),
                AvailableCities = await _context.Venues
                    .Where(v => v.IsActive && v.IsVerified)
                    .Select(v => v.City)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // GET: /Home/VenueDetails/5
        [Authorize] // <--- Added: Forces login. Guests are automatically redirected to /Auth/Login.
        public IActionResult VenueDetails(int id)
        {
            // First check if the user is a customer to use the right layout
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            // If they are a registered Customer (or general user), immediately redirect to Customer/VenueDetails
            // which has the proper Booking form layout and required data structures
            if (role == UserRole.Customer)
            {
                return RedirectToAction("VenueDetails", "Customer", new { id = id });
            }

            // Fallback for Admin/Owner trying to view public detail
            return RedirectToAction("Index");
        }

        public IActionResult About() => View();
        
        // GET: /Home/Contact
        public IActionResult Contact() => View();

        // POST: /Home/Contact -> Handles Contact Page Form Submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Contact(string name, string email, string subject, string message)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Please fill in all required fields.";
                return View();
            }

            // In a real application, you would integrate an email service like SendGrid, MailKit, 
            // or SMTP here to actually send the email to hello@venuevista.com.
            // Example structure:
            // _emailService.SendEmailAsync("hello@venuevista.com", subject, $"From: {name} ({email})\n\n{message}");

            // For now, we simulate success and notify the user.
            TempData["Success"] = "Thank you for reaching out! Your message has been sent successfully. We will get back to you within 24 hours.";
            
            return RedirectToAction(nameof(Contact));
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View();
    }
}