using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tickets_management.Models.ViewModels;
using tickets_management.Enums;
using tickets_management.Services.Interfaces;

namespace tickets_management.Controllers
{
    [Authorize(Roles = "Seller")]
    public class BoxOfficeController : Controller
    {
        private readonly ILogin _login;

        public BoxOfficeController(ILogin login)
        {
            _login = login;
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
                // Token dentro de Response.data para mañana 
                HttpContext.Session.SetString("Username", username);
                HttpContext.Session.SetString("JWToken", response.Data);
                
                return RedirectToAction("Pos");
            }
            
            ModelState.AddModelError(string.Empty, "Credenciales incorrectas o no tienes permisos de Seller.");
            return View();
        }

        [HttpGet]
        public IActionResult Orders()
        {
            var resumenInicial = new OrderSumary
            {
                Subtotal = 0,
                Tax = 0,
                Discount = 0,
                Total = 0,
                TypePayment = TypePayment.Cash,
                CartSummaries = new List<CartSummary>()
            };

            return View(resumenInicial);
        }

 
        
        
        [HttpGet]
        public IActionResult Logout()
        {
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Pos()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Checkout(int showId, List<string> seats, string customerEmail, string paymentMethod)
        {
           
            var orderNum = $"TKT-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(100, 999)}";
            return RedirectToAction("PrintTicket", new { 
                orderNumber = orderNum,
                showName = "The Phantom of the Opera",
                showTime = "Monday, October 12 • 07:00 PM",
                hall = "Main Hall",
                seats = string.Join(", ", seats ?? new List<string>()),
                email = customerEmail,
                paymentMethod = paymentMethod,
                subtotal = "45.00",
                serviceFee = "2.50",
                total = "47.50"
            });
        }

        [HttpGet]
        public IActionResult PrintTicket(string orderNumber, string showName, string showTime, string hall, string seats, string email, string paymentMethod, string subtotal, string serviceFee, string total)
        {
            ViewData["OrderNumber"] = orderNumber;
            ViewData["ShowName"] = showName;
            ViewData["ShowTime"] = showTime;
            ViewData["Hall"] = hall;
            ViewData["Seats"] = seats;
            ViewData["Email"] = email;
            ViewData["PaymentMethod"] = paymentMethod;
            ViewData["Subtotal"] = subtotal;
            ViewData["ServiceFee"] = serviceFee;
            ViewData["Total"] = total;
            return View();
        }
        
        public IActionResult Seats()
        {
            return View();
        }
    }
}
