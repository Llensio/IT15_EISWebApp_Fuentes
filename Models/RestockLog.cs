public class RestockLog
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product Product { get; set; }

    public int QuantityAdded { get; set; }

    public DateTime RestockDate { get; set; } = DateTime.Now;
}
