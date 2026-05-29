namespace tickets_management.Models;

public class TicketType
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int EventId { get; set; }
}