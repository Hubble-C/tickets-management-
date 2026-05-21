using tickets_management.Models;
using tickets_management.Response;

namespace tickets_management.Services.Interfaces;

public interface ITicketService
{
    /// <summary>
    /// Consumes a ticket at the venue door. Succeeds only when the ticket is
    /// Available AND its order is Paid; rejects tickets whose order is
    /// Cancelled/Rejected/Pending, or that were already used.
    /// </summary>
    Task<ServiceResponse<Ticket>> MarkAsUsedAsync(string ticketCode, CancellationToken ct = default);

    /// <summary>All tickets belonging to a customer, newest order first, for their QR history.</summary>
    Task<IReadOnlyList<Ticket>> GetByCustomerAsync(int customerId, CancellationToken ct = default);
}
