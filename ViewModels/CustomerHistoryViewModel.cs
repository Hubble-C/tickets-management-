namespace tickets_management.ViewModels;

public class CustomerHistoryViewModel
{
    public int? CustomerId { get; set; }
    public bool Searched { get; set; }
    public IReadOnlyList<TicketQrViewModel> Tickets { get; init; } = [];
}
