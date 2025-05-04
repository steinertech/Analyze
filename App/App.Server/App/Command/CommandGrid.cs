public class CommandGrid(MemoryDb memoryDb)
{
    public GridDto Load(GridDto grid)
    {
        // Data
        if (grid.ParentCell == null)
        {
            LoadData(grid);
        }
        // Header
        if (grid.ParentCell?.CellEnum == GridCellEnum.Header)
        {
            LoadHeader(grid);
        }
        // Autocomplete
        if (grid.ParentCell?.CellEnum == GridCellEnum.FieldAutocomplete)
        {
            LoadAutocomplete(grid);
        }
        return grid;
    }

    private void LoadData(GridDto grid)
    {
        grid.RowCellList = [];
        var propertyInfoList = typeof(ProductDto).GetProperties();
        // Header
        grid.RowCellList.Add(new List<GridCellDto>());
        foreach (var propertyInfo in propertyInfoList)
        {
            grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.Header, Text = propertyInfo.Name, FieldName = propertyInfo.Name });
        }
        // Filter
        grid.RowCellList.Add(new List<GridCellDto>());
        foreach (var propertyInfo in propertyInfoList)
        {
            grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.Filter, FieldName = propertyInfo.Name });
        }
        // Row
        var list = memoryDb.Load(grid);
        for (int dataRowIndex = 0; dataRowIndex < list.Count; dataRowIndex++)
        {
            var dataRow = list[dataRowIndex];
            grid.RowCellList.Add(new List<GridCellDto>());
            // Cell
            foreach (var propertyInfo in propertyInfoList)
            {
                var value = propertyInfo.GetValue(dataRow);
                var text = (string?)Convert.ChangeType(value, typeof(string));
                if (propertyInfo.Name == nameof(ProductDto.StorageFileName))
                {
                    var dropDownList = LoadDropDown(grid.GridName, propertyInfo.Name);
                    grid.RowCellList.Last().Add(new GridCellDto { DataRowIndex = dataRowIndex, FieldName = propertyInfo.Name, Text = text, DropDownList = dropDownList, CellEnum = GridCellEnum.Dropdown });
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
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.FieldAutocomplete, FieldName = "Text" });
    }

    private void LoadHeader(GridDto grid)
    {
        grid.RowCellList = [];
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.ButtonLookupSort });
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.Filter, FieldName = "Text" }); // Search field
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto { Text = "false", CellEnum = GridCellEnum.FieldCheckbox });
        grid.RowCellList.Last().Add(new GridCellDto { Text = "(Select All)", CellEnum = GridCellEnum.Field });
        // Data
        var list = memoryDb.LoadHeader(grid);
        foreach (var item in list)
        {
            grid.RowCellList.Add(new List<GridCellDto>());
            grid.RowCellList.Last().Add(new GridCellDto { Text = "true", CellEnum = GridCellEnum.FieldCheckbox });
            grid.RowCellList.Last().Add(new GridCellDto { Text = item.Text, CellEnum = GridCellEnum.Field });
        }
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.ButtonLookupCancel });
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.ButtonLookupOk });
    }

    private void LoadAutocomplete(GridDto grid)
    {
        grid.RowCellList = [];
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.Field, Text = "Hello", DataRowIndex = 0 });
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.Field, Text = "World", DataRowIndex = 1 });
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.ButtonLookupCancel });
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.ButtonLookupOk });
    }

    private void SaveDataMemoryDb(GridDto grid)
    {
        var dataRowList = memoryDb.Load(grid);
        foreach (var cellList in grid.RowCellList!)
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

    private GridDto SaveData(GridDto grid)
    {
        SaveDataMemoryDb(grid);
        return grid;
    }

    private GridDto SaveHeader(GridDto grid)
    {
        return Load(new GridDto { GridName = grid.GridName });
    }

    private GridDto SaveAutocomplete(GridDto grid)
    {
        return Load(new GridDto { GridName = grid.GridName });
    }

    public GridDto Save(GridDto grid)
    {
        if (grid.ParentCell == null)
        {
            return SaveData(grid);
        }
        if (grid.ParentCell?.CellEnum == GridCellEnum.Header)
        {
            return SaveHeader(grid);
        }
        if (grid.ParentCell?.CellEnum == GridCellEnum.FieldAutocomplete)
        {
            return SaveAutocomplete(grid);
        }
        return grid;
    }

    private List<string> LoadDropDown(string gridName, string fieldName)
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

    /// <summary>
    /// Gets or sets ParentCell. This is the origin cell which opened this lookup grid.
    /// </summary>
    public GridCellDto? ParentCell { get; set; }

    public GridStateDto? State { get; set; }
}

public class GridStateDto
{
    public GridStateSortDto? Sort { get; set; } // public List<GridStateSortDto> SortList { get; set; }

    public List<GridStateFilterDto>? FilterList { get; set; }

    /// <summary>
    /// (DataRowIndex)
    /// </summary>
    public List<bool?>? IsMouseEnterList { get; set; }

    /// <summary>
    /// (DataRowIndex)
    /// </summary>
    public List<bool?>? IsSelectList { get; set; }
}

public class GridStateSortDto
{
    public string FieldName { get; set; } = default!;

    public bool IsDesc { get; set; }
}

public class GridStateFilterDto
{
    public string FieldName { get; set; } = default!;

    public string Text { get; set; } = default!;
}

public class GridCellDto
{
    public GridCellEnum? CellEnum { get; set; }

    /// <summary>
    /// Gets or sets DataRowIndex. Also used for row select and mouse enter.
    /// </summary>
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
    Filter = 10,
    ButtonCancel = 3,
    ButtonSave = 4,
    Dropdown = 5,
    ButtonLookupOk = 7,
    ButtonLookupCancel = 8,
    ButtonLookupSort = 9,
    FieldCheckbox = 11,
    FieldAutocomplete = 12
}

public class GridConfigFieldDto
{
    public string FieldName { get; set; } = default!;

    public string? Text { get; set; }

    public bool? IsDropDown { get; set; }
}