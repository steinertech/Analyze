public class CommandGrid(MemoryDb memoryDb)
{
    /// <summary>
    /// Returns loaded grid.
    /// </summary>
    public GridDto Load(GridDto grid, GridCellDto? parentCell, GridDto? parentGrid)
    {
        // Data
        if (parentCell == null)
        {
            LoadData(grid);
        }
        // Header
        if (parentCell?.CellEnum == GridCellEnum.Header)
        {
            LoadHeader(grid, parentCell);
        }
        // Autocomplete
        if (parentCell?.CellEnum == GridCellEnum.FieldAutocomplete)
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
                    grid.RowCellList.Last().Add(new GridCellDto { DataRowIndex = dataRowIndex, FieldName = propertyInfo.Name, Text = text, DropDownList = dropDownList, CellEnum = GridCellEnum.FieldDropdown });
                }
                else
                {
                    if (propertyInfo.Name == nameof(ProductDto.City))
                    {
                        grid.RowCellList.Last().Add(new GridCellDto { DataRowIndex = dataRowIndex, FieldName = propertyInfo.Name, Text = text, CellEnum = GridCellEnum.FieldAutocomplete });
                    }
                    else
                    {
                        grid.RowCellList.Last().Add(new GridCellDto { DataRowIndex = dataRowIndex, FieldName = propertyInfo.Name, Text = text, CellEnum = GridCellEnum.Field });
                    }
                }
            }
        }
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.ButtonCancel });
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.ButtonSave });
    }

    private void LoadHeader(GridDto grid, GridCellDto parentCell)
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
        var list = memoryDb.LoadHeader(grid, parentCell);
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
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.Field, Text = "Berlin", DataRowIndex = 0, FieldName = "Text" });
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.Field, Text = "Paris", DataRowIndex = 1, FieldName = "Text" });
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.Field, Text = "Rome", DataRowIndex = 2, FieldName = "Text" });
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.Field, Text = "Madrid", DataRowIndex = 3, FieldName = "Text" });
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
        return Load(new GridDto { GridName = grid.GridName }, null, null);
    }

    /// <summary>
    /// Returns parent grid. User clicked entry on autocomplete lookup.
    /// </summary>
    private GridDto SaveAutocomplete(GridDto grid, GridCellDto parentCell, GridDto parentGrid)
    {
        var cell = grid.SelectedCell("Text");
        if (cell != null)
        {
            parentCell.TextModified = cell.Text;
        }
        return parentGrid; // Load(new GridDto { GridName = grid.GridName }, null);
    }

    public GridDto Save(GridDto grid, GridCellDto? parentCell, GridDto? parentGrid)
    {
        if (parentCell == null)
        {
            return SaveData(grid);
        }
        if (parentCell?.CellEnum == GridCellEnum.Header)
        {
            return SaveHeader(grid);
        }
        if (parentCell?.CellEnum == GridCellEnum.FieldAutocomplete && parentGrid != null)
        {
            if (parentCell.DataRowIndex != null && parentCell.FieldName != null)
            {
                // Parameter parentCell to parentGrid cell instance
                parentCell = parentGrid.RowCellList?.SelectMany(item => item).Where(item => item.DataRowIndex == parentCell.DataRowIndex && item.FieldName == parentCell.FieldName).FirstOrDefault() ?? parentCell;
            }
            return SaveAutocomplete(grid, parentCell, parentGrid);
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

    public GridStateDto? State { get; set; }

    public GridCellDto? SelectedCell(string fieldName)
    {
        GridCellDto? result = null;
        if (State?.IsSelectList != null && RowCellList != null)
        {
            var index = State.IsSelectList.IndexOf(true);
            var row = RowCellList[index];
            result = row.Where(item => item.FieldName == fieldName).FirstOrDefault();
        }
        return result;
    }
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
    FieldDropdown = 5,
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