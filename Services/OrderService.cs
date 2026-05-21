using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using tickets_management.Data;
using tickets_management.Dto;
using tickets_management.Enums;
using tickets_management.Models;
using tickets_management.Response;
using tickets_management.Services.Interfaces;
using tickets_management.Services.StateMachines;

namespace tickets_management.Services;

public class OrderService : IOrderService
{
    private const int MaxCodeAttempts = 5;

    private readonly MySqlDbContext _db;
    private readonly TimeProvider _clock;
    private readonly ITicketCodeGenerator _codeGenerator;

    public OrderService(MySqlDbContext db, TimeProvider clock, ITicketCodeGenerator codeGenerator)
    {
        _db = db;
        _clock = clock;
        _codeGenerator = codeGenerator;
    }

    public async Task<ServiceResponse<Order>> CreateOrderAsync(
        CreateOrderDto dto, CancellationToken ct = default)
    {
        if (dto.Items.Count == 0)
            return ServiceResponse<Order>.Fail("An order must contain at least one item.");
        if (dto.Items.Any(i => i.Seats.Count == 0))
            return ServiceResponse<Order>.Fail("Every item must include at least one seat.");

        var now = _clock.GetUtcNow().UtcDateTime;
        var order = new Order
        {
            Nit = dto.Nit,
            CustomerId = dto.CustomerId,
            PaymentMethod = dto.PaymentMethod,
            HourAt = dto.HourAt,
            Status = OrderStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
        };

        foreach (var itemDto in dto.Items)
        {
            var item = new OrderItem
            {
                EventId = itemDto.EventId,
                PriceTicket = itemDto.PriceTicket,
                Quantity = itemDto.Seats.Count,
                Total = itemDto.PriceTicket * itemDto.Seats.Count,
                CreatedAt = now,
                UpdatedAt = now,
            };
            foreach (var seat in itemDto.Seats)
            {
                item.Tickets.Add(new Ticket
                {
                    Seat = seat,
                    EventId = itemDto.EventId,
                    Status = TicketStatus.Pending,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
            order.Items.Add(item);
        }

        _db.Orders.Add(order);

        // The DB unique index is the source of truth for uniqueness; on the rare
        // collision we regenerate every code and retry, giving up after a cap so a
        // pathological case fails cleanly instead of throwing or looping forever.
        for (var attempt = 1; ; attempt++)
        {
            AssignTicketCodes(order);
            try
            {
                await _db.SaveChangesAsync(ct);
                return ServiceResponse<Order>.Ok(order);
            }
            catch (DbUpdateException ex) when (IsDuplicateTicketCode(ex))
            {
                if (attempt >= MaxCodeAttempts)
                    return ServiceResponse<Order>.Fail(
                        $"Could not generate a unique ticket code after {MaxCodeAttempts} attempts.");
            }
            catch (Exception ex) when (ex is DbException or DbUpdateException)
            {
                return ServiceResponse<Order>.Fail($"Could not save the order: {ex.Message}");
            }
        }
    }

    public Task<Order?> GetByIdAsync(int orderId, CancellationToken ct = default) =>
        _db.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Tickets)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

    public async Task<SalesReportDto> GetSalesReportAsync(CancellationToken ct = default)
    {
        var ordersByStatus = await _db.Orders
            .GroupBy(o => o.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        // Revenue and "sold" only count paid orders; a pending/cancelled order
        // has not produced money.
        var paidItems = _db.OrderItems.Where(i => i.Order.Status == OrderStatus.Paid);

        var grossRevenue = await paidItems.SumAsync(i => (decimal?)i.Total, ct) ?? 0m;
        var ticketsSold = await _db.Tickets
            .CountAsync(t => t.OrderItem.Order.Status == OrderStatus.Paid, ct);
        var ticketsUsed = await _db.Tickets.CountAsync(t => t.Status == TicketStatus.Used, ct);

        var byEvent = await paidItems
            .GroupBy(i => i.EventId)
            .Select(g => new EventSalesDto
            {
                EventId = g.Key,
                TicketsSold = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.Total),
            })
            .OrderByDescending(e => e.Revenue)
            .ToListAsync(ct);

        return new SalesReportDto
        {
            TotalOrders = ordersByStatus.Values.Sum(),
            PaidOrders = ordersByStatus.GetValueOrDefault(OrderStatus.Paid),
            GrossRevenue = grossRevenue,
            TicketsSold = ticketsSold,
            TicketsUsed = ticketsUsed,
            OrdersByStatus = ordersByStatus,
            ByEvent = byEvent,
        };
    }

    private void AssignTicketCodes(Order order)
    {
        foreach (var ticket in order.Items.SelectMany(i => i.Tickets))
            ticket.TicketCode = _codeGenerator.Generate();
    }

    private static bool IsDuplicateTicketCode(DbUpdateException ex) =>
        ex.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry };

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
        Order? order;
        try
        {
            order = await _db.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Tickets)
                .FirstOrDefaultAsync(o => o.Id == orderId, ct);
        }
        catch (DbException ex)
        {
            return ServiceResponse<Order>.Fail($"Could not reach the database: {ex.Message}");
        }

        if (order is null)
            return ServiceResponse<Order>.Fail($"Order {orderId} was not found.");

        if (order.Status == target)
            return ServiceResponse<Order>.Ok(order, $"Order is already {target}.");

        if (!OrderStateMachine.CanTransition(order.Status, target))
            return ServiceResponse<Order>.Fail(
                $"Illegal order transition: {order.Status} -> {target}.");

        try
        {
            // Disposing an uncommitted transaction rolls it back, so order and
            // tickets are never left half-updated on failure.
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            var now = _clock.GetUtcNow().UtcDateTime;
            order.Status = target;
            order.UpdatedAt = now;
            CascadeToTickets(order, target, now);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return ServiceResponse<Order>.Ok(order);
        }
        catch (Exception ex) when (ex is DbException or DbUpdateException)
        {
            return ServiceResponse<Order>.Fail($"Could not update order {orderId}: {ex.Message}");
        }
    }

    private static void CascadeToTickets(Order order, OrderStatus target, DateTime now)
    {
        foreach (var ticket in order.Items.SelectMany(i => i.Tickets))
        {
            var next = target switch
            {
                OrderStatus.Paid when ticket.Status == TicketStatus.Pending
                    => TicketStatus.Available,
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
