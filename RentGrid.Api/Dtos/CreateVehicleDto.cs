namespace RentGrid.Api.Dtos
{
    public class CreateVehicleDto
    {
        public required string Brand { get; set; }
        public required string Model { get; set; }
        public decimal DailyPrice { get; set; }
    }
}
