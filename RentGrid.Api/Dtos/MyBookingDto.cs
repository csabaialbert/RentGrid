namespace RentGrid.Api.Dtos
{
    public class MyBookingDto
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public string VehicleBrand { get; set; } = string.Empty;
        public string VehicleModel { get; set; } = string.Empty;
        public decimal VehicleDailyPrice { get; set; }
        public string? VehicleImageFileId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<BookingExtraDto> Extras { get; set; } = new();
    }
}
