using Microsoft.EntityFrameworkCore;
using VenueBooking.Models;

namespace VenueBooking.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ===== TABLES =====

        public DbSet<User> Users { get; set; }
        public DbSet<Venue> Venues { get; set; }
        public DbSet<VenueImage> VenueImages { get; set; }
        public DbSet<VenueAmenity> VenueAmenities { get; set; }
        public DbSet<Amenity> Amenities { get; set; }
        public DbSet<VenueBlockedDate> VenueBlockedDates { get; set; }

        public DbSet<Theme> Themes { get; set; }
        public DbSet<ThemeImage> ThemeImages { get; set; }
        public DbSet<ThemePricing> ThemePricings { get; set; }

        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<OwnerSubscription> OwnerSubscriptions { get; set; }

        public DbSet<SeasonalPricing> SeasonalPricings { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        // ===== MODEL CONFIG =====

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ✅ GLOBAL FIX — PREVENT CASCADE DELETE CONFLICT
            foreach (var relationship in modelBuilder.Model
                .GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            // UNIQUE EMAIL
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // UNIQUE SLUG
            modelBuilder.Entity<Venue>()
                .HasIndex(v => v.Slug)
                .IsUnique();

            // ONE REVIEW PER BOOKING
            modelBuilder.Entity<Review>()
                .HasIndex(r => r.BookingId)
                .IsUnique();

            // VENUE BLOCK UNIQUE DATE
            modelBuilder.Entity<VenueBlockedDate>()
                .HasIndex(b => new { b.VenueId, b.BlockDate })
                .IsUnique();

            // RELATION: Message Sender
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // RELATION: Message Receiver
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // DECIMAL PRECISION SAFETY
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(12, 2);

            modelBuilder.Entity<Booking>()
                .Property(b => b.TotalPrice)
                .HasPrecision(12, 2);
        }
    }
}