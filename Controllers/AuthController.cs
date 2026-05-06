using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VenueBooking.Data;
using VenueBooking.Models;
using VenueBooking.Models.Enums;

namespace VenueBooking.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }
        // ==================== LOGIN ====================

        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in, redirect to home
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Email and password are required");
                return View();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user == null || !user.IsActive || user.PasswordHash != password)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View();
            }

            // Update last login
            user.LastLoginAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // Create authentication cookie
            var claims = new List<System.Security.Claims.Claim>
    {
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Name),
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email),
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role),
        new System.Security.Claims.Claim("UserId", user.UserId.ToString())
    };

            var claimsIdentity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new System.Security.Claims.ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Keep session for backward compatibility
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserEmail", user.Email);

            return user.Role switch
            {
                UserRole.Admin => RedirectToAction("Dashboard", "Admin"),
                UserRole.Owner => RedirectToAction("Dashboard", "Owner"),
                _ => RedirectToAction("Dashboard", "Customer")
            };
        }

        // ==================== CUSTOMER REGISTRATION ====================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string Name, string Email, string Phone, string PasswordHash, string Password, string AcceptTerms)
        {
            // Validation
            if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(PasswordHash))
            {
                ModelState.AddModelError("", "All fields are required");
                return View();
            }

            if (PasswordHash != Password)
            {
                ModelState.AddModelError("", "Passwords do not match");
                return View();
            }

            if (AcceptTerms != "true")
            {
                ModelState.AddModelError("", "You must accept the terms");
                return View();
            }

            // Check email exists
            var existing = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);
            if (existing != null)
            {
                ModelState.AddModelError("", "Email already registered");
                return View();
            }

            // Create user
            var user = new User
            {
                Name = Name,
                Email = Email,
                Phone = Phone,
                PasswordHash = PasswordHash,
                Role = UserRole.Customer,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create authentication cookie 
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Name),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role),
                new System.Security.Claims.Claim("UserId", user.UserId.ToString())
            };

            var claimsIdentity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new System.Security.Claims.ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2) });

            // Keep session for backward compatibility
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserEmail", user.Email);

            return RedirectToAction("Dashboard", "Customer");
        }

        // ==================== OWNER REGISTRATION ====================
        [HttpGet]
        public IActionResult JoinNow()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> JoinNow(string Name, string Email, string Phone, string PasswordHash, string Password, string AcceptTerms)
        {
            // Validation
            if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(PasswordHash))
            {
                ModelState.AddModelError("", "All fields are required");
                return View();
            }

            // Check password match
            if (PasswordHash != Password)
            {
                ModelState.AddModelError("", "Passwords do not match");
                return View();
            }

            // Check terms
            if (AcceptTerms != "true")
            {
                ModelState.AddModelError("", "You must accept the terms");
                return View();
            }

            // Check if email exists
            var existing = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email);
            if (existing != null)
            {
                ModelState.AddModelError("", "Email already registered");
                return View();
            }

            // Create owner
            var user = new User
            {
                Name = Name,
                Email = Email,
                Phone = Phone,
                PasswordHash = PasswordHash,  // Plain text (follow-up: hash this)
                Role = UserRole.Owner,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create authentication cookie (sign in)
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Name),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role),
                new System.Security.Claims.Claim("UserId", user.UserId.ToString())
            };

            var claimsIdentity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new System.Security.Claims.ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Keep session for backward compatibility
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserEmail", user.Email);

            // Redirect to owner dashboard
            return RedirectToAction("Dashboard", "Owner");
        }

        // ==================== FORGOT PASSWORD ====================
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            // TODO: Send password reset email
            return View("ForgotPasswordConfirmation");
        }

        // ==================== LOGOUT ====================
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Sign out of cookie authentication
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Clear all session data
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtp(string otp)
        {
            // TODO: Verify OTP logic
            return RedirectToAction("ResetPassword");
        }

        [HttpGet]
        public IActionResult OtpVerified()
        {
            return View();
        }
    }
}