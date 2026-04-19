using System;
using Microsoft.EntityFrameworkCore;
using RentGrid.Api.Models;

namespace RentGrid.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<ExtraService> ExtraServices => Set<ExtraService>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingExtra> BookingExtras => Set<BookingExtra>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- VEHICLE KONFIGURÁCIÓ ---
        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(v => v.Id);
            
            entity.Property(v => v.Brand).IsRequired();
            entity.Property(v => v.Model).IsRequired();
            
            // Decimal konfiguráció a DailyPrice-hoz
            entity.Property(v => v.DailyPrice)
                  .HasPrecision(18, 2);

            // MongoDB GridFS referenciák (JSON-ként tárolva)
            var property = entity.Property(v => v.MongoImageIds)
                  .HasConversion(
                      v => string.Join(',', v),
                      v => string.IsNullOrEmpty(v) ? new List<string>() : v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                  )
                  .IsRequired(false);
            property.Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c == null ? new List<string>() : new List<string>(c)));

            entity.HasData(
                new Vehicle { Id = 1, Brand = "Toyota", Model = "Corolla", DailyPrice = 17500m, IsAvailable = true, MongoImageIds = new List<string>() },
                new Vehicle { Id = 2, Brand = "Ford", Model = "Focus", DailyPrice = 16500m, IsAvailable = true, MongoImageIds = new List<string>() },
                new Vehicle { Id = 3, Brand = "BMW", Model = "320i", DailyPrice = 32000m, IsAvailable = true, MongoImageIds = new List<string>() },
                new Vehicle { Id = 4, Brand = "Škoda", Model = "Octavia", DailyPrice = 18500m, IsAvailable = true, MongoImageIds = new List<string>() }
            );
        });

        // --- BOOKING KONFIGURÁCIÓ ---
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(b => b.Id);
            
            entity.Property(b => b.TotalPrice)
                  .HasPrecision(18, 2);

            // Kapcsolat a User-rel (1:N)
            entity.HasOne(b => b.User)
                  .WithMany(u => u.Bookings)
                  .HasForeignKey(b => b.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Kapcsolat a Vehicle-lel (1:N)
            entity.HasOne(b => b.Vehicle)
                  .WithMany(v => v.Bookings) // Most már mindkét oldalon ott a navigáció
                  .HasForeignKey(b => b.VehicleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // --- BOOKINGEXTRA (MANY-TO-MANY) KONFIGURÁCIÓ ---
        modelBuilder.Entity<BookingExtra>(entity =>
        {
            // Kompozit elsődleges kulcs
            entity.HasKey(be => new { be.BookingId, be.ExtraServiceId });

            // Kapcsolat a Booking felé
            entity.HasOne(be => be.Booking)
                  .WithMany(b => b.BookingExtras)
                  .HasForeignKey(be => be.BookingId);

            // Kapcsolat az ExtraService felé
            entity.HasOne(be => be.ExtraService)
                  .WithMany(es => es.BookingExtras)
                  .HasForeignKey(be => be.ExtraServiceId);
        });

        // --- EXTRA SERVICE KONFIGURÁCIÓ ---
        modelBuilder.Entity<ExtraService>(entity =>
        {
            entity.Property(es => es.Price)
                  .HasPrecision(18, 2);

            entity.HasData(
                new ExtraService { Id = 1, Name = "GPS", Price = 2000m },
                new ExtraService { Id = 2, Name = "Gyerekülés", Price = 1500m },
                new ExtraService { Id = 3, Name = "Extra biztosítás", Price = 5000m }
            );
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.FullName).IsRequired();
            entity.Property(u => u.Email).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).IsRequired();

            entity.HasData(
                new User
                {
                    Id = 1,
                    FullName = "Admin User",
                    Email = "admin@rentgrid.local",
                    PasswordHash = "$2a$12$OMuczbfgm9D4Ix.5EhxeKeDmqYFbW464rY2k9TT2yHoYeMiy8K/ja",
                    Role = "Admin",
                    CreatedAt = new DateTime(2026, 4, 11, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        });
    }
}