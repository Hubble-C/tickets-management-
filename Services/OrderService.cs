using Microsoft.EntityFrameworkCore;
using tickets_management.Data;
using tickets_management.Enums;
using tickets_management.Models;
using tickets_management.Response;
using tickets_management.Services.Interfaces;
using tickets_management.Services.StateMachines;

namespace tickets_management.Services;

public class OrderService : IOrderService
{
    private readonly MySqlDbContext _db;
    private readonly TimeProvider _clock;

    public OrderService(MySqlDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public Task<ServiceResponse<Order>> MarkAsPaidAsync(int orderId, CancellationToken ct = default) =>
        TransitionAsync(orderId, OrderStatus.Paid, ct);

    public Task<ServiceResponse<Order>> CancelAsync(int orderId, CancellationToken ct = default) =>
        TransitionAsync(orderId, OrderStatus.Cancelled, ct);

    public Task<ServiceResponse<Order>> RejectAsync(int orderId, CancellationToken ct = default) =>
        TransitionAsync(orderId, OrderStatus.Rejected, ct);

    /// <summary>
    /// Moves the order to <paramref name="target"/> and cascades the matching
    /// ticket transition, all inside a single database transaction so order and
    /// tickets can never end up in inconsistent states.
    /// </summary>
    private async Task<ServiceResponse<Order>> TransitionAsync(
        int orderId, OrderStatus target, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Tickets)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
            return ServiceResponse<Order>.Fail($"Order {orderId} was not found.");

        if (order.Status == target)
            return ServiceResponse<Order>.Ok(order, $"Order is already {target}.");

        if (!OrderStateMachine.CanTransition(order.Status, target))
            return ServiceResponse<Order>.Fail(
                $"Illegal order transition: {order.Status} -> {target}.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var now = _clock.GetUtcNow().UtcDateTime;
            order.Status = target;
            order.UpdatedAt = now;
            CascadeToTickets(order, target, now);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return ServiceResponse<Order>.Ok(order);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            return ServiceResponse<Order>.Fail($"Could not update order {orderId}: {ex.Message}");
        }
    }

    private static void CascadeToTickets(Order order, OrderStatus target, DateTime now)
    {
        foreach (var ticket in order.Items.SelectMany(i => i.Tickets))
        {
            var next = target switch
            {
                // Payment activates pending tickets.
                OrderStatus.Paid when ticket.Status == TicketStatus.Pending
                    => TicketStatus.Available,
                // Cancelling/rejecting voids tickets that have not been consumed.
                (OrderStatus.Cancelled or OrderStatus.Rejected)
                    when ticket.Status is TicketStatus.Pending or TicketStatus.Available
                    => TicketStatus.Cancelled,
                _ => ticket.Status
            };

            if (next != ticket.Status && TicketStateMachine.CanTransition(ticket.Status, next))
            {
                ticket.Status = next;
                ticket.UpdatedAt = now;
            }
        }
    }
}
