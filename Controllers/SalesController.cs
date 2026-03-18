using Executive_Fuentes.Data;
using Executive_Fuentes.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "SuperAdmin,Executive,HeadManager")]
public class SalesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _auditService;

    public SalesController(ApplicationDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // ===============================
    // SALES LIST
    // ===============================
    public async Task<IActionResult> Index()
    {
        var sales = await _context.Sales
            .Include(s => s.Product)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();

        return View(sales);
    }

    // ===============================
    // CREATE SALE (GET)
    // ===============================
    public async Task<IActionResult> Create()
    {
        ViewBag.Products = await _context.Products
            .Where(p => p.Status == "Active")
            .ToListAsync();

        ViewData["Title"] = "Create Sale";

        return View();
    }

    // ===============================
    // CREATE SALE (POST)
    // ===============================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Sale sale)
    {
        var product = await _context.Products.FindAsync(sale.ProductId);

        if (product == null || product.Status != "Active")
            ModelState.AddModelError("", "Product not available");

        if (sale.QuantitySold <= 0)
            ModelState.AddModelError("QuantitySold", "Quantity must be greater than 0");

        if (product != null && sale.QuantitySold > product.StockQuantity)
            ModelState.AddModelError("", "Insufficient stock");

        if (!ModelState.IsValid)
        {
            ViewBag.Products = await _context.Products
                .Where(p => p.Status == "Active")
                .ToListAsync();

            return View(sale);
        }

        // Calculate total
        sale.TotalAmount = sale.QuantitySold * product.Price;

        // Deduct stock
        product.StockQuantity -= sale.QuantitySold;

        _context.Update(product);
        _context.Sales.Add(sale);

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Create Sale",
            "Sales",
            $"Sold {sale.QuantitySold} of '{product.ProductName}' | Total: ₱{sale.TotalAmount}"
        );

        return RedirectToAction(nameof(Index));
    }

    // ===============================
    // EDIT SALE (GET)
    // ===============================
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var sale = await _context.Sales.FindAsync(id);

        if (sale == null)
            return NotFound();

        ViewBag.Products = await _context.Products
            .Where(p => p.Status == "Active")
            .ToListAsync();

        ViewData["Title"] = "Edit Sale";

        return View(sale);
    }

    // ===============================
    // EDIT SALE (POST)
    // ===============================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Sale sale)
    {
        if (id != sale.Id)
            return NotFound();

        var existingSale = await _context.Sales
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (existingSale == null)
            return NotFound();

        var product = await _context.Products.FindAsync(sale.ProductId);

        if (product == null)
            ModelState.AddModelError("", "Product not found");

        if (sale.QuantitySold <= 0)
            ModelState.AddModelError("QuantitySold", "Quantity must be greater than 0");

        if (!ModelState.IsValid)
        {
            ViewBag.Products = await _context.Products
                .Where(p => p.Status == "Active")
                .ToListAsync();

            return View(sale);
        }

        // Restore previous stock
        product.StockQuantity += existingSale.QuantitySold;

        // Check stock again
        if (sale.QuantitySold > product.StockQuantity)
        {
            ModelState.AddModelError("", "Insufficient stock");

            ViewBag.Products = await _context.Products
                .Where(p => p.Status == "Active")
                .ToListAsync();

            return View(sale);
        }

        // Deduct new quantity
        product.StockQuantity -= sale.QuantitySold;

        // Recalculate total
        sale.TotalAmount = sale.QuantitySold * product.Price;

        _context.Update(product);
        _context.Update(sale);

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Edit Sale",
            "Sales",
            $"Updated sale | Product: {product.ProductName} | Qty: {sale.QuantitySold} | Total: ₱{sale.TotalAmount}"
        );

        return RedirectToAction(nameof(Index));
    }

    // ===============================
    // DELETE SALE (GET)
    // ===============================
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var sale = await _context.Sales
            .Include(s => s.Product)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (sale == null)
            return NotFound();

        return View(sale);
    }

    // ===============================
    // DELETE SALE (POST)
    // ===============================
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var sale = await _context.Sales.FindAsync(id);

        if (sale == null)
            return NotFound();

        var product = await _context.Products.FindAsync(sale.ProductId);

        if (product != null)
        {
            // Restore stock
            product.StockQuantity += sale.QuantitySold;
            _context.Update(product);
        }

        _context.Sales.Remove(sale);

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Delete Sale",
            "Sales",
            $"Deleted sale | Product: {product?.ProductName} | Qty: {sale.QuantitySold}"
        );

        return RedirectToAction(nameof(Index));
    }
}