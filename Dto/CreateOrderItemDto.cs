namespace tickets_management.Dto;

public class CreateOrderItemDto
{
    public int EventId { get; set; }
    public decimal PriceTicket { get; set; }

    /// <summary>One ticket is minted per seat, so this also drives the quantity.</summary>
    public List<string> Seats { get; set; } = new();
}
