namespace tickets_management.Enums;

public enum OrderStatus
{
    // Pending must be 0 so a freshly created order defaults to "not yet paid".
    Pending = 0,
    Paid,
    Cancelled,
    Rejected
}
