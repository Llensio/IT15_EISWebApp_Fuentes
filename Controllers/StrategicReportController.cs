using Executive_Fuentes.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

[Authorize(Roles = "SuperAdmin,Executive,HeadManager,AuthorizedUser")]
public class StrategicReportController : Controller
{
    private readonly ApplicationDbContext _context;

    public StrategicReportController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var totalRevenue = await _context.Sales.SumAsync(s => (decimal?)s.TotalAmount) ?? 0;
        var totalQuantity = await _context.Sales.SumAsync(s => (int?)s.QuantitySold) ?? 0;
        var totalProducts = await _context.Products.CountAsync(p => !p.IsArchived);
        var archivedProducts = await _context.Products.CountAsync(p => p.IsArchived);

        var productPerformance = await _context.Sales
            .Include(s => s.Product)
            .GroupBy(s => s.Product.ProductName)
            .Select(g => new ProductPerformanceVM
            {
                ProductName = g.Key,
                QuantitySold = g.Sum(x => x.QuantitySold),
                Revenue = g.Sum(x => x.TotalAmount)
            })
            .OrderByDescending(x => x.Revenue)
            .ToListAsync();

        var topProducts = productPerformance.Take(5).ToList();
        var lowProducts = productPerformance.OrderBy(x => x.Revenue).Take(5).ToList();

        var monthlyRaw = await _context.Sales
            .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Revenue = g.Sum(x => x.TotalAmount)
            })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToListAsync();

        var monthlyTrend = monthlyRaw.Select(x => new MonthlyTrendVM    
        {
            Month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(x.Month) + " " + x.Year,
            Revenue = x.Revenue
        }).ToList();

        decimal growthRate = 0;
        if (monthlyTrend.Count >= 2)
        {
            var last = monthlyTrend.Last().Revenue;
            var previous = monthlyTrend[monthlyTrend.Count - 2].Revenue;

            if (previous != 0)
                growthRate = ((last - previous) / previous) * 100;
        }

        var vm = new StrategicReportVM
        {
            TotalRevenue = totalRevenue,
            TotalQuantitySold = totalQuantity,
            TotalProducts = totalProducts,
            ArchivedProducts = archivedProducts,
            TopProducts = topProducts,
            LowProducts = lowProducts,
            MonthlyRevenue = monthlyTrend,
            GrowthRate = growthRate
        };

        return View(vm);
    }
}