using System.Data;
using System.Reflection;

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
            return new GridConfigDto
            {
                IsAllowUpdate = true,
                GridConfigFieldList = new()
                {
                    new GridConfigFieldDto { FieldName = nameof(ProductDto.Text), Text = "Description" },
                    new GridConfigFieldDto { FieldName = nameof(ProductDto.StorageFileName), Text = "File", IsDropDown = true },
                    new GridConfigFieldDto { FieldName = nameof(ProductDto.Price), Text = "Price" }
                }
            };
        }
        throw new Exception($"Grid select config not found! ({gridName})");
    }

    public GridDto Load(GridDto grid)
    {
        if (grid.GridName == nameof(ProductDto))
        {
            grid.RowCellList = [];
            var propertyInfoList = typeof(ProductDto).GetProperties();
            grid.RowCellList.Add(new List<GridCellDto>());
            foreach (var propertyInfo in propertyInfoList)
            {
                grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.Header, Text = propertyInfo.Name });
            }
            for (int dataRowIndex = 0; dataRowIndex < memoryDb.ProductList.Count; dataRowIndex++)
            {
                var dataRow = memoryDb.ProductList[dataRowIndex];
                grid.RowCellList.Add(new List<GridCellDto>());
                foreach (var propertyInfo in propertyInfoList)
                {
                    var value = propertyInfo.GetValue(dataRow);
                    var text = (string?)Convert.ChangeType(value, typeof(string));
                    if (propertyInfo.Name == nameof(ProductDto.StorageFileName))
                    {
                        var dropDownList = SelectDropDown(grid.GridName, propertyInfo.Name);
                        grid.RowCellList.Last().Add(new GridCellDto { DataRowIndex = dataRowIndex, FieldName = propertyInfo.Name, Text = text, DropDownList = dropDownList, CellEnum = GridCellEnum.DropDown });
                    }
                    else
                    {
                        grid.RowCellList.Last().Add(new GridCellDto { DataRowIndex = dataRowIndex, FieldName = propertyInfo.Name, Text = text, CellEnum = GridCellEnum.Field });
                    }
                }
            }
            grid.RowCellList.Add(new List<GridCellDto>());
            grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.ButtonCancel });
            grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.ButtonSave });
            return grid;
        }
        throw new Exception($"Grid load not found! ({grid.GridName})");
    }

    private bool SaveMemoryDb(GridDto grid)
    {
        if (grid.GridName == nameof(ProductDto))
        {
            if (grid.RowCellList != null)
            {
                var dataRowList = memoryDb.ProductList;
                foreach (var cellList in grid.RowCellList)
                {
                    foreach (var cell in cellList)
                    {
                        if (cell.FieldName != null && cell.TextModified != null)
                        {
                            if (cell.DataRowIndex == null) // Or -1, -2 for multiple row insert
                            {
                                // TODO Insert
                            }
                            else
                            {
                                var dataRow = dataRowList[cell.DataRowIndex.Value];
                                var propertyInfo = dataRow.GetType().GetProperty(cell.FieldName)!;
                                object? value = null;
                                if (!string.IsNullOrEmpty(cell.TextModified))
                                {
                                    var type = propertyInfo.PropertyType;
                                    var typeUnderlying = Nullable.GetUnderlyingType(type);
                                    if (typeUnderlying != null)
                                    {
                                        type = typeUnderlying;
                                    }
                                    value = Convert.ChangeType(cell.TextModified, type);
                                }
                                propertyInfo.SetValue(dataRow, value);
                            }
                        }
                    }
                }
            }
            return true;
        }
        return false;
    }

    public GridDto Save(GridDto grid)
    {
        if (SaveMemoryDb(grid))
        {
            Load(grid);
            return grid;
        }
        throw new Exception($"Grid save not found! ({grid.GridName})");
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

public class GridConfigDto
{
    public string GridName { get; set; } = default!;
    
    public string DataTableName { get; set; } = default!;

    public List<GridConfigFieldDto> GridConfigFieldList { get; set; } = default!;

    public bool? IsAllowUpdate { get; set; }

    public bool? IsAllowInsert { get; set; }

    public bool? IsAllowDelete { get; set; }
}

public class GridDto
{
    public string GridName { get; set; } = default!;

    public List<object>? DataRowList;

    public GridConfigDto? GridConfig;

    /// <summary>
    /// (Row, Cell)
    /// </summary>
    public List<List<GridCellDto>>? RowCellList { get; set; }
}

public class GridCellDto
{
    public GridCellEnum? CellEnum { get; set; }
 
    public int? DataRowIndex { get; set; }
    
    public string? FieldName { get; set; }
    
    public string? Text { get; set; }
    
    /// <summary>
    /// Gets or sets TextModified. This is from user modified text to save.
    /// </summary>
    public string? TextModified { get; set; }

    public List<string>? DropDownList { get; set; }
}

public enum GridCellEnum
{
    None = 0,
    Field = 1,
    Header = 2,
    ButtonCancel = 3,
    ButtonSave = 4,
    DropDown = 5,
}

public class GridConfigFieldDto
{
    public string FieldName { get; set; } = default!;

    public string? Text { get; set; }

    public bool? IsDropDown { get; set; }
}