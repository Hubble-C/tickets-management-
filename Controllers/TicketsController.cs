using Microsoft.AspNetCore.Mvc;
using tickets_management.Services.Interfaces;
using tickets_management.ViewModels;

namespace tickets_management.Controllers;

/// <summary>Customer-facing flows: browse the QR history of purchased tickets.</summary>
public class TicketsController : Controller
{
    private readonly ITicketService _tickets;
    private readonly IQrCodeService _qr;

    public TicketsController(ITicketService tickets, IQrCodeService qr)
    {
        _tickets = tickets;
        _qr = qr;
    }

    [HttpGet]
    public async Task<IActionResult> History(int? customerId, CancellationToken ct)
    {
        if (customerId is not int id)
            return View(new CustomerHistoryViewModel { Searched = false });

        var tickets = await _tickets.GetByCustomerAsync(id, ct);
        return View(new CustomerHistoryViewModel
        {
            CustomerId = id,
            Searched = true,
            Tickets = tickets
                .Select(t => new TicketQrViewModel { Ticket = t, QrSvg = _qr.ToSvg(t.TicketCode) })
                .ToList(),
        });
    }
}
