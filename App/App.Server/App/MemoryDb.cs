using System.Linq.Dynamic.Core;

public class MemoryDb
{
    public MemoryDb()
    {
        productList = new List<ProductDto>
        {
            new ProductDto { Text = "Example A", Price = 100 },
            new ProductDto { Text = "Example B", Price = 200 },
            new ProductDto { Text = "Example C", Price = 250 }
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

    public List<HeaderDataRowDto> LoadHeader(GridDto grid, GridCellDto parentCell)
    {
        var result = new List<HeaderDataRowDto>();
        if (parentCell.FieldName != null)
        {
            var query = productList.AsQueryable();
            result = query.Select(parentCell.FieldName).ToDynamicList().Select(item => ((object)item)?.ToString()).Distinct().Select(item => new HeaderDataRowDto { Text = item }).ToList();
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
}
