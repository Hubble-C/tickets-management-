namespace tickets_management.Enums;

public enum TicketStatus
{
    // Pending must be 0: a ticket is inactive until its order is paid.
    Pending = 0,
    Available,
    Used,
    Returned,
    Cancelled
}
