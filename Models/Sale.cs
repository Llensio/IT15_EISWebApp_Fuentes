using System.ComponentModel.DataAnnotations;

public class Sale
{
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    public Product? Product { get; set; } 

    [Required]
    public int QuantitySold { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime SaleDate { get; set; } = DateTime.Now;

    [StringLength(100)]
    public string? CampaignName { get; set; }
}