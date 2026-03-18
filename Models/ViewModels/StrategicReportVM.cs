using System.Collections.Generic;

public class StrategicReportVM
{
    public decimal TotalRevenue { get; set; }
    public int TotalQuantitySold { get; set; }
    public int TotalProducts { get; set; }
    public int ArchivedProducts { get; set; }

    public List<ProductPerformanceVM> TopProducts { get; set; }
    public List<ProductPerformanceVM> LowProducts { get; set; }
    public List<MonthlyTrendVM> MonthlyRevenue { get; set; }

    public decimal GrowthRate { get; set; }
}

public class ProductPerformanceVM
{
    public string ProductName { get; set; }
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}