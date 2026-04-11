using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RentGrid.Api.Models
{
    public class ExtraService
    {
        public int Id { get; set; }
        public required string Name { get; set; } // pl. GPS, Biztosítás
        public decimal Price { get; set; }
        // Navigáció a kapcsolótáblához
        public List<BookingExtra> BookingExtras { get; set; } = new();
    }
}