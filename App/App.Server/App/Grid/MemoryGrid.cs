using System.Linq.Dynamic.Core;

public class MemoryGrid
{
    public MemoryGrid()
    {
        productList = new List<ProductDto>
        {
            new ProductDto { Text = "Pasta", Price = 100, Amount = 4, City = "Paris" },
            new ProductDto { Text = "Chocolate", Price = 200, Amount = 4, City = "Rome" },
            new ProductDto { Text = "Honey", Price = 250 , Amount = 10, City = "Berlin" },
            new ProductDto { Text = "Butter", Price = 880.80 , Amount = 34, City = "Sydney" },
            new ProductDto { Text = "Yogurt", Price = 65.25 , Amount = 8, City = "Miami" },
            new ProductDto { Text = "Olive", Price = 90.30 , Amount = 2, City = "Denver" },
            new ProductDto { Text = "Bred", Price = 105.25 , Amount = 3, City = "Boston" },
        };
    }

    private List<ProductDto> productList { get; set; }

    /// <summary>
    /// Returns filtered and sorted list according to grid state.
    /// </summary>
    private List<T> Load<T>(List<T> list, GridDto grid)
    {
        var query = list.AsQueryable();
        // Filter
        if (grid.State?.FilterList != null)
        {
            foreach (var filter in grid.State.FilterList)
            {
                query = query.Where($"Convert.ToString({filter.FieldName}).ToLower().Contains(@0)", filter.Text.ToLower());
            }
        }
        // FilterMany
        if (grid.State?.FilterMultiList != null)
        {
            foreach (var filterMulti in grid.State.FilterMultiList)
            {
                var textListLower = filterMulti.TextList.Select(item => item.ToLower()).ToList();
                query = query.Where($"@0.Contains(Convert.ToString({filterMulti.FieldName}).ToLower())", textListLower);
            }
        }
        // Sort
        if (grid.State?.Sort != null)
        {
            if (grid.State.Sort.IsDesc)
            {
                query = query.OrderBy($"{grid.State.Sort.FieldName} DESC");
            }
            else
            {
                query = query.OrderBy($"{grid.State.Sort.FieldName}");
            }
        }
        var result = query.Cast<T>().ToList();
        return result;
    }

    public List<object> Load(GridDto grid)
    {
        var result = Load(productList, grid).Cast<object>().ToList();
        return result;
    }

    public List<HeaderLookupDataRowDto> LoadHeaderLookup(GridDto grid, GridCellDto parentCell)
    {
        var result = new List<HeaderLookupDataRowDto>();
        if (parentCell.FieldName != null)
        {
            var query = productList.AsQueryable();
            result = query.Select(parentCell.FieldName).ToDynamicList().Select(item => ((object)item)?.ToString()).Distinct().Select(item => new HeaderLookupDataRowDto { Text = item }).ToList();
        }
        result = Load(result, grid);
        return result;
    }

    public List<ColumnLookupDataRowDto> LoadColumnLookup(GridDto grid, GridCellDto parentCell)
    {
        var result = new List<ColumnLookupDataRowDto>();
        var propertyInfoList = typeof(ProductDto).GetProperties();
        foreach (var propertyInfo in propertyInfoList)
        {
            result.Add(new() { FieldName = propertyInfo.Name });
        }
        result = Load(result, grid);
        return result;
    }
}

public class ProductDto
{
    public string? Text { get; set; }

    public string? StorageFileName { get; set; }

    public double? Price { get; set; }

    public string? City { get; set; }

    public double? Amount { get; set; }
}
