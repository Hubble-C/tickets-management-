namespace tickets_management.Models;


//Models para relacionar las bases de datos
public class OrderItem : BaseEntity
{
    public int Quantity { get; set; }
    public decimal PriceTicket { get; set; }
    public decimal Total { get; set; }
    public Order Order { get; set; } = null!;
    public int OrderId { get; set; }
    public int EventId { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}