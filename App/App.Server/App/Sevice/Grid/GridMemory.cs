using System.Linq.Dynamic.Core;

public class GridMemory : GridBase
{
    public GridMemory()
    {
        productList = new List<ProductDto>
        {
            new ProductDto { Id = 1, Text = "Pasta", Price = 100, Amount = 4, City = "Paris" },
            new ProductDto { Id = 2, Text = "Chocolate", Price = 200, Amount = 4, City = "Rome", StorageFileName = "My.png" },
            new ProductDto { Id = 3, Text = "Honey", Price = 250 , Amount = -10, City = "Berlin" },
            new ProductDto { Id = 4, Text = "Butter", Price = 880.80 , Amount = 34, City = "Sydney" },
            new ProductDto { Id = 5, Text = "Yogurt", Price = 65.25 , Amount = 8, City = "Miami" },
            new ProductDto { Id = 6, Text = "Olive", Price = 90.30 , Amount = 2, City = "Denver" },
            new ProductDto { Id = 7, Text = "Bred", Price = 105.25 , Amount = 3, City = "Boston" },
        };
    }

    private List<ProductDto> productList { get; set; }

    private List<Dynamic> ProductListGet()
    {
        var result = UtilGridReflection.DynamicFrom(productList, (dataRowFrom, dataRowTo) =>
        {
            if (dataRowFrom.Amount < 0)
            {
                dataRowTo.IconSet("Amount", "i-warning", "Value negative!");
            }
            if (dataRowFrom.Amount > 0)
            {
                dataRowTo.IconSet("Amount", "i-success", "Value ok!");
            }
            var dropdownList = new List<string?>() { null, "P1.png", "p2.png" };
            dataRowTo.DropdownListSet("StorageFileName", dropdownList);
        });
        return result;
    }

    private void ProductListSet(List<Dynamic> list)
    {
        var result = UtilGridReflection.DynamicTo<ProductDto>(list, (dataRowFrom, dataRowTo) =>
        {
        });
        productList = result;
    }

    /// <summary>
    /// Returns filtered and sorted list according to grid state.
    /// </summary>
    private List<T> Load<T>(List<T> list, GridDto grid)
    {
        var query = list.AsQueryable();
        // Filter
        if (grid.State?.FilterList != null)
        {
            foreach (var (fieldName, text) in grid.State.FilterList)
            {
                query = query.Where($"Convert.ToString({fieldName}).ToLower().Contains(@0)", text.ToLower());
            }
        }
        // FilterMany
        if (grid.State?.FilterMultiList != null)
        {
            foreach (var (fieldName, filterMulti) in grid.State.FilterMultiList)
            {
                var textListLower = filterMulti.TextList.Select(item => item?.ToLower()).ToList();
                query = query.Where($"@0.Contains(Convert.ToString({fieldName}).ToLower())", textListLower);
            }
        }
        // Sort
        var sort = grid.State?.SortList?.FirstOrDefault();
        if (sort != null)
        {
            if (sort.IsDesc)
            {
                query = query.OrderBy($"{sort.FieldName} DESC");
            }
            else
            {
                query = query.OrderBy($"{sort.FieldName}");
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

    public List<GridFilterLookupDataRowDto> LoadFilterLookup(GridDto grid, GridCellDto parentCell)
    {
        var result = new List<GridFilterLookupDataRowDto>();
        if (parentCell.FieldName != null)
        {
            var query = productList.AsQueryable();
            result = query.Select(parentCell.FieldName).ToDynamicList().Select(item => ((object)item)?.ToString()).Distinct().Select(item => new GridFilterLookupDataRowDto { Text = item }).ToList();
        }
        result = Load(result, grid);
        return result;
    }

    public List<GridColumnLookupDataRowDto> LoadColumnLookup(GridDto grid, GridCellDto parentCell)
    {
        var result = new List<GridColumnLookupDataRowDto>();
        var propertyInfoList = typeof(ProductDto).GetProperties();
        foreach (var propertyInfo in propertyInfoList)
        {
            result.Add(new() { FieldName = propertyInfo.Name });
        }
        result = Load(result, grid);
        return result;
    }

    protected override Task<GridConfig> Config2(GridRequest2Dto request)
    {
        var result = UtilGridReflection.GridConfig(typeof(ProductDto));
        result.ColumnList.Single(item => item.FieldName == "City").IsAutocomplete = true;
        result.ColumnList.Single(item => item.FieldName == "StorageFileName").IsDropdown = true;
        return Task.FromResult(result);
    }

    protected override async Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, string? fieldNameDistinct, GridConfig config, GridConfigEnum configEnum, string? modalName)
    {
        var result = ProductListGet();
        result = await UtilGrid.GridLoad2(request, result, null, config, configEnum);
        return result;
    }

    protected override Task GridSave2(GridRequest2Dto request, List<Dynamic> sourceList, GridConfig config)
    {
        var destList = ProductListGet();
        UtilGrid.GridSave2(sourceList, destList, config);
        ProductListSet(destList);
        return Task.CompletedTask;
    }
}

public class ProductDto
{
    public int Id { get; set; }

    public string? Text { get; set; }

    public string? StorageFileName { get; set; }

    public double? Price { get; set; }

    public string? City { get; set; }

    public double? Amount { get; set; }
}
