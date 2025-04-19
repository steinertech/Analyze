public class CommandGrid(MemoryDb memoryDb)
{
    public List<object> Select(string gridName)
    {
        if (gridName == nameof(ProductDto))
        {
            return memoryDb.ProductList.Cast<object>().ToList();
        }
        throw new Exception($"Grid select not found! ({gridName})");
    }

    public GridConfigDto SelectConfig(string gridName)
    {
        if (gridName == nameof(ProductDto))
        {
            var result = new GridConfigDto
            {
                IsAllowUpdate = true,
                GridConfigFieldList =
            {
                new GridConfigFieldDto { FieldName = nameof(ProductDto.Text), Text = "Description" },
                new GridConfigFieldDto { FieldName = nameof(ProductDto.StorageFileName), Text = "File", IsDropDown = true },
                new GridConfigFieldDto { FieldName = nameof(ProductDto.Price), Text = "Price" }
            }
            };
        }
        throw new Exception($"Grid select config not found! ({gridName})");
    }

    private void Update(List<object> list, List<UpdateDto> updateList)
    {
        foreach (var item in updateList)
        {
            var row = list[item.Index];
            var propertyInfo = row.GetType().GetProperty(item.FieldName)!;
            var value = Convert.ChangeType(item.Text, propertyInfo.PropertyType);
            propertyInfo.SetValue(item, value);
        }
    }

    public void Update(string gridName, List<object> list, List<UpdateDto> updateList)
    {
        if (gridName == nameof(ProductDto))
        {
            Update(list, updateList);
            memoryDb.ProductList = list.Cast<ProductDto>().ToList();
            return;
        }
        throw new Exception($"Grid update not found! ({gridName})");
    }

    public List<string> SelectDropDown(string gridName, string fieldName)
    {
        if (gridName == nameof(ProductDto) && fieldName == nameof(ProductDto.StorageFileName))
        {
            return new List<string>
            {
                "",
                "P1.png",
                "p2.png"
            };
        }
        throw new Exception($"Grid select drop down not found! ({gridName}.{fieldName})");
    }
}

public class UpdateDto
{
    public int Index { get; set; } = default!;

    public string FieldName { get; set; } = default!;

    /// <summary>
    /// Gets or sets Text. This is user entered text to parse.
    /// </summary>
    public string? Text { get; set; }
}

public class GridConfigDto
{
    public List<GridConfigFieldDto> GridConfigFieldList { get; set; } = default!;

    public bool? IsAllowUpdate { get; set; }

    public bool? IsAllowInsert { get; set; }

    public bool? IsAllowDelete { get; set; }
}

public class GridConfigFieldDto
{
    public string FieldName { get; set; } = default!;

    public string? Text { get; set; }

    public bool? IsDropDown { get; set; }
}