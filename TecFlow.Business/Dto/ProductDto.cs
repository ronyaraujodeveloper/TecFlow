namespace TecFlow.Business.Dto;

public class ProductDto
{
    public string Name { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Features { get; set; } = string.Empty;
    public string Benefits { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? MainImageUrl { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public decimal SalesVolume { get; set; }
    public double Rating { get; set; }
    public string? Material { get; set; }
    public int Stock { get; set; }
    public string? Color { get; set; }
}
