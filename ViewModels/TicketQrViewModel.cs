using tickets_management.Models;

namespace tickets_management.ViewModels;

/// <summary>A ticket paired with its pre-rendered QR code as a PNG data URI.</summary>
public class TicketQrViewModel
{
    public Ticket Ticket { get; init; } = null!;
    public string QrImage { get; init; } = string.Empty;
}
