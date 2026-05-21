using Microsoft.AspNetCore.Mvc;

namespace tickets_management.Controllers
{
    public class BoxOfficeController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string employeeId, string pin)
        {
            // Redirección directa al punto de venta (POS)
            return RedirectToAction("Pos");
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
            // Pasar variables de simulación directamente a la vista de impresión
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
    }
}
