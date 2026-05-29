namespace tickets_management.Models.ViewModels;

public class SeatViewModel
{
    public string Id { get; set; }
    public string Row { get; set; }
    public int Num { get; set; }
    public string Zone { get; set; }
    public decimal Price { get; set; }
}