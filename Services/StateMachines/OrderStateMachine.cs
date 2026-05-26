using tickets_management.Enums;

namespace tickets_management.Services.StateMachines;

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
