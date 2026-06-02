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
    public class ValidateTicketRequest
    {
        public string TicketCode { get; set; }
        public int EventId { get; set; }
    }

    public class BoxOfficeController : Controller
    {
        private readonly ILogin _login;
        private readonly IOrderService _orderService;
        private readonly IEventService _eventService;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IN8nService _n8nService;

        public BoxOfficeController(ILogin login, IOrderService orderService, IEventService eventService, IDbConnectionFactory dbConnectionFactory, IN8nService n8nService)
        {
            _orderService = orderService;
            _login = login;
            _eventService = eventService;
            _dbConnectionFactory = dbConnectionFactory;
            _n8nService = n8nService;
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
            var token    = HttpContext.Session.GetString("JWToken");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(username))
                return RedirectToAction("Login");

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
                using var salesConnCheck = _dbConnectionFactory.GetSalesConnection();
                var selectedSeatsList = tempOrder.SelectedSeats.Select(s => $"{s.Row}-{s.SeatNumber}").ToList();
                
                if (selectedSeatsList.Any())
                {
                    var checkQuery = @"SELECT seat FROM tickets WHERE event_id = @EventId AND seat IN @Seats AND status IN ('Pending', 'Scanned', 'Active')";
                    var alreadySold = await salesConnCheck.QueryAsync<string>(checkQuery, new { EventId = tempOrder.EventId, Seats = selectedSeatsList });

                    if (alreadySold.Any())
                    {
                        TempData["OrderError"] = $"Lo sentimos, los siguientes asientos acaban de ser vendidos a otra persona: {string.Join(", ", alreadySold)}";
                        return RedirectToAction("Pos");
                    }
                }

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
                                            VALUES (@OrderItemId, @EventId, @Seat, @TicketCode, 'Pending', NOW(), NOW())";
                        
                        var itemsList = savedOrder.Items.ToList();
                        for (int i = 0; i < tempOrder.SelectedSeats.Count && i < itemsList.Count; i++)
                        {
                            var seat = tempOrder.SelectedSeats[i];
                            var orderItem = itemsList[i];
                            var seatStr = $"{seat.Row}-{seat.SeatNumber}";
                            var ticketCode = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();
                            generatedTickets.Add($"{ticketCode}|{seatStr}");
                            
                            await salesConn.ExecuteAsync(queryTicket, new { OrderItemId = orderItem.Id, EventId = orderItem.EventId, Seat = seatStr, TicketCode = ticketCode });
                        }
                    }
                    catch (Exception ex) 
                    { 
                        Console.WriteLine("TICKET INSERT ERROR: " + ex);
                    }
                }

                await _orderService.ClearOrderAsync(username);

                // ── n8n: disparar correo al comprador ────────────────────────────────
                var finalEmailForN8n = !string.IsNullOrEmpty(Email) ? Email : ExistingEmail;
                var allEvents    = await _eventService.GetActiveEventsAsync();
                var currentEvent = allEvents.FirstOrDefault(e => e.Id == int.Parse(tempOrder.EventId));
                if (!string.IsNullOrEmpty(finalEmailForN8n) && savedOrder != null && currentEvent != null)
                {
                    var n8nTickets = new List<N8nTicket>();

                    for (int i = 0; i < generatedTickets.Count; i++)
                    {
                        var parts    = generatedTickets[i].Split('|');
                        var code     = parts[0];
                        var seatStr  = parts.Length > 1 ? parts[1] : string.Empty;
                        var seat     = i < tempOrder.SelectedSeats.Count ? tempOrder.SelectedSeats[i] : null;
                        var isVip    = seat?.Zone == LabelZone.VIP;
                        var price    = seat?.Price ?? 0;
                        var ttName   = isVip ? "VIP" : "General";
                        var emoji    = isVip ? "⭐" : "🎸";
                        var section  = isVip ? "VIP Lounge + Bar" : "General Floor";
                        var doors    = isVip ? "3:00 PM" : "4:00 PM";

                        n8nTickets.Add(new N8nTicket
                        {
                            Type      = ttName,
                            Emoji     = emoji,
                            Code      = code,
                            UnitPrice = price,
                            Section   = section,
                            DoorsOpen = doors
                        });
                    }

                    var payload = new TicketPurchasePayload
                    {
                        Source   = "app_web",
                        Customer = new N8nCustomer { Name = Name ?? string.Empty, Email = finalEmailForN8n },
                        Order    = new N8nOrder
                        {
                            Id           = savedOrder.Id.ToString(),
                            PurchaseDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            Tickets      = n8nTickets
                        },
                        Invoice = new N8nInvoice
                        {
                            Nit           = savedOrder.Nit,
                            PaymentMethod = TypePayment.ToString(),
                            Status        = "Paid",
                            Total         = tempOrder.Total
                        },
                        Event = new N8nEvent
                        {
                            Name  = currentEvent.Name,
                            Date  = currentEvent.StartDate.ToString("MMM dd, yyyy"),
                            Venue = currentEvent.VenueName ?? "Teatro Central"
                        }
                    };

                    _ = Task.Run(() => _n8nService.SendTicketPurchaseNotificationAsync(payload));
                }
                // ────────────────────────────────────────────────────────────────────

                return RedirectToAction("PrintTicket", new { 
                    orderNumber = savedOrder.Id.ToString(),
                    eventId = int.Parse(tempOrder.EventId),
                    hourAt = DateTime.Now.ToString("HH:mm"),
                    tickets = string.Join(",", generatedTickets)
                });
            }
            return RedirectToAction("Pos");
        }

        [HttpGet]
        public async Task<IActionResult> PrintTicket(string orderNumber, int eventId, string hourAt, string tickets)
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login");

            var events = await _eventService.GetActiveEventsAsync();
            var currentEvent = events.FirstOrDefault(e => e.Id == eventId);

            ViewData["OrderNumber"] = orderNumber;
            ViewData["EventName"] = currentEvent?.Name ?? "Evento";
            ViewData["Date"] = currentEvent?.StartDate.ToString("dd/MM/yyyy") ?? DateTime.Now.ToString("dd/MM/yyyy");
            ViewData["Venue"] = currentEvent?.VenueName ?? "Teatro Central";
            ViewData["HourAt"] = hourAt;
            ViewData["Tickets"] = tickets;

            // Fetch full ticket info for QR codes
            var ticketCodes = new List<string>();
            if (!string.IsNullOrEmpty(tickets))
            {
                foreach (var t in tickets.Split(','))
                {
                    var parts = t.Split('|');
                    if (parts.Length > 0) ticketCodes.Add(parts[0]);
                }
            }
            
            var ticketDtos = new List<tickets_management.Models.ViewModels.BoughtTicketDto>();
            try
            {
                using var conn = _dbConnectionFactory.GetSalesConnection();
                var query = @"
                    SELECT 
                        o.customer_id AS UserId,
                        t.seat AS Seat,
                        oi.price_ticket AS Price,
                        e.StartDate AS EventDate,
                        e.Id AS EventId,
                        e.VenueId AS VenueId,
                        t.status AS DbStatus,
                        t.ticket_code AS TicketCode
                    FROM db_sales.tickets t
                    INNER JOIN db_sales.order_items oi ON t.order_item_id = oi.id
                    INNER JOIN db_sales.orders o ON oi.order_id = o.id
                    INNER JOIN db_catalog.Events e ON t.event_id = e.Id
                    WHERE t.ticket_code IN @TicketCodes";

                if (ticketCodes.Any())
                {
                    ticketDtos = (await conn.QueryAsync<tickets_management.Models.ViewModels.BoughtTicketDto>(query, new { TicketCodes = ticketCodes })).ToList();
                }
            }
            catch (Exception) { /* Ignored for view rendering safety */ }

            ViewData["TicketDtos"] = ticketDtos;

            return View();
        }

        [HttpGet("api/tickets/bought")]
        public async Task<IActionResult> GetBoughtTickets()
        {
            try
            {
                using var conn = _dbConnectionFactory.GetSalesConnection();
                var query = @"
                    SELECT 
                        o.customer_id AS UserId,
                        t.seat AS Seat,
                        oi.price_ticket AS Price,
                        e.StartDate AS EventDate,
                        e.Id AS EventId,
                        e.VenueId AS VenueId,
                        t.status AS DbStatus,
                        t.ticket_code AS TicketCode
                    FROM db_sales.tickets t
                    INNER JOIN db_sales.order_items oi ON t.order_item_id = oi.id
                    INNER JOIN db_sales.orders o ON oi.order_id = o.id
                    INNER JOIN db_catalog.Events e ON t.event_id = e.Id";

                var tickets = await conn.QueryAsync<tickets_management.Models.ViewModels.BoughtTicketDto>(query);
                return Ok(tickets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error retrieving bought tickets", Error = ex.Message });
            }
        }

        [HttpGet("api/tickets/bought/{ticketCode}")]
        public async Task<IActionResult> GetBoughtTicket(string ticketCode)
        {
            try
            {
                using var conn = _dbConnectionFactory.GetSalesConnection();
                var query = @"
                    SELECT 
                        o.customer_id AS UserId,
                        t.seat AS Seat,
                        oi.price_ticket AS Price,
                        e.StartDate AS EventDate,
                        e.Id AS EventId,
                        e.VenueId AS VenueId,
                        t.status AS DbStatus,
                        t.ticket_code AS TicketCode
                    FROM db_sales.tickets t
                    INNER JOIN db_sales.order_items oi ON t.order_item_id = oi.id
                    INNER JOIN db_sales.orders o ON oi.order_id = o.id
                    INNER JOIN db_catalog.Events e ON t.event_id = e.Id
                    WHERE t.ticket_code = @TicketCode";

                var ticket = await conn.QueryFirstOrDefaultAsync<tickets_management.Models.ViewModels.BoughtTicketDto>(query, new { TicketCode = ticketCode });
                
                if (ticket == null)
                    return NotFound(new { Message = "Ticket not found" });

                return Ok(ticket);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error retrieving ticket", Error = ex.Message });
            }
        }

        [HttpPost("api/tickets/scan/{ticketCode}")]
        public async Task<IActionResult> ScanTicket(string ticketCode)
        {
            try
            {
                using var conn = _dbConnectionFactory.GetSalesConnection();
                // Find ticket
                var checkQuery = "SELECT status FROM tickets WHERE ticket_code = @TicketCode";
                var currentStatus = await conn.QueryFirstOrDefaultAsync<string>(checkQuery, new { TicketCode = ticketCode });

                if (string.IsNullOrEmpty(currentStatus))
                    return NotFound(new { Message = "Ticket not found" });

                if (currentStatus.Equals("Scanned", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { Message = "Ticket has already been scanned!" });

                // Update to Scanned
                var updateQuery = "UPDATE tickets SET status = 'Scanned', updated_at = NOW() WHERE ticket_code = @TicketCode";
                await conn.ExecuteAsync(updateQuery, new { TicketCode = ticketCode });

                return Ok(new { Message = "Ticket successfully scanned", TicketCode = ticketCode, Status = "Scanned" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error scanning ticket", Error = ex.Message });
            }
        }

        [HttpGet("api/events/active")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveEventsApi()
        {
            var events = await _eventService.GetActiveEventsAsync();
            return Ok(events);
        }

        [HttpPost("api/tickets/validate")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateTicketApi([FromBody] ValidateTicketRequest request)
        {
            try
            {
                using var conn = _dbConnectionFactory.GetSalesConnection();
                
                var query = @"
                    SELECT 
                        t.ticket_code AS TicketCode,
                        t.status AS Status,
                        t.event_id AS EventId,
                        t.seat AS Seat,
                        e.StartDate AS EventDate
                    FROM db_sales.tickets t
                    INNER JOIN db_catalog.Events e ON t.event_id = e.Id
                    WHERE t.ticket_code = @Code OR t.order_item_id IN (SELECT id FROM db_sales.order_items WHERE order_id = @OrderId)
                ";
                
                int.TryParse(request.TicketCode, out int orderId);
                
                var tickets = await conn.QueryAsync<dynamic>(query, new { Code = request.TicketCode, OrderId = orderId > 0 ? orderId : -1 });
                var ticketList = tickets.ToList();

                if (!ticketList.Any())
                    return Ok(new { Result = "NotFound", Message = "Ticket or Order not found", TicketCode = request.TicketCode });

                var eventTickets = request.EventId > 0
                    ? ticketList.Where(t => t.EventId == request.EventId).ToList()
                    : ticketList;

                if (!eventTickets.Any())
                    return Ok(new { Result = "InvalidState", Message = "Ticket does not belong to this Event/Venue", TicketCode = request.TicketCode });

                var eventDate = (DateTime)eventTickets.First().EventDate;
                if (eventDate.Date < DateTime.Today)
                    return Ok(new { Result = "InvalidState", Message = "Event has already expired", TicketCode = request.TicketCode });
                    
                var pendingTickets = eventTickets.Where(t => t.Status == "Pending").ToList();
                
                if (!pendingTickets.Any())
                    return Ok(new { Result = "AlreadyUsed", Message = "Ticket(s) already scanned", TicketCode = request.TicketCode });

                var codesToUpdate = pendingTickets.Select(t => (string)t.TicketCode).ToList();
                var updateQuery = "UPDATE db_sales.tickets SET status = 'Scanned', updated_at = NOW() WHERE ticket_code IN @Codes";
                await conn.ExecuteAsync(updateQuery, new { Codes = codesToUpdate });

                var seats = string.Join(", ", pendingTickets.Select(t => t.Seat));
                
                return Ok(new { 
                    Result = "Granted", 
                    Message = "Valid", 
                    TicketCode = request.TicketCode,
                    Seat = seats,
                    EventId = request.EventId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Result = "Error", Message = ex.Message });
            }
        }

        public IActionResult Seats() => View();
    }
}