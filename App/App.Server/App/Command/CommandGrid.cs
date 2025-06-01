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
        // Header Lookup
        if (parentCell?.CellEnum == GridCellEnum.Header && parentGrid != null)
        {
            LoadHeaderLookup(grid, parentCell, parentGrid);
        }
        // Autocomplete
        if (parentCell?.CellEnum == GridCellEnum.FieldAutocomplete)
        {
            LoadAutocomplete(grid);
        }
        // Column Lookup
        if (parentCell?.CellEnum == GridCellEnum.ButtonColumn && parentGrid != null)
        {
            LoadColumnLookup(grid, parentCell, parentGrid);
        }
        return grid;
    }

    private void LoadData(GridDto grid)
    {
        var propertyInfoList = typeof(ProductDto).GetProperties().ToList();
        if (grid.State?.ColumnList?.Count > 0)
        {
            var columnList = grid.State.ColumnList.Select(item => item.FieldName);
            propertyInfoList = propertyInfoList.Where(item => columnList.Contains(item.Name)).ToList();
        }
        grid.RowCellList = [];
        // Column
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.ButtonColumn });
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
        // Cancel
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.ButtonCancel });
        // Save
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.ButtonSave });
    }

    private void LoadColumnLookup(GridDto grid, GridCellDto parentCell, GridDto parentGrid)
    {
        grid.RowCellList = [];
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.Filter, FieldName = "Text" }); // Search field
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto { Text = "false", CellEnum = GridCellEnum.ButtonSelectMultiAll });
        grid.RowCellList.Last().Add(new GridCellDto { Text = "(Select All)", CellEnum = GridCellEnum.Field });
        // State
        if (grid.State == null)
        {
            grid.State = new();
        }
        grid.State.IsSelectMultiList = new();
        // Data
        var list = memoryDb.LoadColumnLookup(grid, parentCell);
        grid.State.IsSelectMultiList.AddRange(new bool?[list.Count]);
        var columnList = parentGrid.State?.ColumnList?.Select(item => item.FieldName).ToList();
        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];
            grid.RowCellList.Add(new List<GridCellDto>());
            grid.RowCellList.Last().Add(new GridCellDto { DataRowIndex = i, CellEnum = GridCellEnum.ButtonSelectMulti });
            grid.RowCellList.Last().Add(new GridCellDto { Text = item.FieldName, DataRowIndex = i, CellEnum = GridCellEnum.Field });
            if (columnList != null && columnList.Count >= 0)
            {
                if (columnList.Contains(item.FieldName!))
                {
                    grid.State.IsSelectMultiList[i] = true;
                }
            }
        }
        grid.RowCellList.Add(new List<GridCellDto>());
        // Cancel
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.ButtonLookupCancel });
        // Ok
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.ButtonLookupOk });
    }

    private void LoadHeaderLookup(GridDto grid, GridCellDto parentCell, GridDto parentGrid)
    {
        grid.RowCellList = [];
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.ButtonLookupSort });
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.Filter, FieldName = "Text" }); // Search field
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto { Text = "false", CellEnum = GridCellEnum.ButtonSelectMultiAll });
        grid.RowCellList.Last().Add(new GridCellDto { Text = "(Select All)", CellEnum = GridCellEnum.Field });
        // State
        if (grid.State == null)
        {
            grid.State = new();
        }
        grid.State.IsSelectMultiList = new();
        // Data
        var list = memoryDb.LoadHeaderLookup(grid, parentCell);
        grid.State.IsSelectMultiList.AddRange(new bool?[list.Count]);
        var filterMulti = parentGrid.State?.FilterMultiList?.SingleOrDefault(item => item.FieldName == parentCell.FieldName);
        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];
            grid.RowCellList.Add(new List<GridCellDto>());
            grid.RowCellList.Last().Add(new GridCellDto { DataRowIndex = i, CellEnum = GridCellEnum.ButtonSelectMulti });
            grid.RowCellList.Last().Add(new GridCellDto { Text = item.Text, DataRowIndex = i, CellEnum = GridCellEnum.Field });
            if (filterMulti != null && item.Text != null && filterMulti.TextList.Contains(item.Text))
            {
                grid.State.IsSelectMultiList[i] = true;
            }
        }
        grid.RowCellList.Add(new List<GridCellDto>());
        // Cancel
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.ButtonLookupCancel });
        // Ok
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

    private GridDto SaveHeaderLookup(GridDto grid, GridCellDto parentCell, GridDto parentGrid)
    {
        if (parentGrid.State == null)
        {
            parentGrid.State = new GridStateDto();
        }
        if (parentGrid.State.FilterList != null)
        {
            parentGrid.State.FilterList.Clear();
        }
        if (grid.State?.IsSelectMultiList != null)
        {
            if (parentGrid.State.FilterMultiList == null)
            {
                parentGrid.State.FilterMultiList = new();
            }
            var fieldName = parentCell.FieldName;
            if (fieldName != null) 
            {
                var filterMulti = parentGrid.State.FilterMultiList.SingleOrDefault(item => item.FieldName == fieldName);
                if (filterMulti == null)
                {
                    filterMulti = new() { FieldName = fieldName, TextList = new() };
                    parentGrid.State.FilterMultiList.Add(filterMulti);
                }
                filterMulti.TextList.Clear();
                for (int dataRowIndex = 0; dataRowIndex < grid.State.IsSelectMultiList.Count; dataRowIndex++)
                {
                    var isSelect = grid.State.IsSelectMultiList[dataRowIndex];
                    if (isSelect == true)
                    {
                        var cell = grid.RowCellList?.SelectMany(item => item).Where(item => item.CellEnum == GridCellEnum.Field && item.DataRowIndex == dataRowIndex).SingleOrDefault();
                        if (cell != null)
                        {
                            var text = cell.Text;
                            if (text != null)
                            {
                                filterMulti.TextList.Add(text);
                            }
                        }
                    }
                }
                if (filterMulti.TextList.Count == 0)
                {
                    parentGrid.State.FilterMultiList.Remove(filterMulti);
                }
            }
        }
        return Load(parentGrid, null, null);
    }

    private GridDto SaveColumnLookup(GridDto grid, GridCellDto parentCell, GridDto parentGrid)
    {
        if (parentGrid.State == null)
        {
            parentGrid.State = new GridStateDto();
        }
        parentGrid.State.ColumnList = new();
        int i = 0;
        foreach (var cell in grid.RowCellList!.SelectMany(item => item).Where(item => item.CellEnum == GridCellEnum.Field && item.DataRowIndex != null))
        {
            var isSelect = grid.State!.IsSelectMultiList![cell.DataRowIndex ?? -1];
            if (isSelect == true)
            {
                parentGrid.State.ColumnList.Add(new GridStateColumnDto { FieldName = cell.Text!, OrderBy = i });
                i++;
            }
        }
        return Load(parentGrid, null, null);
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
        if (parentCell?.CellEnum == GridCellEnum.Header && parentGrid != null)
        {
            return SaveHeaderLookup(grid, parentCell, parentGrid);
        }
        if (parentCell?.CellEnum == GridCellEnum.ButtonColumn && parentGrid != null)
        {
            return SaveColumnLookup(grid, parentCell, parentGrid);
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

    // public List<object>? DataRowList;

    // public GridConfigDto? GridConfig;

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

    /// <summary>
    /// (FieldName)
    /// </summary>
    public List<GridStateFilterDto>? FilterList { get; set; }

    /// <summary>
    /// (FieldName)
    /// </summary>
    public List<GridStateFilterMultiDto>? FilterMultiList { get; set; }

    /// <summary>
    /// (DataRowIndex)
    /// </summary>
    public List<bool?>? IsMouseEnterList { get; set; }

    /// <summary>
    /// (DataRowIndex)
    /// </summary>
    public List<bool?>? IsSelectList { get; set; }

    /// <summary>
    /// (DataRowIndex)
    /// </summary>
    public List<bool?>? IsSelectMultiList { get; set; }

    /// <summary>
    /// Gets or sets ColumnList. This is the list columns to display. If null, display all columns.
    /// </summary>
    public List<GridStateColumnDto>? ColumnList { get; set; }
}

public class GridStateColumnDto
{
    public string FieldName { get; set; } = default!;

    public int OrderBy {  get; set; }
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

public class GridStateFilterMultiDto
{
    public string FieldName { get; set; } = default!;

    public List<string> TextList { get; set; } = default!;
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
    /// <summary>
    /// Column header.
    /// </summary>
    Header = 2,

    /// <summary>
    /// Search field.
    /// </summary>
    Filter = 10,

    /// <summary>
    /// Data grid cancel button.
    /// </summary>
    ButtonCancel = 3,

    /// <summary>
    /// Data grid save button.
    /// </summary>
    ButtonSave = 4,

    FieldDropdown = 5,

    /// <summary>
    /// Lookup window ok button.
    /// </summary>
    ButtonLookupOk = 7,

    /// <summary>
    /// Lookup window cancel button.
    /// </summary>
    ButtonLookupCancel = 8,

    /// <summary>
    /// Lookup window sort button.
    /// </summary>
    ButtonLookupSort = 9,

    /// <summary>
    /// Checkbox field.
    /// </summary>
    FieldCheckbox = 11,

    FieldAutocomplete = 12,

    /// <summary>
    /// Select row button.
    /// </summary>
    ButtonSelectMulti = 13,

    /// <summary>
    /// Select all row button.
    /// </summary>
    ButtonSelectMultiAll = 14,

    /// <summary>
    /// Button to open column select lookup window.
    /// </summary>
    ButtonColumn = 15,
}

public class GridConfigFieldDto
{
    public string FieldName { get; set; } = default!;

    public string? Text { get; set; }

    public bool? IsDropDown { get; set; }
}
