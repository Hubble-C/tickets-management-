using Microsoft.AspNetCore.Mvc;
using tickets_management.Dto;
using tickets_management.Models;
using tickets_management.Services.Interfaces;
using tickets_management.ViewModels;

namespace tickets_management.Controllers;

/// <summary>Box-office (taquilla) flows: sell, confirm, settle and validate tickets.</summary>
public class BoxOfficeController : Controller
{
    private readonly IOrderService _orders;
    private readonly ITicketService _tickets;
    private readonly IQrCodeService _qr;

    public BoxOfficeController(IOrderService orders, ITicketService tickets, IQrCodeService qr)
    {
        _orders = orders;
        _tickets = tickets;
        _qr = qr;
    }
    
    [HttpGet]
    public async Task<IActionResult> Print(int id, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(id, ct);
        return order is null ? NotFound() : View(BuildConfirmation(order));
    } 

    [HttpGet]
    public IActionResult Checkout() => View(new CheckoutViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel model, CancellationToken ct)
    {
        var seats = model.ParseSeats();
        if (seats.Count == 0)
            ModelState.AddModelError(nameof(model.Seats), "Enter at least one seat.");

        if (!ModelState.IsValid)
            return View(model);

        var dto = new CreateOrderDto
        {
            Nit = model.Nit,
            CustomerId = model.CustomerId,
            PaymentMethod = model.PaymentMethod,
            HourAt = model.HourAt,
            Items =
            [
                new CreateOrderItemDto
                {
                    EventId = model.EventId,
                    PriceTicket = model.PriceTicket,
                    Seats = seats,
                }
            ],
        };

        var result = await _orders.CreateOrderAsync(dto, ct);
        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Could not create the order.");
            return View(model);
        }

        // Post/Redirect/Get: a refresh of the result page never re-submits the order.
        TempData["Flash"] = $"Order #{result.Data.Id} created with {seats.Count} ticket(s).";
        return RedirectToAction(nameof(Confirmation), new { id = result.Data.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(int id, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(id, ct);
        return order is null ? NotFound() : View(BuildConfirmation(order));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(int id, CancellationToken ct)
    {
        var result = await _orders.MarkAsPaidAsync(id, ct);
        TempData["Flash"] = result.Message ?? (result.Success ? "Order paid." : "Could not pay order.");
        return RedirectToAction(nameof(Confirmation), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var result = await _orders.CancelAsync(id, ct);
        TempData["Flash"] = result.Message ?? (result.Success ? "Order cancelled." : "Could not cancel order.");
        return RedirectToAction(nameof(Confirmation), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Sales(CancellationToken ct) =>
        View(await _orders.GetSalesReportAsync(ct));

    [HttpGet]
    public IActionResult Validate() => View(new TicketValidationViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Validate(TicketValidationViewModel model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.TicketCode))
        {
            ModelState.AddModelError(nameof(model.TicketCode), "Enter a ticket code.");
            return View(model);
        }

        var result = await _tickets.MarkAsUsedAsync(model.TicketCode.Trim(), ct);
        model.Success = result.Success;
        model.Message = result.Message;
        model.Ticket = result.Data;
        return View(model);
    }

    private OrderConfirmationViewModel BuildConfirmation(Order order)
    {
        var tickets = order.Items
            .SelectMany(i => i.Tickets)
            .OrderBy(t => t.Seat)
            .Select(t => new TicketQrViewModel { Ticket = t, QrSvg = _qr.ToSvg(t.TicketCode) })
            .ToList();

        return new OrderConfirmationViewModel { Order = order, Tickets = tickets };
    }
}
