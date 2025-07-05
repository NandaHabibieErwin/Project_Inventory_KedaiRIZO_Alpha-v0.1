using Microsoft.AspNetCore.Mvc;
using NuGet.ContentModel;
using Project_Inventory_KedaiRIZO_Alpha_v0._1.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Project_Inventory_KedaiRIZO_Alpha_v0._1.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;


namespace Project_Inventory_KedaiRIZO_Alpha_v0._1.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardViewModel();

            // Get summary statistics
            viewModel.TotalProducts = await _context.Product.CountAsync();
            viewModel.TotalCategories = await _context.Kategori.CountAsync();
            viewModel.TotalTransactions = await _context.Data_Transaksi.CountAsync();
            viewModel.TotalRevenue = await _context.Data_Transaksi.SumAsync(t => t.TotalAmount);

            // Get low stock products (stock < 10)
            viewModel.LowStockProducts = await _context.Product
                .Include(p => p.Kategori)
                .Where(p => p.stock < 10)
                .OrderBy(p => p.stock)
                .ToListAsync();

            // Get recent transactions (last 10)
            viewModel.RecentTransactions = await _context.Data_Transaksi
                .Include(t => t.ApplicationUser)
                .Include(t => t.Details)
                .OrderByDescending(t => t.Tanggal)
                .Take(10)
                .ToListAsync();

            // Get top selling products
            var topSellingQuery = await _context.Detail_Transaksi
                .Include(dt => dt.Product)
                .ThenInclude(p => p.Kategori)
                .GroupBy(dt => dt.ProductId)
                .Select(g => new TopSellingProductViewModel
                {
                    Id = g.Key,
                    Product_Name = g.First().Product.Product_Name,
                    harga = g.First().Product.harga,
                    stock = g.First().Product.stock,
                    Kategori = g.First().Product.Kategori,
                    TotalSold = g.Sum(dt => dt.Quantity)
                })
                .OrderByDescending(p => p.TotalSold)
                .Take(10)
                .ToListAsync();

            viewModel.TopSellingProducts = topSellingQuery;

            // Get category data for chart
            viewModel.CategoryData = await _context.Product
                .Include(p => p.Kategori)
                .GroupBy(p => p.Kategori.Nama_Kategori)
                .Select(g => new CategoryDataViewModel
                {
                    CategoryName = g.Key,
                    ProductCount = g.Count()
                })
                .OrderByDescending(c => c.ProductCount)
                .ToListAsync();

            // Get monthly sales data for chart (last 12 months)
            var monthlyData = await _context.Data_Transaksi
    .Where(t => t.Tanggal >= DateTime.Now.AddMonths(-12))
    .GroupBy(t => new { t.Tanggal.Year, t.Tanggal.Month })
    .Select(g => new
    {
        Year = g.Key.Year,
        Month = g.Key.Month,
        TotalSales = g.Sum(t => t.TotalAmount)
    })
    .OrderBy(g => g.Year).ThenBy(g => g.Month)
    .ToListAsync();

            // Convert to view model on client side
            viewModel.MonthlySalesData = monthlyData
                .Select(g => new MonthlySalesDataViewModel
                {
                    Month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Month) + " " + g.Year,
                    TotalSales = g.TotalSales
                })
                .ToList();

            return View(viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> GetLowStockAlert()
        {
            var lowStockCount = await _context.Product
                .Where(p => p.stock < 10)
                .CountAsync();

            return Json(new { count = lowStockCount });
        }

        [HttpGet]
        public async Task<IActionResult> GetTodaySales()
        {
            var today = DateTime.Today;
            var todaySales = await _context.Data_Transaksi
                .Where(t => t.Tanggal.Date == today)
                .SumAsync(t => t.TotalAmount);

            return Json(new { sales = todaySales });
        }

        [HttpGet]
        public async Task<IActionResult> GetProductStockStatus()
        {
            var stockStatus = await _context.Product
                .GroupBy(p => p.stock < 10 ? "Low Stock" : p.stock < 50 ? "Medium Stock" : "High Stock")
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return Json(stockStatus);
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
    public class DashboardViewModel
    {
        // Summary Statistics
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalRevenue { get; set; }

        // Product Lists
        public List<Product> LowStockProducts { get; set; } = new List<Product>();
        public List<TopSellingProductViewModel> TopSellingProducts { get; set; } = new List<TopSellingProductViewModel>();

        // Transaction Data
        public List<Data_Transaksi> RecentTransactions { get; set; } = new List<Data_Transaksi>();

        // Chart Data
        public List<CategoryDataViewModel> CategoryData { get; set; } = new List<CategoryDataViewModel>();
        public List<MonthlySalesDataViewModel> MonthlySalesData { get; set; } = new List<MonthlySalesDataViewModel>();
    }

    public class TopSellingProductViewModel
    {
        public int Id { get; set; }
        public string Product_Name { get; set; }
        public int harga { get; set; }
        public int stock { get; set; }
        public Kategori? Kategori { get; set; }
        public int TotalSold { get; set; }
    }

    public class CategoryDataViewModel
    {
        public string CategoryName { get; set; }
        public int ProductCount { get; set; }
    }

    public class MonthlySalesDataViewModel
    {
        public string Month { get; set; }
        public decimal TotalSales { get; set; }
    }
}

