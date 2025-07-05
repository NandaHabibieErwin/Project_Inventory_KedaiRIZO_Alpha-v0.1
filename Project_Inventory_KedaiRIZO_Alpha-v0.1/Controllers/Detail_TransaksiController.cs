using System;
using System.Collections.Generic;
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
    public class Detail_TransaksiController : Controller
    {
        private readonly ApplicationDbContext _context;

        public Detail_TransaksiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Detail_Transaksi
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Detail_Transaksi.Include(d => d.DataTransaksi).Include(d => d.Product);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Detail_Transaksi/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var detail_Transaksi = await _context.Detail_Transaksi
                .Include(d => d.DataTransaksi)
                .Include(d => d.Product)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (detail_Transaksi == null)
            {
                return NotFound();
            }

            return View(detail_Transaksi);
        }

        // GET: Detail_Transaksi/Create
        public IActionResult Create()
        {
            ViewData["DataTransaksiId"] = new SelectList(_context.Data_Transaksi, "Id", "Id");
            ViewData["ProductId"] = new SelectList(_context.Product, "Id", "Id");
            return View();
        }

        // POST: Detail_Transaksi/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,DataTransaksiId,ProductId,Quantity,total")] Detail_Transaksi detail_Transaksi)
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
            var product = await _context.Product.FindAsync(detail_Transaksi.ProductId);
            if (detail_Transaksi.Quantity > product.stock)
            {
                ModelState.AddModelError("Quantity", "Stock tidak cukup.");
            }
            if (ModelState.IsValid)
            { 
                product.stock -= detail_Transaksi.Quantity;
               
                _context.Add(detail_Transaksi);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DataTransaksiId"] = new SelectList(_context.Data_Transaksi, "Id", "Id", detail_Transaksi.DataTransaksiId);
            ViewData["ProductId"] = new SelectList(_context.Product, "Id", "Id", detail_Transaksi.ProductId);
            return View(detail_Transaksi);
        }

        // GET: Detail_Transaksi/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var detail_Transaksi = await _context.Detail_Transaksi.FindAsync(id);
            if (detail_Transaksi == null)
            {
                return NotFound();
            }
            ViewData["DataTransaksiId"] = new SelectList(_context.Data_Transaksi, "Id", "Id", detail_Transaksi.DataTransaksiId);
            ViewData["ProductId"] = new SelectList(_context.Product, "Id", "Id", detail_Transaksi.ProductId);
            return View(detail_Transaksi);
        }

        // POST: Detail_Transaksi/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DataTransaksiId,ProductId,Quantity,total")] Detail_Transaksi detail_Transaksi)
        {
            if (id != detail_Transaksi.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(detail_Transaksi);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!Detail_TransaksiExists(detail_Transaksi.Id))
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
            ViewData["DataTransaksiId"] = new SelectList(_context.Data_Transaksi, "Id", "Id", detail_Transaksi.DataTransaksiId);
            ViewData["ProductId"] = new SelectList(_context.Product, "Id", "Id", detail_Transaksi.ProductId);
            return View(detail_Transaksi);
        }

        // GET: Detail_Transaksi/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var detail_Transaksi = await _context.Detail_Transaksi
                .Include(d => d.DataTransaksi)
                .Include(d => d.Product)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (detail_Transaksi == null)
            {
                return NotFound();
            }

            return View(detail_Transaksi);
        }

        // POST: Detail_Transaksi/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var detail_Transaksi = await _context.Detail_Transaksi.FindAsync(id);
            if (detail_Transaksi != null)
            {
                _context.Detail_Transaksi.Remove(detail_Transaksi);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool Detail_TransaksiExists(int id)
        {
            return _context.Detail_Transaksi.Any(e => e.Id == id);
        }
    }
}
