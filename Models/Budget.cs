using System.ComponentModel.DataAnnotations;

public class Budget
{
    public int Id { get; set; }

    [Required]
    public decimal Amount { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; } = true;
}