public class SalesDashboardVM
{
    public int ActiveProducts { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalUnitsSold { get; set; }
    public string TopProduct { get; set; }

    public List<SalesByProductVM> SalesByProduct { get; set; }
    public List<MonthlyTrendVM> MonthlyTrends { get; set; }

    public int FromMonth { get; set; }
    public int ToMonth { get; set; }
}

    public class SalesByProductVM
{
    public string ProductName { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class MonthlyTrendVM
{
    public string Month { get; set; }
    public decimal Revenue { get; set; }
}