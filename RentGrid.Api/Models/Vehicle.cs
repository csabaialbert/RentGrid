using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RentGrid.Api.Models
{
    public class Vehicle
    {
        public int Id { get; set; }
        public required string Brand { get; set; }
        public required string Model { get; set; }
        public decimal DailyPrice { get; set; }
        public bool IsAvailable { get; set; } = true;
        
        // Ez mutat a MongoDB GridFS fájlra
        public string? MongoImageId { get; set; }

        public List<Booking> Bookings { get; set; } = new();
    }
}