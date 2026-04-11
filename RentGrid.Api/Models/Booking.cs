using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RentGrid.Api.Models
{
    public class Booking
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int VehicleId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Cancelled

        // Navigációs tulajdonságok
        public User? User { get; set; }
        public Vehicle? Vehicle { get; set; }

        // Kapcsolat az extra szolgáltatásokhoz
        public List<BookingExtra> BookingExtras { get; set; } = new();
    }
}