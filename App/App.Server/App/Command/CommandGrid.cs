public class CommandGrid(GridMemory memoryGrid, GridExcel excelGrid, GridStorage storageGrid, GridArticle articleGrid, GridArticle2 gridArticle)
{
    /// <summary>
    /// Returns loaded grid.
    /// </summary>
    public async Task<GridResponseDto> Load(GridRequestDto request)
    {
        // Article
        if (request.Grid.GridName == "Article")
        {
            await articleGrid.Load(request.Grid, request.ParentCell, request.ParentControl, request.ParentGrid);
            return new GridResponseDto { Grid = request.Grid };
        }
        // Article
        if (request.Grid.GridName == "Article2")
        {
            var response = await gridArticle.Load(request);
            return response;
        }
        if (request.Grid.State?.FieldSaveList?.Count() > 0)
        {
            // Storage
            if (request.Grid.GridName == "Storage")
            {
                await storageGrid.Save(request.Grid, request.ParentCell, request.ParentControl, request.ParentGrid);
                return new() { Grid = request.Grid, ParentGrid = request.ParentGrid };
            }
            if (request.ParentCell == null)
            {
                var result = new GridResponseDto() { Grid = DataSave(request.Grid) };
                if (result.Grid?.State?.FieldSaveList != null)
                {
                    result.Grid.State.FieldSaveList = null;
                }
                await Load(request);
                return result;
            }
        }
        // Excel
        if (request.Grid.GridName == "Excel")
        {
            await excelGrid.Load(request.Grid);
            return new GridResponseDto { Grid = request.Grid };
        }
        // Storage
        if (request.Grid.GridName == "Storage")
        {
            await storageGrid.Load(request.Grid, request.ParentCell, request.ParentControl, request.ParentGrid);
            return new GridResponseDto { Grid = request.Grid };
        }
        // Data
        if (request.ParentCell == null)
        {
            DataLoad(request.Grid);
        }
        // Filter Lookup
        if (request.ParentCell?.CellEnum == GridCellEnum.Header && request.ParentGrid != null)
        {
            if (request.Grid.RowCellList == null)
            {
                LookupFilterLoad(request.Grid, request.ParentCell, request.ParentGrid);
            }
            else
            {
                await LookupFilterSave(request.Grid, request.ParentCell, request.ParentGrid);
                return new() { Grid = request.Grid, ParentGrid = request.ParentGrid };
            }
        }
        // Autocomplete
        if (request.ParentCell?.CellEnum == GridCellEnum.FieldAutocomplete && request.ParentGrid != null)
        {
            if (request.Grid.RowCellList == null)
            {
                LookupAutocompleteLoad(request.Grid);
            }
            else
            {
                if (request.ParentCell.DataRowIndex != null && request.ParentCell.FieldName != null)
                {
                    // Parameter parentCell to parentGrid cell instance
                    request.ParentCell = request.ParentGrid.RowCellList?.SelectMany(item => item).Where(item => item.DataRowIndex == request.ParentCell.DataRowIndex && item.FieldName == request.ParentCell.FieldName).FirstOrDefault() ?? request.ParentCell;
                }
                LookupAutocompleteSave(request.Grid, request.ParentCell, request.ParentGrid);
                return new() { Grid = request.Grid, ParentGrid = request.ParentGrid };
            }
        }
        // Column Lookup
        var parentCellIsColumn = request.ParentCell?.ControlList?.Where(item => item.ControlEnum == GridControlEnum.ButtonColumn).Any();
        if (request.ParentCell != null && parentCellIsColumn == true && request.ParentGrid != null)
        {
            if (request.Grid.RowCellList == null)
            {
                LookupColumnLoad(request.Grid, request.ParentCell, request.ParentGrid);
            }
            else
            {
                await LookupColumnSave(request.Grid, request.ParentCell, request.ParentGrid);
                return new() { Grid = request.Grid, ParentGrid = request.ParentGrid };
            }
        }
        return new GridResponseDto { Grid = request.Grid };
    }

    private void DataLoad(GridDto grid)
    {
        var propertyInfoList = typeof(ProductDto).GetProperties().ToList();
        if (grid.State?.ColumnList?.Count > 0)
        {
            var columnList = grid.State.ColumnList;
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
        var columnList = parentGrid.State?.ColumnList;
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

    private void LookupFilterLoad(GridDto grid, GridCellDto parentCell, GridDto parentGrid)
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
        var list = memoryGrid.LoadFilterLookup(grid, parentCell);
        grid.State.IsSelectMultiList.AddRange(new bool?[list.Count]);
        var filter = parentGrid.State?.FilterMultiList != null && parentCell.FieldName != null && parentGrid.State.FilterMultiList.TryGetValue(parentCell.FieldName, out var resultFilter) ? resultFilter : null;
        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];
            grid.RowCellList.Add(new List<GridCellDto>());
            grid.RowCellList.Last().Add(new GridCellDto { DataRowIndex = i, CellEnum = GridCellEnum.CheckboxSelectMulti });
            grid.RowCellList.Last().Add(new GridCellDto { Text = item.Text, DataRowIndex = i, CellEnum = GridCellEnum.Field });
            if (filter != null && item.Text != null && filter.TextList.Contains(item.Text))
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

    private async Task LookupFilterSave(GridDto grid, GridCellDto parentCell, GridDto parentGrid)
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
                var filter = parentGrid.State.FilterMultiList.TryGetValue(fieldName, out var resultFilter) ? resultFilter : null;
                if (filter == null)
                {
                    filter = new() { TextList = new() };
                    parentGrid.State.FilterMultiList[fieldName] = filter;
                }
                filter.TextList.Clear();
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
                                filter.TextList.Add(text);
                            }
                        }
                    }
                }
                if (filter.TextList.Count == 0)
                {
                    parentGrid.State.FilterMultiList.Remove(fieldName);
                }
            }
        }
        await Load(new() { Grid = parentGrid });
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
                parentGrid.State.ColumnList.Add(cell.Text!);
                i++;
            }
        }
        await Load(new() { Grid = parentGrid }); // TODO Column on lookup. For example filter would be missing.
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

public class GridRequestDto
{
    public GridDto Grid { get; set; } = default!;

    public GridCellDto? Cell { get; set; }

    public GridControlDto? Control { get; set; }

    public GridCellDto? ParentCell { get; set; }

    public GridControlDto? ParentControl { get; set; }

    public GridDto? ParentGrid { get; set; }
}

public class GridResponseDto
{
    public GridDto Grid { get; set; } = default!;

    /// <summary>
    /// Gets or sets ParentGrid. Can be used to reload parent grid when lookup is closed. See also GridControlEnum.ButtonLookupOk;
    /// </summary>
    public GridDto? ParentGrid { get; set; }

    public void ClearResponse()
    {
        if (Grid?.State?.ButtonCustomClick != null)
        {
            Grid.State.ButtonCustomClick = null;
        }
        if (Grid?.State?.Pagination?.PageIndexDeltaClick != null)
        {
            Grid.State.Pagination.PageIndexDeltaClick = null;
        }
    }
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

    public List<GridCellDto> AddRow()
    {
        RowCellList = RowCellList ?? new();
        var result = new List<GridCellDto>();
        RowCellList.Add(result);
        return result;
    }

    public GridCellDto AddCellControl(int? colSpan = null)
    {
        RowCellList = RowCellList ?? new();
        if (RowCellList.LastOrDefault() == null)
        {
            RowCellList.Add(new());
        }
        var cell = new GridCellDto() { CellEnum = GridCellEnum.Control, ColSpan = colSpan };
        RowCellList.Last().Add(cell);
        return cell;
    }

    public GridCellDto AddCell(GridCellDto cell)
    {
        RowCellList = RowCellList ?? new();
        if (RowCellList.LastOrDefault() == null)
        {
            RowCellList.Add(new());
        }
        RowCellList.Last().Add(cell);
        return cell;
    }

    public GridCellDto AddCell(GridCellDto cell, string? rowKey)
    {
        var result = AddCell(cell);
        UtilServer.Assert(cell.DataRowIndex != null, "Cell DataRowIndex can not be null when used with RowKey!");
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
        if (rowKey != null)
        {
            UtilServer.Assert(State.RowKeyList[dataRowIndex] == null || State.RowKeyList[dataRowIndex] == rowKey, "RowKey invalid!");
            State.RowKeyList[dataRowIndex] = rowKey;
        }
        return result;
    }

    public GridControlDto AddControl(GridControlDto control, int? dataRowIndex = null)
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
        if (dataRowIndex != null)
        {
            UtilServer.Assert(cell.DataRowIndex == null || cell.DataRowIndex == dataRowIndex, "DataRowIndex invalid!");
            cell.DataRowIndex = dataRowIndex;
        }
        return control;
    }
}

public class GridStateDto
{
    public GridStateSortDto? Sort { get; set; } // public List<GridStateSortDto> SortList { get; set; }

    /// <summary>
    /// (FieldName, Text)
    /// </summary>
    public Dictionary<string, string>? FilterList { get; set; }

    /// <summary>
    /// (FieldName, GridStateFilterMultiDto)
    /// </summary>
    public Dictionary<string, GridStateFilterMultiDto>? FilterMultiList { get; set; }

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

    public bool? IsSelectMultiAll { get; set; }

    /// <summary>
    /// Gets or sets IsSelectMultiIndeterminate. If true, three state checkbox shows indeterminate state.
    /// </summary>
    public bool? IsSelectMultiIndeterminate { get; set; }

    /// <summary>
    /// Gets or sets ColumnList. This is the list of columns to display. If null, display all columns.
    /// </summary>
    public List<string>? ColumnList { get; set; }

    /// <summary>
    /// Gets or sets ColumnFilterMulti. This is the columns to include or exclude from GridConfig.ColumnList.
    /// </summary>
    public GridStateFilterMultiDto? ColumnFilterMulti { get; set; }

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

    public List<FieldSaveDto>? FieldSaveList { get; set; }
}

public class FieldSaveDto
{
    public string? FieldName { get; set; }
 
    public int? DataRowIndex { get; set; }

    public string? Text { get; set; }

    public string? TextModified { get; set; }
}

public class GridPaginationDto
{
    public int? PageIndex { get; set; }

    public int? PageCount { get; set; }
    
    public int? PageSize { get; set; }

    public int? PageIndexDeltaClick { get; set; }
}

public class GridStateButtonCustomClickDto
{
    public string? Name { get; set; }

    public int? DataRowIndex { get; set; }

    public string? FieldName { get; set; }
}

public class GridStateSortDto
{
    public string FieldName { get; set; } = default!;

    public bool IsDesc { get; set; }
}

public class GridStateFilterMultiDto
{
    /// <summary>
    /// Gets TextList. Contains from user clicked filter lookup entries accross pagination. See also IsSelectMultiAll.
    /// </summary>
    public List<string?> TextList { get; set; } = default!;

    /// <summary>
    /// Gets or sets IsSelectAll. If true, include. If false, exclude TextList entries.
    /// </summary>
    public bool IsSelectAll { get; set; }
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

    public string? TextPlaceholder { get; set; }

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
    
    public string? TextModified { get; set; }

    public string? Name { get; set; }
}

/// <summary>
/// Grid cell is data column related. For example data field.
/// </summary>
public enum GridCellEnum
{
    None = 0,

    /// <summary>
    /// Data field.
    /// </summary>
    Field = 1,

    /// <summary>
    /// Column header for sort and filter lookup.
    /// </summary>
    Header = 2,

    /// <summary>
    /// Column header empty for example for command delete button. No sort on this column.
    /// </summary>
    HeaderEmpty = 3,

    /// <summary>
    /// Search field.
    /// </summary>
    Filter = 10,

    /// <summary>
    /// Search field empty for example for command delete button. No search on this column.
    /// </summary>
    FilterEmpty = 14,

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

/// <summary>
/// Grid control is not data related. For example lookup grid cancel button.
/// </summary>
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

public class GridFilterLookupDataRowDto
{
    public string? Text { get; set; }
}

public class GridColumnLookupDataRowDto
{
    public string? FieldName { get; set; }
}

public enum GridColumnEnum
{
    None = 0,

    Text = 1,
    
    Number = 2,
    
    Date = 3,
}

public class GridConfig
{
    /// <summary>
    /// Gets or sets ColumnList. This is all columns.
    /// </summary>
    public List<GridColumn> ColumnList { get; set; } = default!;

    /// <summary>
    /// Gets or sets PageSize. This is the number of rows in one page.
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// Returns column list to render data grid.
    /// </summary>
    public List<GridColumn> ColumnListGet(GridDto grid)
    {
        var result = ColumnList;
        if (grid.State?.ColumnList?.Count > 0)
        {
            result = result.Where(item => grid.State?.ColumnList?.Contains(item.FieldName!) == true).ToList();
        }
        result = result.OrderBy(item => item.Sort).ToList();
        return result;
    }
}

public class GridColumn
{
    public string? FieldName { get; set; }

    public GridColumnEnum ColumnEnum { get; set; }

    public int? Sort { get; set; }

    public bool? IsRowKey { get; set; }
}