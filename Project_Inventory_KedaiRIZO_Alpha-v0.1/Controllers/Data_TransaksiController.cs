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
