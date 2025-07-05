using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using OfficeOpenXml;
using Project_Inventory_KedaiRIZO_Alpha_v0._1.Data;
using Project_Inventory_KedaiRIZO_Alpha_v0._1.Models;
using CsvHelper;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Project_Inventory_KedaiRIZO_Alpha_v0._1.Controllers
{
    [Authorize]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }
        private async Task<bool> ProductNameExists(string productName, int? excludeId = null)
        {
            return await _context.Product
                .AnyAsync(p => p.Product_Name.ToLower() == productName.ToLower() &&
                              (excludeId == null || p.Id != excludeId));
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            // var applicationDbContext = _context.Product.Include(p => p.Kategori);
            var applicationDbContext = _context.Product.Include(p => p.Kategori);
            ViewData["KategoriID"] = new SelectList(_context.Kategori, "Id", "Nama_Kategori");
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Product
                .Include(p => p.Kategori)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            ViewData["KategoriID"] = new SelectList(_context.Kategori, "Id", "Nama_Kategori");
            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Product_Name,harga,stock,KategoriID")] Product product)
        {
            Console.WriteLine($"KategoriID: {product.KategoriID}");
            if (await ProductNameExists(product.Product_Name))
            {
                ModelState.AddModelError("Product_Name", "A product with this name already exists.");
            }
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
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["KategoriID"] = new SelectList(_context.Kategori, "Id", "Nama_Kategori");
            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Product.FindAsync(id);
            if (await ProductNameExists(product.Product_Name, id))
            {
                ModelState.AddModelError("Product_Name", "A product with this name already exists.");
            }
            if (product == null)
            {
                return NotFound();
            }
            ViewData["KategoriID"] = new SelectList(_context.Kategori, "Id", "Nama_Kategori");
            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Product_Name,harga,stock,KategoriID")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
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
            ViewData["KategoriID"] = new SelectList(_context.Kategori, "Id", "Nama_Kategori");
            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Product
                .Include(p => p.Kategori)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Product.FindAsync(id);
            if (product != null)
            {
                _context.Product.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Import
        public IActionResult Import()
        {
            var kategoriOptions = _context.Kategori.Select(k => new { k.Id, k.Nama_Kategori }).ToList();
            ViewData["KategoriOptions"] = kategoriOptions;

            // Log tiap item ke output
            foreach (var item in kategoriOptions)
            {
                Console.WriteLine($"ID: {item.Id}, Nama: {item.Nama_Kategori}");
            }
            return View();
        }

        // POST: Products/Import
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a file to import.";
                return RedirectToAction(nameof(Import));
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            var products = new List<Product>();
            var categories = await _context.Kategori.ToDictionaryAsync(k => k.Nama_Kategori.ToLower(), k => k.Id);
            var errors = new List<string>();

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    if (fileExtension == ".csv")
                    {
                        products = await ParseCsvFile(stream, categories, errors);
                    }
                    else if (fileExtension == ".xlsx" || fileExtension == ".xls")
                    {
                        products = await ParseExcelFile(stream, categories, errors);
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Unsupported file format. Please upload a CSV or Excel file.";
                        return RedirectToAction(nameof(Import));
                    }
                }

                if (errors.Any())
                {
                    TempData["ErrorMessage"] = $"Import completed with {errors.Count} errors: {string.Join(", ", errors.Take(5))}";
                }

                if (products.Any())
                {
                    _context.Product.AddRange(products);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Successfully imported {products.Count} products.";
                }
                else
                {
                    TempData["ErrorMessage"] = "No valid products found in the file.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error importing file: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // Export to CSV
        public async Task<IActionResult> ExportCsv()
        {
            var products = await _context.Product.Include(p => p.Kategori).ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Product_Name,Price,Stock,Category");

            foreach (var product in products)
            {
                csv.AppendLine($"{product.Product_Name},{product.harga},{product.stock},{product.Kategori?.Nama_Kategori ?? ""}");
            }

            var fileName = $"products_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var bytes = Encoding.UTF8.GetBytes(csv.ToString());

            return File(bytes, "text/csv", fileName);
        }

        // Export to Excel
        public async Task<IActionResult> ExportExcel()
        {
            var products = await _context.Product.Include(p => p.Kategori).ToListAsync();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Products");

                // Headers
                worksheet.Cells[1, 1].Value = "Product Name";
                worksheet.Cells[1, 2].Value = "Price";
                worksheet.Cells[1, 3].Value = "Stock";
                worksheet.Cells[1, 4].Value = "Category";

                // Style headers
                using (var range = worksheet.Cells[1, 1, 1, 4])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // Data
                for (int i = 0; i < products.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = products[i].Product_Name;
                    worksheet.Cells[i + 2, 2].Value = products[i].harga;
                    worksheet.Cells[i + 2, 3].Value = products[i].stock;
                    worksheet.Cells[i + 2, 4].Value = products[i].Kategori?.Nama_Kategori ?? "";
                }

                worksheet.Cells.AutoFitColumns();

                var fileName = $"products_export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var bytes = package.GetAsByteArray();

                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        // Helper method to parse CSV file
        private async Task<List<Product>> ParseCsvFile(Stream stream, Dictionary<string, int> categories, List<string> errors)
        {
            var products = new List<Product>();
            var processedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (var reader = new StreamReader(stream))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                var records = csv.GetRecords<dynamic>().ToList();

                for (int i = 0; i < records.Count; i++)
                {
                    var record = records[i] as IDictionary<string, object>;
                    var rowNumber = i + 2; // +2 for header row and 1-based indexing

                    try
                    {
                        var product = new Product();

                        // Product Name
                        if (record.ContainsKey("Product_Name") && record["Product_Name"] != null)
                        {                            
                            var productName = record["Product_Name"].ToString();

                            // Check if name already exists in database
                            if (await ProductNameExists(productName))
                            {
                                errors.Add($"Row {rowNumber}: Product '{productName}' already exists in database");
                                continue;
                            }

                            // Check if name is duplicate within the same import file
                            if (processedNames.Contains(productName))
                            {
                                errors.Add($"Row {rowNumber}: Duplicate product name '{productName}' found in import file");
                                continue;
                            }

                            product.Product_Name = productName;
                            processedNames.Add(productName);

                        }
                        else
                        {
                            errors.Add($"Row {rowNumber}: Product name is required");
                            continue;
                        }

                        // Price
                        if (record.ContainsKey("Price")
                            && record["Price"] != null
                            && !string.IsNullOrWhiteSpace(record["Price"].ToString())
                            && decimal.TryParse(record["Price"].ToString(), out var price))
                        {
                            product.harga = (int)price;
                        }
                        else
                        {
                            errors.Add($"Row {rowNumber}: Invalid or missing price");
                            continue;
                        }

                        // Stock
                        if (record.ContainsKey("Stock") && int.TryParse(record["Stock"].ToString(), out var stock))
                        {
                            product.stock = stock;
                        }
                        else
                        {
                            errors.Add($"Row {rowNumber}: Invalid stock format");
                            continue;
                        }

                        // Category
                        if (record.ContainsKey("Category") && record["Category"] != null)
                        {
                            var categoryName = record["Category"].ToString().ToLower();
                            if (categories.TryGetValue(categoryName, out var categoryId))
                            {
                                product.KategoriID = categoryId;
                            }
                            else
                            {
                                // Create new category if it doesn't exist
                                var newCategory = new Kategori { Nama_Kategori = record["Category"].ToString() };
                                _context.Kategori.Add(newCategory);
                                await _context.SaveChangesAsync();

                                // Add the new category to the dictionary for future use
                                categories[newCategory.Nama_Kategori.ToLower()] = newCategory.Id;
                                product.KategoriID = newCategory.Id;

                                // Log that a new category was created (optional)
                                 Console.WriteLine($"New category '{newCategory.Nama_Kategori}' created with ID: {newCategory.Id}");
                            }
                        }
                        else
                        {
                            errors.Add($"Row {rowNumber}: Category is required");
                            continue;
                        }

                        products.Add(product);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {rowNumber}: {ex.Message}");
                    }
                }
            }

            return products;
        }

        // Helper method to parse Excel file
        private async Task<List<Product>> ParseExcelFile(Stream stream, Dictionary<string, int> categories, List<string> errors)
        {
            var products = new List<Product>();

            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++) // Start from row 2 (skip header)
                {
                    try
                    {
                        var product = new Product();

                        // Product Name
                        var productName = worksheet.Cells[row, 1].Value?.ToString();
                        if (string.IsNullOrWhiteSpace(productName))
                        {
                            errors.Add($"Row {row}: Product name is required");
                            continue;
                        }
                        product.Product_Name = productName;

                        // Price
                        if (decimal.TryParse(worksheet.Cells[row, 2].Value?.ToString(), out var price))
                        {
                            product.harga = (int)price;
                        }
                        else
                        {
                            errors.Add($"Row {row}: Invalid price format");
                            continue;
                        }

                        // Stock
                        if (int.TryParse(worksheet.Cells[row, 3].Value?.ToString(), out var stock))
                        {
                            product.stock = stock;
                        }
                        else
                        {
                            errors.Add($"Row {row}: Invalid stock format");
                            continue;
                        }

                        // Category
                        var categoryName = worksheet.Cells[row, 4].Value?.ToString()?.ToLower();
                        if (string.IsNullOrWhiteSpace(categoryName))
                        {
                            errors.Add($"Row {row}: Category is required");
                            continue;
                        }

                        if (categories.TryGetValue(categoryName, out var categoryId))
                        {
                            product.KategoriID = categoryId;
                        }
                        else
                        {
                            errors.Add($"Row {row}: Category '{categoryName}' not found");
                            continue;
                        }

                        products.Add(product);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {row}: {ex.Message}");
                    }
                }
            }

            return products;
        }

        private bool ProductExists(int id)
        {
            return _context.Product.Any(e => e.Id == id);
        }

    }
}
