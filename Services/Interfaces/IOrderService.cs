using tickets_management.Dto;
using tickets_management.Models;
using tickets_management.Response;

namespace tickets_management.Services.Interfaces;

public interface IOrderService
{
    /// <summary>
    /// Creates a Pending order with its items and one Pending ticket per seat,
    /// minting a unique TicketCode for each (collision-safe against the DB index).
    /// </summary>
    Task<ServiceResponse<Order>> CreateOrderAsync(CreateOrderDto dto, CancellationToken ct = default);

    /// <summary>Payment completed: Pending -> Paid, activating the order's tickets.</summary>
    Task<ServiceResponse<Order>> MarkAsPaidAsync(int orderId, CancellationToken ct = default);

    /// <summary>Cancellation/refund: -> Cancelled, deactivating non-consumed tickets.</summary>
    Task<ServiceResponse<Order>> CancelAsync(int orderId, CancellationToken ct = default);

    /// <summary>Payment rejected: Pending -> Rejected, voiding the order's tickets.</summary>
    Task<ServiceResponse<Order>> RejectAsync(int orderId, CancellationToken ct = default);
}
