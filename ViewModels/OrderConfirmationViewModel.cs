using tickets_management.Dto;
using tickets_management.Models;

namespace tickets_management.ViewModels;

public class OrderConfirmationViewModel
{
    public Order Order { get; init; } = null!;
    public IReadOnlyList<TicketQrViewModel> Tickets { get; init; } = [];

    /// <summary>Catalog event details keyed by EventId, for printing real titles/dates.</summary>
    public IReadOnlyDictionary<int, EventInfoDto> Events { get; init; }
        = new Dictionary<int, EventInfoDto>();

    public decimal Total => Order.Items.Sum(i => i.Total);

    /// <summary>Event info for a ticket, or null if the catalog had no match.</summary>
    public EventInfoDto? EventFor(Models.Ticket ticket) =>
        Events.TryGetValue(ticket.EventId, out var ev) ? ev : null;
}
