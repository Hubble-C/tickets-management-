namespace tickets_management.Services.Interfaces;

public interface IN8nService
{
    Task SendTicketPurchaseNotificationAsync(TicketPurchasePayload payload);
}

// ── Payload models ──────────────────────────────────────────────────────────

public class TicketPurchasePayload
{
    public string Source { get; set; } = "app_web";
    public N8nCustomer Customer { get; set; } = new();
    public N8nOrder Order { get; set; } = new();
    public N8nInvoice Invoice { get; set; } = new();
    public N8nEvent Event { get; set; } = new();
}

public class N8nCustomer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class N8nOrder
{
    public string Id { get; set; } = string.Empty;
    public string PurchaseDate { get; set; } = string.Empty;
    public List<N8nTicket> Tickets { get; set; } = new();
}

public class N8nTicket
{
    public string Type { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string Section { get; set; } = string.Empty;
    public string DoorsOpen { get; set; } = string.Empty;
    public string Seat { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
}

public class N8nInvoice
{
    public string Nit { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = "Paid";
    public decimal Total { get; set; }
}

public class N8nEvent
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public int VenueId { get; set; }
}
