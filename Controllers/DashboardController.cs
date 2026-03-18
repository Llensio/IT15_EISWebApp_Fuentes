using Executive_Fuentes.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Executive_Fuentes.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? fromMonth, int? toMonth)
        {
            int currentYear = DateTime.Now.Year;

            // Default range
            fromMonth ??= 1;
            toMonth ??= 12;

            // Active products
            var activeProducts = await _context.Products
                .CountAsync(p => !p.IsArchived);

            // Sales by product
            var salesByProduct = await _context.Sales
                .Include(s => s.Product)
                .GroupBy(s => s.Product.ProductName)
                .Select(g => new SalesByProductVM
                {
                    ProductName = g.Key,
                    TotalQuantity = g.Sum(x => x.QuantitySold),
                    TotalRevenue = g.Sum(x => x.TotalAmount)
                })
                .ToListAsync();

            // Top product
            var topProduct = salesByProduct
                .OrderByDescending(p => p.TotalRevenue)
                .FirstOrDefault()?.ProductName ?? "N/A";

            var totalRevenue = salesByProduct.Sum(p => p.TotalRevenue);
            var totalUnitsSold = salesByProduct.Sum(p => p.TotalQuantity);

            // FILTERED monthly data
            var monthlyDataRaw = await _context.Sales
                .Where(s =>
                    s.SaleDate.Year == currentYear &&
                    s.SaleDate.Month >= fromMonth &&
                    s.SaleDate.Month <= toMonth)
                .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
                .Select(g => new
                {
                    Month = g.Key.Month,
                    Revenue = g.Sum(x => x.TotalAmount)
                })
                .ToListAsync();

            var monthlyTrends = monthlyDataRaw
                .OrderBy(x => x.Month)
                .Select(x => new MonthlyTrendVM
                {
                    Month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(x.Month),
                    Revenue = x.Revenue
                })
                .ToList();

            var vm = new SalesDashboardVM
            {
                ActiveProducts = activeProducts,
                SalesByProduct = salesByProduct,
                MonthlyTrends = monthlyTrends,
                TotalRevenue = totalRevenue,
                TotalUnitsSold = totalUnitsSold,
                TopProduct = topProduct,

                FromMonth = fromMonth.Value,
                ToMonth = toMonth.Value
            };

            return View(vm);
        }

        [Authorize(Roles = "SuperAdmin,Executive,HeadManager,AuthorizedUser")]
        public IActionResult AdminDashboard()
        {
            return RedirectToAction(nameof(Index));
        }
    }
}