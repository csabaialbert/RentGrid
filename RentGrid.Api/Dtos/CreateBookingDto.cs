using System.Collections.Generic;

namespace RentGrid.Api.Dtos
{
    public class CreateBookingDto
    {
        public int VehicleId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<int> ExtraServiceIds { get; set; } = new();
    }
}
