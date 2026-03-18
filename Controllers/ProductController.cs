using Executive_Fuentes.Data;
using Executive_Fuentes.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "SuperAdmin,Executive,HeadManager")]
public class ProductController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _auditService;

    public ProductController(ApplicationDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // Active Products
    public async Task<IActionResult> Index()
    {
        var products = await _context.Products
            .Where(p => !p.IsArchived)
            .ToListAsync();

        return View(products);
    }

    // Archived Products
    public async Task<IActionResult> Archived()
    {
        var archivedProducts = await _context.Products
            .Where(p => p.IsArchived)
            .ToListAsync();

        return View(archivedProducts);
    }

    // Create
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product)
    {
        if (!ModelState.IsValid) return View(product);

        _context.Add(product);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Create",
            "Product",
            $"Created product '{product.ProductName}' with price {product.Price}"
        );

        return RedirectToAction(nameof(Index));
    }

    // Edit
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product product)
    {
        if (id != product.Id) return NotFound();
        if (!ModelState.IsValid) return View(product);

        _context.Update(product);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Edit",
            "Product",
            $"Edited product '{product.ProductName}' (ID: {product.Id})"
        );

        return RedirectToAction(nameof(Index));
    }

    // Archive
    public async Task<IActionResult> Archive(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        product.IsArchived = true;
        _context.Update(product);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Archive",
            "Product",
            $"Archived product '{product.ProductName}' (ID: {product.Id})"
        );

        return RedirectToAction(nameof(Index));
    }

    // Restore
    public async Task<IActionResult> Restore(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        product.IsArchived = false;
        _context.Update(product);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Restore",
            "Product",
            $"Restored product '{product.ProductName}' (ID: {product.Id})"
        );

        return RedirectToAction(nameof(Archived));
    }

    // Delete permanently
    public async Task<IActionResult> DeletePermanent(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Delete Permanent",
            "Product",
            $"Permanently deleted product '{product.ProductName}' (ID: {product.Id})"
        );

        return RedirectToAction(nameof(Archived));
    }

    // Update Stock
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStock(int id, int amount)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        product.StockQuantity += amount;

        _context.Update(product);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Update Stock",
            "Product",
            $"Updated stock for '{product.ProductName}'. Added {amount}. New Stock: {product.StockQuantity}"
        );

        return RedirectToAction(nameof(Index));
    }
}