using System;
using System.ComponentModel.DataAnnotations;

public class Expense
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Description { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public DateTime ExpenseDate { get; set; } = DateTime.Now;

    // New property for archiving
    public bool IsArchived { get; set; } = false;
}