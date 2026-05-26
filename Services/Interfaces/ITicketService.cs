using tickets_management.Models;
using tickets_management.Response;

namespace tickets_management.Services.Interfaces;

public interface ITicketService
{
    Task<ServiceResponse<Ticket>> MarkAsUsedAsync(string ticketCode, CancellationToken ct = default);
    Task<IReadOnlyList<Ticket>> GetByCustomerAsync(int customerId, CancellationToken ct = default);
}
