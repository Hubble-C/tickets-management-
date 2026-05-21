using tickets_management.Models;

namespace tickets_management.ViewModels;

/// <summary>Door-scan screen: input a code and show the validation outcome.</summary>
public class TicketValidationViewModel
{
    public string? TicketCode { get; set; }
    public bool? Success { get; set; }
    public string? Message { get; set; }
    public Ticket? Ticket { get; set; }
}
