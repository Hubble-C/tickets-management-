using tickets_management.Enums;

namespace tickets_management.Models;

//Models para relacionar las bases de datos
public class Ticket : BaseEntity
{
    public string TicketCode { get; set; } = string.Empty;
    public string Seat { get; set; } = string.Empty;
    public int EventId { get; set; }
    public TicketStatus Status { get; set; }
    public OrderItem OrderItem { get; set; } = null!;
    public int OrderItemId { get; set; }
}