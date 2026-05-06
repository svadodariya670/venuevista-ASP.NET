# VenueBooking

VenueBooking is an ASP.NET Core MVC web application for discovering, managing, and booking event venues. It supports three roles: Admin, Owner, and Customer.

## Tech Stack

- ASP.NET Core MVC (.NET 9)
- Entity Framework Core 9
- SQL Server / LocalDB
- Cookie authentication and session state
- Razorpay payment integration

## Main Features

- Customer venue search and browsing with filters for city, venue type, guest count, and date
- Venue details pages with themes, pricing, images, amenities, reviews, and blocked date handling
- Customer registration, login, booking history, booking details, notifications, and messages
- Owner dashboard for venue management, bookings, revenue, reviews, and subscription limits
- Admin dashboard for users, venues, bookings, revenue, approvals, messages, and audit activity
- Razorpay order creation and payment signature verification
- Contact page, FAQ, About, Privacy, and error pages

## Project Structure

- Controllers/ - MVC controllers for Admin, Auth, Customer, Home, and Owner workflows
- Data/ - EF Core DbContext and model configuration
- Models/ - domain entities and enums
- ViewModels/ - page-specific view models
- Services/ - application services such as RazorpayService
- Views/ - Razor views and shared layouts
- wwwroot/ - static files, styles, scripts, and uploads

## Local Configuration

The app loads `appsettings.Local.json` automatically on startup. Keep secrets there so they do not get committed.

Example structure:

```json
{
  "Razorpay": {
    "KeyId": "your_key_id",
    "KeySecret": "your_key_secret"
  }
}
```

The default SQL Server connection string is in `appsettings.json` and targets `VenueBookingDB` on LocalDB.

## Getting Started

1. Restore packages

```powershell
dotnet restore
```

2. Apply database migrations

```powershell
dotnet ef database update
```

3. Run the application

```powershell
dotnet run
```

If you prefer Visual Studio, you can also run the app with F5 after the database is configured.

## Notes

- `appsettings.Local.json` is ignored by Git so Razorpay keys stay local.
- Build output folders such as `bin/` and `obj/` are also ignored.
- Authentication redirects unauthenticated users to `/Auth/Login`.
