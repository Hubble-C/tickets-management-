using Microsoft.AspNetCore.Mvc;

namespace tickets_management.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}