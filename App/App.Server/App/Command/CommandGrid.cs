public class CommandGrid(MemoryGrid memoryGrid, ExcelGrid excelGrid, StorageGrid storageGrid, ArticleGrid articleGrid)
{
    /// <summary>
    /// Returns loaded grid.
    /// </summary>
    public async Task<GridDto> Load(GridDto grid, GridCellDto? parentCell, GridControlDto? parentControl, GridDto? parentGrid)
    {
        // Article
        if (grid.GridName == "Article")
        {
            await articleGrid.Load(grid);
            return grid;
        }
        // Excel
        if (grid.GridName == "Excel")
        {
            await excelGrid.Load(grid);
            return grid;
        }
        // Storage
        if (grid.GridName == "Storage")
        {
            await storageGrid.Load(grid, parentCell, parentControl, parentGrid);
            return grid;
        }
        // Data
        if (parentCell == null)
        {
            DataLoad(grid);
        }
        // Header Lookup
        if (parentCell?.CellEnum == GridCellEnum.Header && parentGrid != null)
        {
            LookupHeaderLoad(grid, parentCell, parentGrid);
        }
        // Autocomplete
        if (parentCell?.CellEnum == GridCellEnum.FieldAutocomplete)
        {
            LookupAutocompleteLoad(grid);
        }
        // Column Lookup
        var parentCellIsColumn = parentCell?.ControlList?.Where(item => item.ControlEnum == GridControlEnum.ButtonColumn).Any();
        if (parentCell != null && parentCellIsColumn == true && parentGrid != null)
        {
            LookupColumnLoad(grid, parentCell, parentGrid);
        }
        return grid;
    }

    private void DataLoad(GridDto grid)
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
        grid.RowCellList.Last().Add(new GridCellDto
        {
            CellEnum = GridCellEnum.Control,
            ControlList = [
            new(){ ControlEnum =  GridControlEnum.ButtonColumn },
            ]
        });
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
        var list = memoryGrid.Load(grid);
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
                    var dropDownList = DropDownLoad(grid.GridName, propertyInfo.Name);
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
                        if (propertyInfo.Name == nameof(ProductDto.Amount))
                        {
                            grid.AddCell(new() { DataRowIndex = dataRowIndex, FieldName = propertyInfo.Name, Text = text, CellEnum = GridCellEnum.Field, IconLeft = new GridCellIconDto { ClassName = "i-warning", Tooltip = "Wg - Latest value from today" }, IconRight = new GridCellIconDto { ClassName = "i-success", Tooltip = "Validation Ok" } });
                        }
                        else
                        {
                            grid.RowCellList.Last().Add(new GridCellDto { DataRowIndex = dataRowIndex, FieldName = propertyInfo.Name, Text = text, CellEnum = GridCellEnum.Field });
                        }
                    }
                }
            }
        }

        // ColSpan RowSpan
        grid.AddRow();
        grid.AddCell(new() { CellEnum = GridCellEnum.Field, Text = "ColSpan2", ColSpan = 2 });
        grid.AddCell(new() { CellEnum = GridCellEnum.Field, Text = "RolSpan2", RowSpan = 2 });
        grid.AddRow();
        grid.AddCell(new() { CellEnum = GridCellEnum.Field, Text = "One" });

        // Button Cancel, Save
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto
        {
            CellEnum = GridCellEnum.Control,
            ControlList = [
            new() { ControlEnum = GridControlEnum.ButtonReload },
            new() { ControlEnum = GridControlEnum.ButtonSave },
            ]
        });
    }

    private void LookupColumnLoad(GridDto grid, GridCellDto parentCell, GridDto parentGrid)
    {
        grid.RowCellList = [];
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.Filter, FieldName = "Text" }); // Search field
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto
        {
            CellEnum = GridCellEnum.Control,
            ControlList = [
            new() { ControlEnum = GridControlEnum.CheckboxSelectMultiAll, Text = "false" },
            new() { ControlEnum = GridControlEnum.LabelCustom, Text = "(Select All)" },
            ]
        });
        // State
        if (grid.State == null)
        {
            grid.State = new();
        }
        grid.State.IsSelectMultiList = new();
        // Data
        var list = memoryGrid.LoadColumnLookup(grid, parentCell);
        grid.State.IsSelectMultiList.AddRange(new bool?[list.Count]);
        var columnList = parentGrid.State?.ColumnList?.Select(item => item.FieldName).ToList();
        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];
            grid.RowCellList.Add(new List<GridCellDto>());
            grid.RowCellList.Last().Add(new GridCellDto { DataRowIndex = i, CellEnum = GridCellEnum.CheckboxSelectMulti });
            grid.RowCellList.Last().Add(new GridCellDto { Text = item.FieldName, DataRowIndex = i, CellEnum = GridCellEnum.Field });
            if (columnList != null && columnList.Count >= 0)
            {
                if (columnList.Contains(item.FieldName!))
                {
                    grid.State.IsSelectMultiList[i] = true;
                }
            }
        }
        // Button Cancel, Ok (Lookup)
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto
        {
            CellEnum = GridCellEnum.Control,
            ControlList = [
            new() { ControlEnum = GridControlEnum.ButtonLookupCancel },
            new() { ControlEnum = GridControlEnum.ButtonLookupOk },
            ]
        });
    }

    private void LookupHeaderLoad(GridDto grid, GridCellDto parentCell, GridDto parentGrid)
    {
        grid.RowCellList = [];
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto
        {
            CellEnum = GridCellEnum.Control,
            ControlList = [
            new() { ControlEnum = GridControlEnum.ButtonLookupSort }
            ]
        });
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.Filter, FieldName = "Text" }); // Search field
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto
        {
            CellEnum = GridCellEnum.Control,
            ControlList = [
            new() { ControlEnum = GridControlEnum.CheckboxSelectMultiAll, Text = "false" },
            new() { ControlEnum = GridControlEnum.LabelCustom, Text = "(Select All)" },
            ]
        });
        // State
        if (grid.State == null)
        {
            grid.State = new();
        }
        grid.State.IsSelectMultiList = new();
        // Data
        var list = memoryGrid.LoadHeaderLookup(grid, parentCell);
        grid.State.IsSelectMultiList.AddRange(new bool?[list.Count]);
        var filterMulti = parentGrid.State?.FilterMultiList?.SingleOrDefault(item => item.FieldName == parentCell.FieldName);
        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];
            grid.RowCellList.Add(new List<GridCellDto>());
            grid.RowCellList.Last().Add(new GridCellDto { DataRowIndex = i, CellEnum = GridCellEnum.CheckboxSelectMulti });
            grid.RowCellList.Last().Add(new GridCellDto { Text = item.Text, DataRowIndex = i, CellEnum = GridCellEnum.Field });
            if (filterMulti != null && item.Text != null && filterMulti.TextList.Contains(item.Text))
            {
                grid.State.IsSelectMultiList[i] = true;
            }
        }
        // Button Cancel, Ok (Lookup)
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto
        {
            CellEnum = GridCellEnum.Control,
            ControlList = [
            new() { ControlEnum = GridControlEnum.ButtonLookupCancel },
            new() { ControlEnum = GridControlEnum.ButtonLookupOk },
            ]
        });
    }

    private void LookupAutocompleteLoad(GridDto grid)
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
        // Button Cancel, Ok (Lookup)
        grid.RowCellList.Add(new List<GridCellDto>());
        grid.RowCellList.Last().Add(new GridCellDto
        {
            CellEnum = GridCellEnum.Control,
            ControlList = [
            new() { ControlEnum = GridControlEnum.ButtonLookupCancel },
            new() { ControlEnum = GridControlEnum.ButtonLookupOk },
            ]
        });
    }

    private GridDto DataSave(GridDto grid)
    {
        var dataRowList = memoryGrid.Load(grid);
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
        return grid;
    }

    private async Task LookupHeaderSave(GridDto grid, GridCellDto parentCell, GridDto parentGrid)
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
        await Load(parentGrid, null, null, null);
    }

    private async Task LookupColumnSave(GridDto grid, GridCellDto parentCell, GridDto parentGrid)
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
        await Load(parentGrid, null, null, null); // TODO Column on lookup. For example filter would be missing.
    }

    /// <summary>
    /// User clicked entry on autocomplete lookup. Update parent grid.
    /// </summary>
    private void LookupAutocompleteSave(GridDto grid, GridCellDto parentCell, GridDto parentGrid)
    {
        var cell = grid.SelectedCell("Text");
        if (cell != null)
        {
            parentCell.TextModified = cell.Text;
        }
    }

    public async Task<GridSaveDto> Save(GridDto grid, GridCellDto? parentCell, GridControlDto? parentControl, GridDto? parentGrid)
    {
        // Storage
        if (grid.GridName == "Storage")
        {
            await storageGrid.Save(grid, parentCell, parentControl, parentGrid);
            return new() { Grid = grid, ParentGrid = parentGrid };
        }
        if (parentCell == null)
        {
            return new() { Grid = DataSave(grid) };
        }
        if (parentCell?.CellEnum == GridCellEnum.Header && parentGrid != null)
        {
            await LookupHeaderSave(grid, parentCell, parentGrid);
            return new() { Grid = grid, ParentGrid = parentGrid };
        }
        var parentCellIsColumn = parentCell?.ControlList?.Where(item => item.ControlEnum == GridControlEnum.ButtonColumn).Any();
        if (parentCell != null && parentCellIsColumn == true && parentGrid != null)
        {
            await LookupColumnSave(grid, parentCell, parentGrid);
            return new() { Grid = grid, ParentGrid = parentGrid };
        }
        if (parentCell?.CellEnum == GridCellEnum.FieldAutocomplete && parentGrid != null)
        {
            if (parentCell.DataRowIndex != null && parentCell.FieldName != null)
            {
                // Parameter parentCell to parentGrid cell instance
                parentCell = parentGrid.RowCellList?.SelectMany(item => item).Where(item => item.DataRowIndex == parentCell.DataRowIndex && item.FieldName == parentCell.FieldName).FirstOrDefault() ?? parentCell;
            }
            LookupAutocompleteSave(grid, parentCell, parentGrid);
            return new() { Grid = grid, ParentGrid = parentGrid };
        }
        return new() { Grid = grid };
    }

    private List<string> DropDownLoad(string gridName, string fieldName)
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

public class GridSaveDto
{
    public GridDto Grid { get; set; } = default!;

    /// <summary>
    /// Gets or sets ParentGrid. Can be used to reload parent grid when lookup is closed. See also GridControlEnum.ButtonLookupOk;
    /// </summary>
    public GridDto? ParentGrid { get; set; }
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

    public GridStateDto StateGet()
    {
        State = State ?? new();
        return State;
    }

    public GridPaginationDto PaginationGet()
    {
        StateGet().Pagination = StateGet().Pagination ?? new();
        return StateGet().Pagination!;
    }

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

    public List<GridCellDto> CellList()
    {
        if (RowCellList == null)
        {
            return new();
        }
        else
        {
            return RowCellList.SelectMany(item => item).ToList();
        }
    }

    public List<GridCellDto> CellModifiedList()
    {
        return CellList().Where(item => item.CellEnum == GridCellEnum.Field && item.TextModified != null).ToList();
    }

    public List<GridControlDto> ControlList()
    {
        if (RowCellList == null)
        {
            return new();
        }
        else
        {
            return RowCellList.SelectMany(item => item).SelectMany(item => item.ControlList ?? []).ToList();
        }
    }

    public List<GridControlDto> ControlModifiedList()
    {
        return ControlList().Where(item => item.ControlEnum == GridControlEnum.FieldCustom && item.TextModified != null).ToList();
    }

    public List<List<GridCellDto>>? Clear()
    {
        RowCellList = RowCellList ?? new();
        RowCellList.Clear();
        if (State != null)
        {
            State.RowKeyList = null;
        }
        return RowCellList;
    }

    public void ClearResponse()
    {
        if (State?.ButtonCustomClick != null)
        {
            State.ButtonCustomClick = null;
        }
        if (State?.Pagination?.PageIndexClick != null)
        {
            State.Pagination.PageIndexClick = null;
        }
    }

    public List<GridCellDto> AddRow()
    {
        RowCellList = RowCellList ?? new();
        var result = new List<GridCellDto>();
        RowCellList.Add(result);
        return result;
    }

    public GridCellDto AddCell(GridCellDto cell, string? rowKey = null)
    {
        RowCellList = RowCellList ?? new();
        if (RowCellList.LastOrDefault() == null)
        {
            RowCellList.Add(new());
        }
        RowCellList.Last().Add(cell);
        if (rowKey != null)
        {
            UtilServer.Assert(cell.DataRowIndex != null, "DataRowIndex can not be null!");
            if (State == null)
            {
                State = new GridStateDto();
            }
            if (State.RowKeyList == null)
            {
                State.RowKeyList = new();
            }
            var dataRowIndex = cell.DataRowIndex!.Value;
            if (State.RowKeyList.Count <= dataRowIndex)
            {
                var emptyList = Enumerable.Repeat<string?>(null, dataRowIndex + 1 - State.RowKeyList.Count).ToList();
                State.RowKeyList.AddRange(emptyList);
            }
            var rowKeyOld = State.RowKeyList[dataRowIndex];
            if (rowKeyOld != null)
            {
                UtilServer.Assert(rowKeyOld == rowKey, "RowKey invalid!");
            }
            State.RowKeyList[dataRowIndex] = rowKey;
        }
        return cell;
    }

    public GridControlDto AddControl(GridControlDto control)
    {
        RowCellList = RowCellList ?? new();
        if (RowCellList.LastOrDefault() == null)
        {
            RowCellList.Add(new());
        }
        var row = RowCellList.Last();
        if (row.LastOrDefault()?.CellEnum != GridCellEnum.Control)
        {
            row.Add(new() { CellEnum = GridCellEnum.Control, ControlList = [] });
        }
        var cell = row.Last();
        cell.ControlList = cell.ControlList ?? [];
        cell.ControlList.Add(control);
        return control;
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

    /// <summary>
    /// Gets or sets ColumnWidthList. Used to resize columns.
    /// </summary>
    public List<double?>? ColumnWidthList { get; set; }

    /// <summary>
    /// Gets or sets ButtonCustomClick. User clicked button. Process it in Grid.Save();
    /// </summary>
    public GridStateButtonCustomClickDto? ButtonCustomClick { get; set; }

    /// <summary>
    /// Gets or sets RowKeyList. This is typically the data primary key. (DataRowIndex, RowKey)
    /// </summary>
    public List<string?>? RowKeyList { get; set; }

    /// <summary>
    /// Gets or sets RowKeyMasterList. This value is set by a master grid on its data row selection. (GridName, RowKey)
    /// </summary>
    public Dictionary<string, string?>? RowKeyMasterList { get; set; }

    public GridPaginationDto? Pagination { get; set; }
}

public class GridPaginationDto
{
    public int? PageIndex { get; set; }

    public int? PageCount { get; set; }
    
    public int? PageSize { get; set; }

    public int? PageIndexClick { get; set; }
}

public class GridStateButtonCustomClickDto
{
    public string? Name { get; set; }

    public int? DataRowIndex { get; set; }

    public string? FieldName { get; set; }
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

/// <summary>
/// GridCellDto hosts data cell specific element like text input box. Or it hosts a list of 
/// not data cell specific controls like cancel, save and delete button.
/// </summary>
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
    /// Gets or sets TextModified. This is from user modified text to save. Empty not null if user deleted text.
    /// </summary>
    public string? TextModified { get; set; } // TODO move to state

    public List<string>? DropDownList { get; set; }

    /// <summary>
    /// Gets or sets ControlList. Applicable for CellEnum.Control
    /// </summary>
    public List<GridControlDto>? ControlList { get; set; }

    public GridCellIconDto? IconLeft { get; set; }

    public GridCellIconDto? IconRight { get; set; }

    public int? ColSpan { get; set; }
    
    public int? RowSpan { get; set; }
}

public class GridCellIconDto
{
    /// <summary>
    /// Gets or sets ClassName. For example i-info, i-warning, i-error.
    /// </summary>
    public string? ClassName { get; set; }

    public string? Tooltip { get; set; }
}

/// <summary>
/// Controls are not data cell specific. Like cancel, save and delete button.
/// Multiple controls can be in one cell.
/// </summary>
public class GridControlDto
{
    public GridControlEnum? ControlEnum { get; set; }

    public string? Text { get; set; }
    
    public string? TextModified { get; set; } // TODO move to state

    public string? Name { get; set; }
}

public enum GridCellEnum
{
    None = 0,

    /// <summary>
    /// Data field.
    /// </summary>
    Field = 1,
    
    /// <summary>
    /// Column header.
    /// </summary>
    Header = 2,

    /// <summary>
    /// Search field.
    /// </summary>
    Filter = 10,

    FieldDropdown = 5,

    /// <summary>
    /// Checkbox field.
    /// </summary>
    FieldCheckbox = 11,

    /// <summary>
    /// Data field with autocomplete.
    /// </summary>
    FieldAutocomplete = 12,

    /// <summary>
    /// Select row checkbox.
    /// </summary>
    CheckboxSelectMulti = 13, // GridCellEnum.CheckboxSelectMulti is data cell specific. ControlEnum.CheckboxSelectMultiAll is not data cell specific.

    /// <summary>
    /// Cell with list of controls.
    /// </summary>
    Control = 16,
}

public enum GridControlEnum
{
    None = 0,

    /// <summary>
    /// Data grid reload button.
    /// </summary>
    ButtonReload = 3,

    /// <summary>
    /// Data grid save button.
    /// </summary>
    ButtonSave = 4,

    /// <summary>
    /// Lookup window cancel button. If this grid is a lookup window it gets closed.
    /// </summary>
    ButtonLookupCancel = 8,

    /// <summary>
    /// Lookup window ok button. If this grid is a lookup window it gets saved and closed after.
    /// </summary>
    ButtonLookupOk = 7,

    /// <summary>
    /// Lookup window sort button.
    /// </summary>
    ButtonLookupSort = 9,

    /// <summary>
    /// Button to open column select lookup window.
    /// </summary>
    ButtonColumn = 15,

    /// <summary>
    /// Custom button like for example delete.
    /// </summary>
    ButtonCustom = 16,

    /// <summary>
    /// Select all row checkbox.
    /// </summary>
    CheckboxSelectMultiAll = 14, // GridCellEnum.CheckboxSelectMulti is data cell specific. ControlEnum.CheckboxSelectMultiAll is not data cell specific.

    /// <summary>
    /// Custom readonly label.
    /// </summary>
    LabelCustom = 17,

    /// <summary>
    /// Custom text field.
    /// </summary>
    FieldCustom = 18,

    /// <summary>
    /// Opens a lookup modal windows. Calls method CommandGrid.Load(); to get modal grid data.
    /// </summary>
    ButtonModal = 19,

    /// <summary>
    /// Data grid pagination. See also GridPaginationDto.
    /// </summary>
    Pagination = 20,
}
