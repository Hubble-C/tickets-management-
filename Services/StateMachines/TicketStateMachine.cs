using tickets_management.Enums;

namespace tickets_management.Services.StateMachines;

/// <summary>
/// Single source of truth for the ticket lifecycle. Only knows about the
/// ticket's own status; cross-entity rules (e.g. a ticket may only be marked
/// <see cref="TicketStatus.Used"/> while its order is paid) are enforced by the
/// services that own both aggregates.
/// </summary>
public static class TicketStateMachine
{
    private static readonly IReadOnlyDictionary<TicketStatus, TicketStatus[]> Transitions =
        new Dictionary<TicketStatus, TicketStatus[]>
        {
            [TicketStatus.Pending] = [TicketStatus.Available, TicketStatus.Cancelled],
            [TicketStatus.Available] = [TicketStatus.Used, TicketStatus.Returned, TicketStatus.Cancelled],
            [TicketStatus.Used] = [],       // terminal: a scanned ticket is spent
            [TicketStatus.Returned] = [],   // terminal
            [TicketStatus.Cancelled] = [],  // terminal
        };

    public static bool CanTransition(TicketStatus from, TicketStatus to) =>
        Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static void EnsureCanTransition(TicketStatus from, TicketStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidOperationException(
                $"Illegal ticket transition: {from} -> {to}.");
        }
    }
}
