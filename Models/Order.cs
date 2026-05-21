using tickets_management.Enums;

namespace tickets_management.Models;


//Models para relacionar las bases de datos
public class Order : BaseEntity
{
    public string Nit { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public TimeOnly HourAt { get; set; }
    public TypePayment PaymentMethod { get; set; }
    public int CustomerId { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}