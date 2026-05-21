using tickets_management.Enums;

namespace tickets_management.Dto;

public class CreateOrderDto
{
    public string Nit { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public TypePayment PaymentMethod { get; set; }
    public TimeOnly HourAt { get; set; }
    public List<CreateOrderItemDto> Items { get; set; } = new();
}
