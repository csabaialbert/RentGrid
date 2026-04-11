namespace RentGrid.Api.Dtos
{
    public class DashboardStatsDto
    {
        public decimal TotalRevenue { get; set; }
        public int RegisteredUserCount { get; set; }
        public int ActiveBookingCount { get; set; }
        public PopularVehicleDto? MostPopularVehicle { get; set; }
    }

    public class PopularVehicleDto
    {
        public int VehicleId { get; set; }
        public required string Brand { get; set; }
        public required string Model { get; set; }
        public int BookingCount { get; set; }
    }
}
