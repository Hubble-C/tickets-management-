namespace tickets_management.Models;

public class OrderItem : BaseEntity
{
    public int Quantity { get; set; }
    public decimal PriceTicket { get; set; }
    public decimal Total { get; set; }
    public Order Order { get; set; } = null!;
    public int OrderId { get; set; }
    public int EventId { get; set; }

    /// <summary>The catalog ticket type sold on this line; drives the unit price.</summary>
    public int TicketTypeId { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}