public class FinanceSummary
{
    // Actual Data
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }

    // Computed
    public decimal NetProfit => TotalRevenue - TotalExpenses;

    // Budget
    public decimal BudgetAmount { get; set; }
    public decimal BudgetRemaining => BudgetAmount - TotalExpenses;
    public bool IsOverBudget => TotalExpenses > BudgetAmount;

    // Forecast
    public decimal ForecastRevenue { get; set; }
    public decimal ForecastExpenses { get; set; }
    public decimal ForecastProfit => ForecastRevenue - ForecastExpenses;
}