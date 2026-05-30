using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tickets_management.Models.ViewModels;
using tickets_management.Enums;
using tickets_management.Models;
using tickets_management.Services.Interfaces;
using Dapper;
namespace tickets_management.Controllers
{
    public class BoxOfficeController : Controller
    {
        private readonly ILogin _login;
        private readonly IOrderService _orderService;
        private readonly IEventService _eventService;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public BoxOfficeController(ILogin login, IOrderService orderService, IEventService eventService, IDbConnectionFactory dbConnectionFactory)
        {
            _orderService = orderService;
            _login = login;
            _eventService = eventService;
            _dbConnectionFactory = dbConnectionFactory;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login() => View();

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

            ViewBag.Message = response?.Message ?? "Login failed";
            ViewBag.Success = false;
            ModelState.AddModelError(string.Empty, "Credenciales incorrectas o no tienes permisos de Seller.");
            return View();
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

        // Endpoint que llama el JS al abrir el mapa de un evento
        // Devuelve JSON con: asientos ocupados + precios reales por zona
        [HttpGet]
        public async Task<IActionResult> GetSeatData(int eventId)
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
                return Unauthorized();

            // Asientos ya vendidos en db_sales
            var occupiedSeats = await _eventService.GetOccupiedSeatsByEventAsync(eventId);

            // Precios reales desde db_catalog TicketTypes
            var ticketTypes = await _eventService.GetTicketTypesByEventAsync(eventId);

            // También incluimos los asientos en proceso de compra ahora mismo (Singleton)
            var username = HttpContext.Session.GetString("Username");
            var currentOrder = await _orderService.GetOrderAsync(username);
            var pendingSeats = currentOrder != null && currentOrder.EventId == eventId.ToString()
                ? currentOrder.SelectedSeats
                    .Select(s => $"{s.Row}-{s.SeatNumber}")
                    .ToList()
                : new List<string>();

            return Json(new
            {
                occupiedSeats = occupiedSeats,
                pendingSeats  = pendingSeats,
                ticketTypes   = ticketTypes.Select(t => new
                {
                    name  = t.Name.ToUpper(),
                    price = t.Price
                })
            });
        }

        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            var token   = HttpContext.Session.GetString("JWToken");
            var session = HttpContext.Session.GetString("Username");

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

        [HttpPost]
        public async Task<IActionResult> PersistOrder(string orderData)
        {
            var username = HttpContext.Session.GetString("Username");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var order   = JsonSerializer.Deserialize<CurrentOrder>(orderData, options);

            if (order?.Seats != null)
            {
                // Limpiamos antes de repoblar por si el usuario volvió y cambió selección
                await _orderService.ClearOrderAsync(username);

                foreach (var seat in order.Seats)
                {
                    await _orderService.AddSeatAsync(username, seat);
                }

                // Guardamos el eventId en el TempOrder para usarlo en SaveOrderAsync
                var tempOrder = await _orderService.GetOrderAsync(username);
                if (tempOrder != null)
                    tempOrder.EventId = order.EventId.ToString();
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

        [HttpPost]
        public async Task<IActionResult> Checkout(string TypePayment, string Name, string Email, string ExistingEmail, string Phone)
        {
            var username = HttpContext.Session.GetString("Username");
            var token    = HttpContext.Session.GetString("JWToken");

            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login");

            var tempOrder = await _orderService.GetOrderAsync(username);
            if (tempOrder != null)
            {
                var savedOrder = await _orderService.SaveOrderAsync(tempOrder);

                // Insert User in db_public.users 
                var finalEmail = !string.IsNullOrEmpty(Email) ? Email : ExistingEmail;
                if (!string.IsNullOrEmpty(finalEmail) && !string.IsNullOrEmpty(Name))
                {
                    try
                    {
                        var randomPassword = Guid.NewGuid().ToString("N").Substring(0, 8);
                        using var publicConn = _dbConnectionFactory.GetPublicConnection();
                        var queryUser = "INSERT INTO users (name, email, phone, password, created_at) VALUES (@Name, @Email, @Phone, @Password, NOW())";
                        await publicConn.ExecuteAsync(queryUser, new { Name, Email = finalEmail, Phone, Password = randomPassword });
                    }
                    catch { /* Handle error or ignore if user already exists */ }
                }

                var generatedTickets = new List<string>();

                // Insert Tickets in db_sales.tickets
                if (savedOrder != null)
                {
                    try
                    {
                        using var salesConn = _dbConnectionFactory.GetSalesConnection();
                        var queryTicket = @"INSERT INTO tickets (order_item_id, event_id, seat, ticket_code, status, created_at, updated_at) 
                                            VALUES (@OrderItemId, @EventId, @Seat, @TicketCode, 'Active', NOW(), NOW())";
                        
                        var itemsList = savedOrder.Items.ToList();
                        for (int i = 0; i < tempOrder.SelectedSeats.Count && i < itemsList.Count; i++)
                        {
                            var seat = tempOrder.SelectedSeats[i];
                            var orderItem = itemsList[i];
                            var seatStr = $"{seat.Row}-{seat.SeatNumber}";
                            var ticketCode = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();
                            generatedTickets.Add(ticketCode);
                            
                            await salesConn.ExecuteAsync(queryTicket, new { OrderItemId = orderItem.Id, EventId = orderItem.EventId, Seat = seatStr, TicketCode = ticketCode });
                        }
                    }
                    catch { }
                }

                await _orderService.ClearOrderAsync(username);

                // Pasar los códigos de ticket generados al TempData para mostrar el modal en Pos
                TempData["OrderSuccess"] = $"Factura generada. Tickets: {string.Join(", ", generatedTickets)}";

                return RedirectToAction("Pos");
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

        public IActionResult Seats() => View();
    }
}