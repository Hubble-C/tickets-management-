using tickets_management.Enums;

namespace tickets_management.Services.StateMachines;

/// <summary>
/// Single source of truth for the order lifecycle. Encodes which
/// <see cref="OrderStatus"/> transitions are legal so the rules cannot drift
/// across the services that mutate orders.
/// </summary>
public static class OrderStateMachine
{
    private static readonly IReadOnlyDictionary<OrderStatus, OrderStatus[]> Transitions =
        new Dictionary<OrderStatus, OrderStatus[]>
        {
            [OrderStatus.Pending] = [OrderStatus.Paid, OrderStatus.Cancelled, OrderStatus.Rejected],
            [OrderStatus.Paid] = [OrderStatus.Cancelled], // refund / chargeback only
            [OrderStatus.Cancelled] = [],                 // terminal
            [OrderStatus.Rejected] = [],                  // terminal
        };

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}
