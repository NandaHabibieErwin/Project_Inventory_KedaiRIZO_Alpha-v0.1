using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml.Style;
using OfficeOpenXml;
using Project_Inventory_KedaiRIZO_Alpha_v0._1.Data;
using Project_Inventory_KedaiRIZO_Alpha_v0._1.Models;

namespace Project_Inventory_KedaiRIZO_Alpha_v0._1.Controllers
{
    [Authorize]
    public class Data_TransaksiController : Controller
    {
        private readonly ApplicationDbContext _context;

        public Data_TransaksiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Data_Transaksi
        public async Task<IActionResult> Index()
        {
            var transactions = await _context.Data_Transaksi
                .Include(d => d.ApplicationUser)
                .Include(d => d.Details)
                .ThenInclude(dt => dt.Product)
                .OrderByDescending(d => d.Tanggal)
                .ToListAsync();
            return View(transactions);
        }

        // GET: Data_Transaksi/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var data_Transaksi = await _context.Data_Transaksi
                .Include(d => d.ApplicationUser)
                .Include(d => d.Details)
                .ThenInclude(dt => dt.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (data_Transaksi == null)
            {
                return NotFound();
            }

            return View(data_Transaksi);
        }

        // GET: Data_Transaksi/Create
        [Authorize]
        public IActionResult Create()
        {
            var model = new TransactionViewModel();
            ViewBag.UserId = new SelectList(_context.Users, "Id", "UserName", model.UserId);
            ViewBag.Products = new SelectList(_context.Product, "Id", "Product_Name");          
            return View(model);
        }

        // POST: Data_Transaksi/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransactionViewModel model)
        {
            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Create main transaction
                    var dataTransaksi = new Data_Transaksi
                    {
                        UserId = model.UserId,
                        Tanggal = model.Tanggal,
                        TotalAmount = 0
                    };

                    _context.Data_Transaksi.Add(dataTransaksi);
                    await _context.SaveChangesAsync();

                    int totalAmount = 0;

                    // Add transaction details
                    foreach (var item in model.Items.Where(i => i.ProductId > 0 && i.Quantity > 0))
                    {
                        var product = await _context.Product.FindAsync(item.ProductId);
                        if (product == null || product.stock < item.Quantity)
                        {
                            ModelState.AddModelError("", $"Stock tidak cukup untuk produk {product?.Product_Name}");
                            await transaction.RollbackAsync();
                            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", model.UserId);
                            ViewData["Products"] = new SelectList(_context.Product, "Id", "Product_Name");
                            return View(model);
                        }

                        var detail = new Detail_Transaksi
                        {
                            DataTransaksiId = dataTransaksi.Id,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            total = item.Quantity * product.harga
                        };

                        // Update stock
                        product.stock -= item.Quantity;
                        totalAmount += detail.total;

                        _context.Detail_Transaksi.Add(detail);
                    }

                    // Update total amount
                    dataTransaksi.TotalAmount = totalAmount;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Terjadi kesalahan saat menyimpan transaksi");
                }
            }

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", model.UserId);
            ViewData["Products"] = new SelectList(_context.Product, "Id", "Product_Name");
            return View(model);
        }

        /*    // GET: Data_Transaksi/Edit/5
            public async Task<IActionResult> Edit(int? id)
            {
                if (id == null)
                {
                    return NotFound();
                }

                var data_Transaksi = await _context.Data_Transaksi.FindAsync(id);
                if (data_Transaksi == null)
                {
                    return NotFound();
                }
                ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", data_Transaksi.UserId);
                return View(data_Transaksi);
            }
        */

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dataTransaksi = await _context.Data_Transaksi
                .Include(d => d.Details)
                .ThenInclude(dt => dt.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (dataTransaksi == null)
            {
                return NotFound();
            }

            // Create TransactionViewModel from existing data
            var model = new TransactionViewModel
            {
                Id = dataTransaksi.Id,
                UserId = dataTransaksi.UserId,
                Tanggal = dataTransaksi.Tanggal,
                Items = dataTransaksi.Details.Select(dt => new TransactionItemViewModel
                {
                    ProductId = dt.ProductId,
                    Quantity = dt.Quantity
                }).ToList()
            };

            // Ensure at least one empty row for adding new items
            if (model.Items.Count == 0)
            {
                model.Items.Add(new TransactionItemViewModel());
            }

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", model.UserId);
            ViewData["Products"] = new SelectList(_context.Product, "Id", "Product_Name");

            return View(model);
        }

        // POST: Data_Transaksi/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TransactionViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Get existing transaction
                    var existingTransaction = await _context.Data_Transaksi
                        .Include(d => d.Details)
                        .FirstOrDefaultAsync(d => d.Id == id);

                    if (existingTransaction == null)
                    {
                        return NotFound();
                    }

                    // Restore stock from existing details before updating
                    foreach (var existingDetail in existingTransaction.Details)
                    {
                        var product = await _context.Product.FindAsync(existingDetail.ProductId);
                        if (product != null)
                        {
                            product.stock += existingDetail.Quantity; // Restore stock
                        }
                    }

                    // Remove existing details
                    _context.Detail_Transaksi.RemoveRange(existingTransaction.Details);

                    // Update main transaction
                    existingTransaction.UserId = model.UserId;
                    existingTransaction.Tanggal = model.Tanggal;
                    existingTransaction.TotalAmount = 0;

                    await _context.SaveChangesAsync();

                    int totalAmount = 0;

                    // Add new transaction details
                    foreach (var item in model.Items.Where(i => i.ProductId > 0 && i.Quantity > 0))
                    {
                        var product = await _context.Product.FindAsync(item.ProductId);
                        if (product == null || product.stock < item.Quantity)
                        {
                            ModelState.AddModelError("", $"Stock tidak cukup untuk produk {product?.Product_Name}. Stock tersedia: {product?.stock ?? 0}");
                            await transaction.RollbackAsync();

                            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", model.UserId);
                            ViewData["Products"] = new SelectList(_context.Product, "Id", "Product_Name");
                            return View(model);
                        }

                        var detail = new Detail_Transaksi
                        {
                            DataTransaksiId = existingTransaction.Id,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            total = item.Quantity * product.harga
                        };

                        // Update stock
                        product.stock -= item.Quantity;
                        totalAmount += detail.total;

                        _context.Detail_Transaksi.Add(detail);
                    }

                    // Update total amount
                    existingTransaction.TotalAmount = totalAmount;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return RedirectToAction("Details", new { id = existingTransaction.Id });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Terjadi kesalahan saat menyimpan perubahan transaksi: " + ex.Message);
                }
            }

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", model.UserId);
            ViewData["Products"] = new SelectList(_context.Product, "Id", "Product_Name");
            return View(model);
        }

        /*   // POST: Data_Transaksi/Edit/5
           [HttpPost]
           [ValidateAntiForgeryToken]
           public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,Tanggal,TotalAmount")] Data_Transaksi data_Transaksi)
           {
               if (id != data_Transaksi.Id)
               {
                   return NotFound();
               }

               if (ModelState.IsValid)
               {
                   try
                   {
                       _context.Update(data_Transaksi);
                       await _context.SaveChangesAsync();
                   }
                   catch (DbUpdateConcurrencyException)
                   {
                       if (!Data_TransaksiExists(data_Transaksi.Id))
                       {
                           return NotFound();
                       }
                       else
                       {
                           throw;
                       }
                   }
                   return RedirectToAction(nameof(Index));
               }
               ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", data_Transaksi.UserId);
               return View(data_Transaksi);
           }
           */
        // GET: Data_Transaksi/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var data_Transaksi = await _context.Data_Transaksi
                .Include(d => d.ApplicationUser)
                .Include(d => d.Details)
                .ThenInclude(dt => dt.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (data_Transaksi == null)
            {
                return NotFound();
            }

            return View(data_Transaksi);
        }

        // POST: Data_Transaksi/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]        
        public async Task<IActionResult> DeleteConfirmed(int id, bool restoreStock = true)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var data_Transaksi = await _context.Data_Transaksi
                    .Include(d => d.Details)
                    .ThenInclude(dt => dt.Product)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (data_Transaksi != null)
                {
                    // Restore stock for each product only if restoreStock is true
                    if (restoreStock)
                    {
                        foreach (var detail in data_Transaksi.Details)
                        {
                            if (detail.Product != null)
                            {
                                detail.Product.stock += detail.Quantity;
                            }
                        }

                        // Add success message for stock restoration
                        TempData["SuccessMessage"] = $"Transaksi telah dihapus dan stock {data_Transaksi.Details.Sum(d => d.Quantity)} items telah dikembalikan ke inventory.";
                    }
                    else
                    {
                        // Add message for no stock restoration
                        TempData["WarningMessage"] = $"Transaksi telah dihapus tanpa mengembalikan stock {data_Transaksi.Details.Sum(d => d.Quantity)} items ke inventory.";
                    }

                    _context.Data_Transaksi.Remove(data_Transaksi);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Terjadi kesalahan saat menghapus transaksi. Silakan coba lagi.";
                // Log the exception if you have logging configured
                // _logger.LogError(ex, "Error deleting transaction with ID: {TransactionId}", id);
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // Optional: Add a method to get transaction details for AJAX if needed
        [HttpGet]
        public async Task<IActionResult> GetTransactionDetails(int id)
        {
            var transaction = await _context.Data_Transaksi
                .Include(d => d.Details)
                .ThenInclude(dt => dt.Product)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (transaction == null)
            {
                return NotFound();
            }

            var details = transaction.Details.Select(d => new
            {
                ProductName = d.Product?.Product_Name,
                Quantity = d.Quantity,
                CurrentStock = d.Product?.stock ?? 0,
                StockAfterRestore = (d.Product?.stock ?? 0) + d.Quantity
            });

            return Json(details);
        }

        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> ExportToExcel(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                // Set default date range if not provided
                if (!startDate.HasValue)
                    startDate = DateTime.Now.AddMonths(-1);
                if (!endDate.HasValue)
                    endDate = DateTime.Now;

                // Get transaction data with ALL required includes
                var transactions = await _context.Data_Transaksi
                    .Include(d => d.ApplicationUser)
                    .Include(d => d.Details)
                        .ThenInclude(dt => dt.Product)
                            .ThenInclude(p => p.Kategori) // Add this missing include
                    .Where(d => d.Tanggal >= startDate && d.Tanggal <= endDate)
                    .OrderByDescending(d => d.Tanggal)
                    .AsNoTracking() // Add this for better performance
                    .ToListAsync();

                // Get product data with category
                var products = await _context.Product
                    .Include(p => p.Kategori) // Add this missing include
                    .OrderBy(p => p.Product_Name)
                    .AsNoTracking()
                    .ToListAsync();

                // Debug: Check if data is loaded
                System.Diagnostics.Debug.WriteLine($"Loaded {transactions.Count} transactions");
                foreach (var trans in transactions.Take(3))
                {
                    System.Diagnostics.Debug.WriteLine($"Transaction {trans.Id}: User={trans.ApplicationUser?.UserName}, Details={trans.Details?.Count}");
                }

                // Create Excel package
                using var package = new ExcelPackage();

                // Create Transaction Summary Sheet
                CreateTransactionSummarySheet(package, transactions, startDate.Value, endDate.Value);

                // Create Transaction Details Sheet
                CreateTransactionDetailsSheet(package, transactions);

                // Create Product List Sheet
                CreateProductListSheet(package, products);

                // Create Statistics Sheet
                CreateStatisticsSheet(package, transactions, products);

                // Generate file
                var fileName = $"KedaiRIZO_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var fileBytes = package.GetAsByteArray();

                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                // Add more detailed error logging
                System.Diagnostics.Debug.WriteLine($"Export error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"Gagal mengekspor data: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
        private void CreateTransactionSummarySheet(ExcelPackage package, List<Data_Transaksi> transactions, DateTime startDate, DateTime endDate)
        {
            var worksheet = package.Workbook.Worksheets.Add("Transaction Summary");

            // Header
            worksheet.Cells["A1:G1"].Merge = true;
            worksheet.Cells["A1"].Value = "LAPORAN TRANSAKSI KEDAI RIZO";
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            worksheet.Cells["A2:G2"].Merge = true;
            worksheet.Cells["A2"].Value = $"Period: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}";
            worksheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Column headers
            worksheet.Cells["A4"].Value = "Transaction ID";
            worksheet.Cells["B4"].Value = "Date";
            worksheet.Cells["C4"].Value = "User";
            worksheet.Cells["D4"].Value = "Items Count";
            worksheet.Cells["E4"].Value = "Total Qty";
            worksheet.Cells["F4"].Value = "Total Amount";
            worksheet.Cells["G4"].Value = "Status";

            // Style headers
            using (var range = worksheet.Cells["A4:G4"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // Data rows
            int row = 5;
            foreach (var transaction in transactions)
            {
                worksheet.Cells[row, 1].Value = transaction.Id;
                worksheet.Cells[row, 2].Value = transaction.Tanggal.ToString("dd/MM/yyyy HH:mm");
                worksheet.Cells[row, 3].Value = transaction.ApplicationUser?.UserName ?? "Unknown";
                worksheet.Cells[row, 4].Value = transaction.Details?.Count ?? 0;
                worksheet.Cells[row, 5].Value = transaction.Details?.Sum(d => d.Quantity) ?? 0;
                worksheet.Cells[row, 6].Value = transaction.TotalAmount;
                worksheet.Cells[row, 6].Style.Numberformat.Format = "#,##0";
                worksheet.Cells[row, 7].Value = "Completed";

                row++;
            }

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        private void CreateTransactionDetailsSheet(ExcelPackage package, List<Data_Transaksi> transactions)
        {
            var worksheet = package.Workbook.Worksheets.Add("Transaction Details");

            // Header
            worksheet.Cells["A1:H1"].Merge = true;
            worksheet.Cells["A1"].Value = "DETAIL TRANSAKSI";
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Column headers
            worksheet.Cells["A3"].Value = "Transaction ID";
            worksheet.Cells["B3"].Value = "Date";
            worksheet.Cells["C3"].Value = "User";
            worksheet.Cells["D3"].Value = "Product Name";
            worksheet.Cells["E3"].Value = "Quantity";
            worksheet.Cells["F3"].Value = "Unit Price";
            worksheet.Cells["G3"].Value = "Subtotal";
            worksheet.Cells["H3"].Value = "Product Category";

            // Style headers
            using (var range = worksheet.Cells["A3:H3"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // Data rows
            int row = 4;
            foreach (var transaction in transactions)
            {
                foreach (var detail in transaction.Details ?? new List<Detail_Transaksi>())
                {
                    worksheet.Cells[row, 1].Value = transaction.Id;
                    worksheet.Cells[row, 2].Value = transaction.Tanggal.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cells[row, 3].Value = transaction.ApplicationUser?.UserName ?? "Unknown";
                    worksheet.Cells[row, 4].Value = detail.Product?.Product_Name ?? "Unknown Product";
                    worksheet.Cells[row, 5].Value = detail.Quantity;
                    worksheet.Cells[row, 6].Value = detail.Product?.harga ?? 0;
                    worksheet.Cells[row, 6].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[row, 7].Value = detail.total;
                    worksheet.Cells[row, 7].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[row, 8].Value = detail.Product?.Kategori.Nama_Kategori ?? "Unknown";

                    row++;
                }
            }

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        private void CreateProductListSheet(ExcelPackage package, List<Product> products)
        {
            var worksheet = package.Workbook.Worksheets.Add("Product List");

            // Header
            worksheet.Cells["A1:F1"].Merge = true;
            worksheet.Cells["A1"].Value = "DAFTAR PRODUK";
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Column headers
            worksheet.Cells["A3"].Value = "Product ID";
            worksheet.Cells["B3"].Value = "Product Name";
            worksheet.Cells["C3"].Value = "Category";
            worksheet.Cells["D3"].Value = "Price";
            worksheet.Cells["E3"].Value = "Stock";
            worksheet.Cells["F3"].Value = "Status";

            // Style headers
            using (var range = worksheet.Cells["A3:F3"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // Data rows
            int row = 4;
            foreach (var product in products)
            {
                worksheet.Cells[row, 1].Value = product.Id;
                worksheet.Cells[row, 2].Value = product.Product_Name;
                worksheet.Cells[row, 3].Value = product.Kategori?.Nama_Kategori ?? "Unknown";
                worksheet.Cells[row, 4].Value = product.harga;
                worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0";
                worksheet.Cells[row, 5].Value = product.stock;
                worksheet.Cells[row, 6].Value = product.stock > 0 ? "In Stock" : "Out of Stock";

                // Color code based on stock
                if (product.stock <= 5)
                {
                    worksheet.Cells[row, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[row, 5].Style.Fill.BackgroundColor.SetColor(Color.LightCoral);
                }
                else if (product.stock <= 10)
                {
                    worksheet.Cells[row, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[row, 5].Style.Fill.BackgroundColor.SetColor(Color.LightGoldenrodYellow);
                }

                row++;
            }

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        private void CreateStatisticsSheet(ExcelPackage package, List<Data_Transaksi> transactions, List<Product> products)
        {
            var worksheet = package.Workbook.Worksheets.Add("Statistics");

            // Header
            worksheet.Cells["A1:D1"].Merge = true;
            worksheet.Cells["A1"].Value = "STATISTIK & RINGKASAN";
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            int row = 3;

            // Transaction Statistics
            worksheet.Cells[row, 1].Value = "TRANSACTION STATISTICS";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            row++;

            worksheet.Cells[row, 1].Value = "Total Transactions:";
            worksheet.Cells[row, 2].Value = transactions.Count;
            row++;

            worksheet.Cells[row, 1].Value = "Total Revenue:";
            worksheet.Cells[row, 2].Value = transactions.Sum(t => t.TotalAmount);
            worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";
            row++;

            worksheet.Cells[row, 1].Value = "Average Transaction Value:";
            worksheet.Cells[row, 2].Value = transactions.Count > 0 ? transactions.Average(t => t.TotalAmount) : 0;
            worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";
            row++;

            worksheet.Cells[row, 1].Value = "Total Items Sold:";
            worksheet.Cells[row, 2].Value = transactions.SelectMany(t => t.Details ?? new List<Detail_Transaksi>()).Sum(d => d.Quantity);
            row += 2;

            // Product Statistics
            worksheet.Cells[row, 1].Value = "PRODUCT STATISTICS";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            row++;

            worksheet.Cells[row, 1].Value = "Total Products:";
            worksheet.Cells[row, 2].Value = products.Count;
            row++;

            worksheet.Cells[row, 1].Value = "Products in Stock:";
            worksheet.Cells[row, 2].Value = products.Count(p => p.stock > 0);
            row++;

            worksheet.Cells[row, 1].Value = "Products Out of Stock:";
            worksheet.Cells[row, 2].Value = products.Count(p => p.stock == 0);
            row++;

            worksheet.Cells[row, 1].Value = "Low Stock Products (≤5):";
            worksheet.Cells[row, 2].Value = products.Count(p => p.stock <= 5 && p.stock > 0);
            row++;

            worksheet.Cells[row, 1].Value = "Total Stock Value:";
            worksheet.Cells[row, 2].Value = products.Sum(p => p.stock * p.harga);
            worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";
            row += 2;

            // Top Products
            var topProducts = transactions
                .SelectMany(t => t.Details ?? new List<Detail_Transaksi>())
                .GroupBy(d => d.Product?.Product_Name ?? "Unknown")
                .Select(g => new { ProductName = g.Key, TotalSold = g.Sum(d => d.Quantity) })
                .OrderByDescending(p => p.TotalSold)
                .Take(5)
                .ToList();

            worksheet.Cells[row, 1].Value = "TOP 5 BEST SELLING PRODUCTS";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            row++;

            worksheet.Cells[row, 1].Value = "Product Name";
            worksheet.Cells[row, 2].Value = "Total Sold";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 2].Style.Font.Bold = true;
            row++;

            foreach (var product in topProducts)
            {
                worksheet.Cells[row, 1].Value = product.ProductName;
                worksheet.Cells[row, 2].Value = product.TotalSold;
                row++;
            }

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        private bool Data_TransaksiExists(int id)
        {
            return _context.Data_Transaksi.Any(e => e.Id == id);
        }
        /*
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var data_Transaksi = await _context.Data_Transaksi
                    .Include(d => d.Details)
                    .ThenInclude(dt => dt.Product)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (data_Transaksi != null)
                {
                    // Restore stock for each product
                    foreach (var detail in data_Transaksi.Details)
                    {
                        if (detail.Product != null)
                        {
                            detail.Product.stock += detail.Quantity;
                        }
                    }

                    _context.Data_Transaksi.Remove(data_Transaksi);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }
        */


        [HttpGet]
        public async Task<IActionResult> GetProductPrice(int productId)
        {
            var product = await _context.Product.FindAsync(productId);
            if (product == null)
            {
                return Json(new { price = 0, stock = 0 });
            }
            return Json(new { price = product.harga, stock = product.stock });
        }
    }

    // ViewModel for transaction creation
    public class TransactionViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public DateTime Tanggal { get; set; } = DateTime.Now;
        public List<TransactionItemViewModel> Items { get; set; } = new List<TransactionItemViewModel>();

        public TransactionViewModel()
        {
            // Initialize with 5 empty items
            for (int i = 0; i < 5; i++)
            {
                Items.Add(new TransactionItemViewModel());
            }
        }
    }

    public class TransactionItemViewModel
    {
        [Required(ErrorMessage = "Produk wajib dipilih")]
        public int ProductId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Quantity minimal 1")]

        public int Quantity { get; set; }
    }
}

/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project_Inventory_KedaiRIZO_Alpha_v0._1.Data;
using Project_Inventory_KedaiRIZO_Alpha_v0._1.Models;

namespace Project_Inventory_KedaiRIZO_Alpha_v0._1.Controllers
{
    public class Data_TransaksiController : Controller
    {
        private readonly ApplicationDbContext _context;

        public Data_TransaksiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Data_Transaksi
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Data_Transaksi.Include(d => d.ApplicationUser);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Data_Transaksi/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var data_Transaksi = await _context.Data_Transaksi
                .Include(d => d.ApplicationUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (data_Transaksi == null)
            {
                return NotFound();
            }

            return View(data_Transaksi);
        }

        // GET: Data_Transaksi/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: Data_Transaksi/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserId,Tanggal,TotalAmount")] Data_Transaksi data_Transaksi)
        {
            if (!ModelState.IsValid)
            {
                foreach (var modelError in ModelState)
                {
                    foreach (var error in modelError.Value.Errors)
                    {
                        // Set a breakpoint here or log the error
                        var errorMessage = error.ErrorMessage;
                        Console.WriteLine(errorMessage);
                    }
                }
            }
            if (ModelState.IsValid)
            {
                _context.Add(data_Transaksi);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", data_Transaksi.UserId);
            return View(data_Transaksi);
        }

        // GET: Data_Transaksi/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var data_Transaksi = await _context.Data_Transaksi.FindAsync(id);
            if (data_Transaksi == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", data_Transaksi.UserId);
            return View(data_Transaksi);
        }

        // POST: Data_Transaksi/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,Tanggal,TotalAmount")] Data_Transaksi data_Transaksi)
        {
            if (id != data_Transaksi.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(data_Transaksi);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!Data_TransaksiExists(data_Transaksi.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", data_Transaksi.UserId);
            return View(data_Transaksi);
        }

        // GET: Data_Transaksi/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var data_Transaksi = await _context.Data_Transaksi
                .Include(d => d.ApplicationUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (data_Transaksi == null)
            {
                return NotFound();
            }

            return View(data_Transaksi);
        }

        // POST: Data_Transaksi/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var data_Transaksi = await _context.Data_Transaksi.FindAsync(id);
            if (data_Transaksi != null)
            {
                _context.Data_Transaksi.Remove(data_Transaksi);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool Data_TransaksiExists(int id)
        {
            return _context.Data_Transaksi.Any(e => e.Id == id);
        }
    }
}
*/