using Executive_Fuentes.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

[Authorize(Roles = "SuperAdmin,Executive,HeadManager,AuthorizedUser")]
public class SalesMarketingController : Controller
{
    private readonly ApplicationDbContext _context;

    public SalesMarketingController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
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

        var monthlyDataRaw = await _context.Sales
            .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Revenue = g.Sum(x => x.TotalAmount)
            })
            .ToListAsync(); 

        var monthlyTrends = monthlyDataRaw
            .Select(x => new MonthlyTrendVM
            {
                Month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(x.Month) + " " + x.Year,
                Revenue = x.Revenue
            })
            .ToList();

        var vm = new SalesDashboardVM
        {
            SalesByProduct = salesByProduct,
            MonthlyTrends = monthlyTrends
        };

        return View(vm);
    }
    public async Task<IActionResult> ExportReport()
    {
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

        var monthlyDataRaw = await _context.Sales
            .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Revenue = g.Sum(x => x.TotalAmount)
            })
            .ToListAsync();

        var monthlyTrends = monthlyDataRaw
            .Select(x => new MonthlyTrendVM
            {
                Month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(x.Month) + " " + x.Year,
                Revenue = x.Revenue
            })
            .ToList();

        QuestPDF.Settings.License = LicenseType.Community;

        var stream = new MemoryStream();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Header()
                    .Text("Sales & Marketing Report")
                    .FontSize(20)
                    .Bold();

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Text($"Generated: {DateTime.Now:MMMM dd, yyyy}")
                        .FontSize(12);

                    col.Item().Text("Product Performance")
                        .FontSize(16)
                        .Bold();

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(120);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Product").Bold();
                            header.Cell().Text("Quantity").Bold();
                            header.Cell().Text("Revenue").Bold();
                        });

                        foreach (var item in salesByProduct)
                        {
                            table.Cell().Text(item.ProductName);
                            table.Cell().Text(item.TotalQuantity.ToString());
                            table.Cell().Text("₱ " + item.TotalRevenue.ToString("N2"));
                        }
                    });

                    col.Item().PaddingTop(20).Text("Monthly Revenue")
                        .FontSize(16)
                        .Bold();

                    foreach (var month in monthlyTrends)
                    {
                        col.Item().Text($"{month.Month} - ₱ {month.Revenue:N2}");
                    }
                });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
            });
        })
        .GeneratePdf(stream);

        return File(stream.ToArray(),
            "application/pdf",
            $"SalesReport_{DateTime.Now:yyyyMMdd}.pdf");
    }
}