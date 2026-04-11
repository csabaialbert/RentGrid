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

            // MongoDB GridFS referencia
            entity.Property(v => v.MongoImageId)
                  .IsRequired(false);
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
        });
    }
}