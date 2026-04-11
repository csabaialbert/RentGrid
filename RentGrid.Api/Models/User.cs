using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RentGrid.Api.Models
{
    public class User
    {
        public int Id { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public string Role { get; set; } = "Customer"; // Admin vagy customer
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Nav: Egy felhasználónak sok foglalása lehet
        public List<Booking> Bookings { get; set; } = new();
    }
}