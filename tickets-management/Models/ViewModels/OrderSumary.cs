using tickets_management.Enums;

namespace tickets_management.Models.ViewModels;

public class OrderSumary
{
    public decimal Total { get; set; }
    public decimal Tax { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public TypePayment TypePayment { get; set; }

    public List<CartSummary> CartSummaries { get; set; } = new();
}