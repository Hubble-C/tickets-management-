using Microsoft.EntityFrameworkCore;
using tickets_management.Data;
using tickets_management.Enums;
using tickets_management.Models;
using tickets_management.Response;
using tickets_management.Services.Interfaces;
using tickets_management.Services.StateMachines;

namespace tickets_management.Services;

public class TicketService : ITicketService
{
    private readonly MySqlDbContext _db;
    private readonly TimeProvider _clock;

    public TicketService(MySqlDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<ServiceResponse<Ticket>> MarkAsUsedAsync(
        string ticketCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ticketCode))
            return ServiceResponse<Ticket>.Fail("A ticket code is required.");

        var ticket = await _db.Tickets
            .Include(t => t.OrderItem)
            .ThenInclude(oi => oi.Order)
            .FirstOrDefaultAsync(t => t.TicketCode == ticketCode, ct);

        if (ticket is null)
            return ServiceResponse<Ticket>.Fail("Ticket not found.");

        // The Reto: a ticket can only be consumed while its order is paid.
        var orderStatus = ticket.OrderItem.Order.Status;
        if (orderStatus != OrderStatus.Paid)
            return ServiceResponse<Ticket>.Fail(
                $"Ticket cannot be used: its order is {orderStatus}, not Paid.");

        if (ticket.Status == TicketStatus.Used)
            return ServiceResponse<Ticket>.Fail("Ticket has already been used.");

        if (!TicketStateMachine.CanTransition(ticket.Status, TicketStatus.Used))
            return ServiceResponse<Ticket>.Fail(
                $"Ticket cannot be used from state {ticket.Status}.");

        ticket.Status = TicketStatus.Used;
        ticket.UpdatedAt = _clock.GetUtcNow().UtcDateTime;
        await _db.SaveChangesAsync(ct);

        return ServiceResponse<Ticket>.Ok(ticket, "Ticket validated.");
    }
}
