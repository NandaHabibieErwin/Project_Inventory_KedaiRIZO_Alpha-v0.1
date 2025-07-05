using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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