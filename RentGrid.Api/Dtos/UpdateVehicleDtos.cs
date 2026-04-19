namespace RentGrid.Api.Dtos
{
    public class UpdateVehiclePriceDto
    {
        public decimal DailyPrice { get; set; }
    }

    public class UpdateVehicleAvailabilityDto
    {
        public bool IsAvailable { get; set; }
    }
}