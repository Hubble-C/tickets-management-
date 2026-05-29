namespace tickets_management.Models.ViewModels;

public class CurrentOrder
{
    public string Movie { get; set; }
    public int EventId { get; set; }
    public List<SeatViewModel> Seats { get; set; }
}