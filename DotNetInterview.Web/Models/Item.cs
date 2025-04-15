namespace DotNetInterview.Web.Models;

public class Item
{
    public Guid Id { get; set; }
    public string Reference { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public decimal? CurrentPrice { get; set; }
    public string? Status { get; set; }
    public List<Variation> Variations { get; set; } = new();
}

public class Variation
{
    public string Size { get; set; }
    public int Quantity { get; set; }
} 