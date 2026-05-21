using tickets_management.Models;

namespace tickets_management.ViewModels;

/// <summary>A ticket paired with its pre-rendered inline-SVG QR code.</summary>
public class TicketQrViewModel
{
    public Ticket Ticket { get; init; } = null!;
    public string QrSvg { get; init; } = string.Empty;
}
