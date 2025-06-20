using Microsoft.AspNetCore.Mvc;
using NuGet.ContentModel;
using Project_Inventory_KedaiRIZO_Alpha_v0._1.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;

namespace Project_Inventory_KedaiRIZO_Alpha_v0._1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
