namespace tickets_management.Models.ViewModels;

public class CartSummary
{
    public int Quantity { get; set; }
    public string Concept { get; set; }
    public string SeatsDetail{get; set;}
    public decimal LineTotal { get; set; }
}