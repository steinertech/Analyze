public class MemoryDb
{
    public MemoryDb()
    {
        ProductList = new List<ProductDto>
        {
            new ProductDto { Text = "Example A", Price = 100 },
            new ProductDto { Text = "Example B", Price = 200 },
            new ProductDto { Text = "Example C", Price = 250 }
        }; 
    }

    public List<ProductDto> ProductList { get; set;  }
}

public class ProductDto
{
    public string? Text { get; set; }
    
    public string? StorageFileName { get; set; }
    
    public double? Price { get; set; }
}
