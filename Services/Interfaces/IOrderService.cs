using tickets_management.Models;
using tickets_management.Response;

namespace tickets_management.Services.Interfaces;

public interface IOrderService
{
    /// <summary>Payment completed: Pending -> Paid, activating the order's tickets.</summary>
    Task<ServiceResponse<Order>> MarkAsPaidAsync(int orderId, CancellationToken ct = default);

    /// <summary>Cancellation/refund: -> Cancelled, deactivating non-consumed tickets.</summary>
    Task<ServiceResponse<Order>> CancelAsync(int orderId, CancellationToken ct = default);

    /// <summary>Payment rejected: Pending -> Rejected, voiding the order's tickets.</summary>
    Task<ServiceResponse<Order>> RejectAsync(int orderId, CancellationToken ct = default);
}
