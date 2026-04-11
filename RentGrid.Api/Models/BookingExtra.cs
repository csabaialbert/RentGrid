using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RentGrid.Api.Models
{
    public class BookingExtra
    {
        public int BookingId { get; set; }
        public Booking? Booking { get; set; }

        public int ExtraServiceId { get; set; }
        public ExtraService? ExtraService { get; set; }
    }
}