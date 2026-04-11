namespace RentGrid.Api.Dtos
{
    public class VehicleDto
    {
        public int Id { get; set; }
        public required string Brand { get; set; }
        public required string Model { get; set; }
        public decimal DailyPrice { get; set; }
        public bool IsAvailable { get; set; }
        public string? ImageFileId { get; set; }
    }
}
