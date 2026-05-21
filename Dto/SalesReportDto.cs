using tickets_management.Enums;

namespace tickets_management.Dto;

/// <summary>Aggregated figures for the box-office sales report.</summary>
public class SalesReportDto
{
    public int TotalOrders { get; set; }
    public int PaidOrders { get; set; }
    public decimal GrossRevenue { get; set; }
    public int TicketsSold { get; set; }
    public int TicketsUsed { get; set; }
    public Dictionary<OrderStatus, int> OrdersByStatus { get; set; } = new();
    public List<EventSalesDto> ByEvent { get; set; } = new();
}

public class EventSalesDto
{
    public int EventId { get; set; }
    public int TicketsSold { get; set; }
    public decimal Revenue { get; set; }
}
