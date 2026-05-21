using tickets_management.Models;

namespace tickets_management.ViewModels;

public class OrderConfirmationViewModel
{
    public Order Order { get; init; } = null!;
    public IReadOnlyList<TicketQrViewModel> Tickets { get; init; } = [];

    public decimal Total => Order.Items.Sum(i => i.Total);
}
