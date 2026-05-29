namespace tickets_management.Models;

public class TempOrder : BaseEntity
{
    public string OrderId { get; set; }
    public string EventId { get; set; }
    public List<Seat> SelectedSeats { get; set; } = new();
    public decimal ServiceRate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
}