namespace tickets_management.Dto;

public class CreateOrderItemDto
{
    /// <summary>The catalog ticket type being sold; its price and event are read from the catalog.</summary>
    public int TicketTypeId { get; set; }

    /// <summary>One ticket is minted per seat, so this also drives the quantity.</summary>
    public List<string> Seats { get; set; } = new();
}
