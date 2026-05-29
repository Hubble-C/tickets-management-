using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tickets_management.Models.ViewModels;
using tickets_management.Enums;
using tickets_management.Models;
using tickets_management.Services.Interfaces;

namespace tickets_management.Controllers
{
    public class BoxOfficeController : Controller
    {
        private readonly ILogin _login;
        private readonly IOrderService _orderService;
        private readonly IEventService _eventService;

        public BoxOfficeController(ILogin login, IOrderService orderService,  IEventService eventService)
        {
            _orderService = orderService;
            _login = login;
            _eventService = eventService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var response = await _login.Login(username, password);
            
            if (response != null && response.Success)
            {
                HttpContext.Session.SetString("Username", username);
                HttpContext.Session.SetString("JWToken", response.Data.Token);
                return RedirectToAction("Pos");
            }
            else
            {
                ViewBag.Message = response?.Message ?? "Login failed";
                ViewBag.Success = false;
                
                ModelState.AddModelError(string.Empty, "Credenciales incorrectas o no tienes permisos de Seller.");
            }
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            var token    = HttpContext.Session.GetString("JWToken");
            var session  = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login");

            var order = await _orderService.GetOrderAsync(session);

            OrderSumary orderSummary;

            if (order != null && order.SelectedSeats.Any())
            {
                orderSummary = new OrderSumary
                {
                    Subtotal      = order.Subtotal,
                    Tax           = order.Tax,
                    Discount      = order.Discount,
                    Total         = order.Total,
                    TypePayment   = TypePayment.Cash,
                    CartSummaries = order.SelectedSeats.Select(s => new CartSummary
                    {
                        Concept     = "Entrada de Teatro",
                        Quantity    = 1,
                        SeatsDetail = $"Fila {s.Row}, Asiento {s.SeatNumber} ({s.Zone})",
                        LineTotal   = s.Price
                    }).ToList()
                };
            }
            else
            {
                orderSummary = new OrderSumary
                {
                    Subtotal      = 0,
                    Tax           = 0,
                    Discount      = 0,
                    Total         = 0,
                    TypePayment   = TypePayment.Cash,
                    CartSummaries = new List<CartSummary>()
                };
            }


            return View(orderSummary);
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("JWToken");
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> Pos()
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login");
 
            var events = await _eventService.GetActiveEventsAsync();
            return View(events);

        }

        [HttpPost]
        public async Task<IActionResult> AddSeat(string seatId)
        {
            var username = HttpContext.Session.GetString("Username");
            await _orderService.AddSeatAsync(username, new SeatViewModel { Id = seatId });
            return RedirectToAction(nameof(Pos));
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(string PaymentMethod)
        {
            var username = HttpContext.Session.GetString("Username");
            var token    = HttpContext.Session.GetString("JWToken");

            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login");

            var tempOrder = await _orderService.GetOrderAsync(username);
            if (tempOrder != null)
            {
                await _orderService.SaveOrderAsync(tempOrder);
                await _orderService.ClearOrderAsync(username);
                return RedirectToAction("PrintTicket", new
                {
                    orderNumber = tempOrder.Id,
                    total       = tempOrder.Total.ToString("F2"),
                });
            }
            return RedirectToAction("Pos");
        }

        [HttpGet]
        public IActionResult PrintTicket(string orderNumber, string showName, string showTime,
            string hall, string seats, string email, string paymentMethod,
            string subtotal, string serviceFee, string total)
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login");

            ViewData["OrderNumber"]   = orderNumber;
            ViewData["ShowName"]      = showName;
            ViewData["ShowTime"]      = showTime;
            ViewData["Hall"]          = hall;
            ViewData["Seats"]         = seats;
            ViewData["Email"]         = email;
            ViewData["PaymentMethod"] = paymentMethod;
            ViewData["Subtotal"]      = subtotal;
            ViewData["ServiceFee"]    = serviceFee;
            ViewData["Total"]         = total;
            return View();
        }

        public IActionResult Seats()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PersistOrder(string orderData)
        {
            var username = HttpContext.Session.GetString("Username");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var order   = JsonSerializer.Deserialize<CurrentOrder>(orderData, options);

            if (order?.Seats != null)
            {
                foreach (var seat in order.Seats)
                {
                   
                    await _orderService.AddSeatAsync(username, seat);
                }
            }

            return RedirectToAction("Orders");
        }
        [HttpPost]
        public async Task<IActionResult> CancelOrder()
        {
            var username = HttpContext.Session.GetString("Username");
            await _orderService.ClearOrderAsync(username);
            return RedirectToAction("Pos");
        }
    }
    
}