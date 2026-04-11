namespace RentGrid.Api.Dtos
{
    public class BookingExtraDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public decimal Price { get; set; }
    }
}
