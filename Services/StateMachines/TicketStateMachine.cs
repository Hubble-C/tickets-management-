using tickets_management.Enums;

namespace tickets_management.Services.StateMachines;

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
}
