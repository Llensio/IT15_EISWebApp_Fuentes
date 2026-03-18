using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Executive_Fuentes.Data;
using Executive_Fuentes.Models;

[Authorize(Roles = "SuperAdmin,Executive")]
public class FinanceController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _auditService;

    public FinanceController(ApplicationDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // ============================================================
    // DASHBOARD (Revenue / Expenses / Profit / Budget / Forecast)
    // ============================================================
    public async Task<IActionResult> Index()
    {
        // ===== ACTUAL TOTALS =====
        decimal totalRevenue = await _context.Sales.AnyAsync()
            ? await _context.Sales.SumAsync(s => s.TotalAmount)
            : 0;

        decimal totalExpenses = await _context.Expenses
            .Where(e => !e.IsArchived)
            .AnyAsync()
            ? await _context.Expenses
                .Where(e => !e.IsArchived)
                .SumAsync(e => e.Amount)
            : 0;

        // ===== ACTIVE BUDGET =====
        var activeBudget = await _context.Budgets
            .Where(b => b.IsActive)
            .OrderByDescending(b => b.StartDate)
            .FirstOrDefaultAsync();

        decimal budgetAmount = activeBudget?.Amount ?? 0;

        // ===== FORECAST (Last 3 Months Average) =====
        DateTime last3Months = DateTime.Now.AddMonths(-3);

        decimal revenueLast3Months = await _context.Sales
            .Where(s => s.SaleDate >= last3Months)
            .AnyAsync()
            ? await _context.Sales
                .Where(s => s.SaleDate >= last3Months)
                .SumAsync(s => s.TotalAmount)
            : 0;

        decimal expenseLast3Months = await _context.Expenses
            .Where(e => e.ExpenseDate >= last3Months && !e.IsArchived)
            .AnyAsync()
            ? await _context.Expenses
                .Where(e => e.ExpenseDate >= last3Months && !e.IsArchived)
                .SumAsync(e => e.Amount)
            : 0;

        decimal forecastRevenue = revenueLast3Months / 3;
        decimal forecastExpenses = expenseLast3Months / 3;

        var summary = new FinanceSummary
        {
            TotalRevenue = totalRevenue,
            TotalExpenses = totalExpenses,
            BudgetAmount = budgetAmount,
            ForecastRevenue = forecastRevenue,
            ForecastExpenses = forecastExpenses
        };

        return View(summary);
    }

    // ============================================================
    // BUDGET MANAGEMENT
    // ============================================================

    public async Task<IActionResult> Budgets()
    {
        var budgets = await _context.Budgets
            .OrderByDescending(b => b.StartDate)
            .ToListAsync();

        return View(budgets);
    }

    public IActionResult CreateBudget()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBudget(Budget budget)
    {
        if (!ModelState.IsValid)
            return View(budget);

        // Deactivate existing budgets
        var existingBudgets = await _context.Budgets.ToListAsync();
        foreach (var b in existingBudgets)
            b.IsActive = false;

        budget.IsActive = true;

        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Create",
            "Finance",
            $"Created new budget: {budget.Amount}"
        );

        return RedirectToAction(nameof(Budgets));
    }
    public async Task<IActionResult> ActivateBudget(int id)
    {
        // Find the budget to activate
        var budgetToActivate = await _context.Budgets.FindAsync(id);
        if (budgetToActivate == null) return NotFound();

        // Deactivate any currently active budget
        var activeBudget = await _context.Budgets.FirstOrDefaultAsync(b => b.IsActive);
        if (activeBudget != null && activeBudget.Id != id)
        {
            activeBudget.IsActive = false;
            _context.Update(activeBudget);
        }

        // Activate the selected budget
        budgetToActivate.IsActive = true;
        _context.Update(budgetToActivate);

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Activate Budget",
            "Finance",
            $"Activated budget ID: {budgetToActivate.Id} Amount: {budgetToActivate.Amount}"
        );

        return RedirectToAction(nameof(Budgets));
    }
    // GET: Finance/EditBudget/5
    public async Task<IActionResult> EditBudget(int id)
    {
        var budget = await _context.Budgets.FindAsync(id);
        if (budget == null) return NotFound();

        return View(budget);
    }

    // POST: Finance/EditBudget/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditBudget(int id, Budget budget)
    {
        if (id != budget.Id) return NotFound();
        if (!ModelState.IsValid) return View(budget);

        try
        {
            _context.Update(budget);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                "Edit Budget",
                "Finance",
                $"Edited budget ID: {budget.Id}, Amount: {budget.Amount}"
            );
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Budgets.Any(b => b.Id == budget.Id))
                return NotFound();
            else
                throw;
        }

        return RedirectToAction(nameof(Budgets));
    }
    // POST: Finance/DeactivateBudget/5
    public async Task<IActionResult> DeactivateBudget(int id)
    {
        var budget = await _context.Budgets.FindAsync(id);
        if (budget == null) return NotFound();

        // Deactivate only if it's active
        if (budget.IsActive)
        {
            budget.IsActive = false;
            _context.Update(budget);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                "Deactivate Budget",
                "Finance",
                $"Deactivated budget ID: {budget.Id}, Amount: {budget.Amount}"
            );
        }

        return RedirectToAction(nameof(EditBudget), new { id });
    }
    // ============================================================
    // EXPENSE MANAGEMENT
    // ============================================================

    public async Task<IActionResult> Expenses()
    {
        var expenses = await _context.Expenses
            .Where(e => !e.IsArchived)
            .OrderByDescending(e => e.ExpenseDate)
            .ToListAsync();

        return View(expenses);
    }

    public IActionResult CreateExpense()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateExpense(Expense expense)
    {
        if (!ModelState.IsValid)
            return View(expense);

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Create",
            "Finance",
            $"Created expense '{expense.Description}' Amount: {expense.Amount}"
        );

        return RedirectToAction(nameof(Expenses));
    }

    public async Task<IActionResult> EditExpense(int id)
    {
        var expense = await _context.Expenses.FindAsync(id);
        if (expense == null)
            return NotFound();

        return View(expense);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditExpense(int id, Expense expense)
    {
        if (id != expense.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View(expense);

        _context.Update(expense);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Edit",
            "Finance",
            $"Edited expense '{expense.Description}' (ID: {expense.Id})"
        );

        return RedirectToAction(nameof(Expenses));
    }

    // ============================================================
    // ARCHIVE (Soft Delete)
    // ============================================================

    public async Task<IActionResult> Archive(int id)
    {
        var expense = await _context.Expenses.FindAsync(id);
        if (expense == null)
            return NotFound();

        expense.IsArchived = true;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Archive",
            "Finance",
            $"Archived expense '{expense.Description}' (ID: {expense.Id})"
        );

        return RedirectToAction(nameof(Expenses));
    }
    public async Task<IActionResult> ArchivedExpenses()
    {
        // Get all archived expenses
        var archivedExpenses = await _context.Expenses
            .Where(e => e.IsArchived)
            .OrderByDescending(e => e.ExpenseDate)
            .ToListAsync();

        return View(archivedExpenses);
    }

    public async Task<IActionResult> Restore(int id)
    {
        var expense = await _context.Expenses.FindAsync(id);
        if (expense == null) return NotFound();

        expense.IsArchived = false;
        _context.Update(expense);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Restore",
            "Finance",
            $"Restored archived expense '{expense.Description}' (ID: {expense.Id})"
        );

        return RedirectToAction(nameof(ArchivedExpenses));
    }
    // ============================================================
    // PERMANENT DELETE
    // ============================================================

    public async Task<IActionResult> DeletePermanent(int id)
    {
        var expense = await _context.Expenses.FindAsync(id);
        if (expense == null)
            return NotFound();

        _context.Expenses.Remove(expense);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Delete Permanent",
            "Finance",
            $"Deleted expense '{expense.Description}' (ID: {expense.Id})"
        );

        return RedirectToAction(nameof(Expenses));
    }
}