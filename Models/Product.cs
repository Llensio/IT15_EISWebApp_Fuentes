using System.ComponentModel.DataAnnotations;

public class Product
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string ProductName { get; set; }

    [Required]
    [StringLength(50)]
    public string Category { get; set; }

    [Required]
    [Range(0.01, 1000000)]
    public decimal Price { get; set; }

    [Required]
    public string Status { get; set; } // Active / Inactive
    public bool IsArchived { get; set; } = false;

    // ✅ ERP Inventory Fields
    public int StockQuantity { get; set; } = 0;

    public int ReorderLevel { get; set; } = 5;

}
