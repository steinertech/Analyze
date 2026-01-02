using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;

public class CommandGrid(GridMemory memoryGrid, GridExcel excelGrid, GridStorage storageGrid, GridArticle articleGrid, GridArticle2 gridArticle)
{
    /// <summary>
    /// Returns loaded grid.
    /// </summary>
    public async Task<GridResponseDto> Load(GridRequestDto request, GridRequest2Dto request2)
    {
        // Article
        if (request.Grid.GridName == "Article")
        {
            await articleGrid.Load(request.Grid, request.ParentCell, request.ParentControl, request.ParentGrid, request);
            return new GridResponseDto { Grid = request.Grid };
        }
        // Article
        if (request.Grid.GridName == "Article2")
        {
            var response = await gridArticle.Load(request, request2);
            return response;
        }
        if (request.Grid.State?.FieldSaveList?.Count() > 0)
        {
            // Storage
            if (request.Grid.GridName == "Storage")
            {
                await storageGrid.Save(request.Grid, request.ParentCell, request.ParentControl, request.ParentGrid, request);
                return new() { Grid = request.Grid, ParentGrid = request.ParentGrid };
            }
            if (request.ParentCell == null)
            {
                var result = new GridResponseDto() { Grid = DataSave(request.Grid) };
                if (result.Grid?.State?.FieldSaveList != null)
                {
                    result.Grid.State.FieldSaveList = null;
                }
                await Load(request, request2);
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
                await LookupFilterSave(request.Grid, request.ParentCell, request.ParentGrid, request2);
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
                await LookupColumnSave(request.Grid, request.ParentCell, request.ParentGrid, request2);
                return new() { Grid = request.Grid, ParentGrid = request.ParentGrid };
            }
        }
        return new GridResponseDto { Grid = request.Grid };
    }

    /// <summary>
    /// Returns loaded grid.
    /// </summary>
    public async Task<GridResponse2Dto> Load2(GridRequest2Dto request)
    {
        GridResponse2Dto result;
        switch (request.Grid.GridName)
        {
            case "Article3":
                result = await gridArticle.Load2(request);
                break;
            case "ProductDto":
                result = await memoryGrid.Load2(request);
                break;
            case "Storage":
                result = await storageGrid.Load2(request);
                break;
            default:
                var resultLoad = await Load(new()
                {
                    Grid = request.Grid,
                    Cell = request.Cell,
                    Control = request.Control,
                    ParentGrid = request.ParentGrid,
                    ParentCell = request.ParentCell,
                    ParentControl = request.ParentControl
                }, request);
                result = new() { Grid = resultLoad.Grid, ParentGrid = resultLoad.ParentGrid };
                break;
        }
        // Clear
        if (result.Grid?.State != null)
        {
            result.Grid.State.FieldSaveList = null;
        }
        if (result.Grid?.State?.Pagination != null)
        {
            result.Grid.State.Pagination.PageIndexDeltaClick = null;
        }
        return result;
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
                    var dropdownList = DropdownLoad(grid.GridName, propertyInfo.Name);
                    grid.RowCellList.Last().Add(new GridCellDto { DataRowIndex = dataRowIndex, FieldName = propertyInfo.Name, Text = text, DropdownList = dropdownList, CellEnum = GridCellEnum.FieldDropdown });
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
            new() { ControlEnum = GridControlEnum.Label, Text = "(Select All)" },
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
            new() { ControlEnum = GridControlEnum.Label, Text = "(Select All)" },
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

    private async Task LookupFilterSave(GridDto grid, GridCellDto parentCell, GridDto parentGrid, GridRequest2Dto request2)
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
                    filter = new();
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
        await Load(new() { Grid = parentGrid }, request2);
    }

    private async Task LookupColumnSave(GridDto grid, GridCellDto parentCell, GridDto parentGrid, GridRequest2Dto request2)
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
        await Load(new() { Grid = parentGrid }, request2); // TODO Column on lookup. For example filter would be missing.
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

    private List<string> DropdownLoad(string gridName, string fieldName) // TODO Remove
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
    /// <summary>
    /// Gets Grid. This is the request grid sender.
    /// </summary>
    public GridDto Grid { get; set; } = default!;

    /// <summary>
    /// Gets Cell. This is the request cell sender.
    /// </summary>
    public GridCellDto? Cell { get; set; }

    /// <summary>
    /// Gets Control. This is the request control sender.
    /// </summary>
    public GridControlDto? Control { get; set; }

    /// <summary>
    /// Gets ParentCell. This is the lookup parent cell.
    /// </summary>
    public GridCellDto? ParentCell { get; set; }

    /// <summary>
    /// Gets ParentControl. This is the lookup parent control.
    /// </summary>
    public GridControlDto? ParentControl { get; set; }

    /// <summary>
    /// Gets ParentGrid. This is the lookup parent grid.
    /// </summary>
    public GridDto? ParentGrid { get; set; }

    public GridRequestDto Parent()
    {
        return new() { Grid = ParentGrid ?? throw new Exception(), Cell = ParentCell, Control = ParentControl };
    }
}

public class GridRequest2EntryDto
{
    public GridDto? Grid { get; set; }

    /// <summary>
    /// Gets Cell. This is the sender.
    /// </summary>
    public GridCellDto? Cell { get; set; }

    /// <summary>
    /// Gets Control. This is the sender.
    /// </summary>
    public GridControlDto? Control { get; set; }
}

/// <summary>
/// Request sent by GridEnum.
/// </summary>
public enum GridRequest2GridEnum
{
    None = 0,
    
    /// <summary>
    /// Request sent by main grid.
    /// </summary>
    Grid = 1,

    /// <summary>
    /// Request sent by filter grid.
    /// </summary>
    LookupFilter = 2,

    /// <summary>
    /// Request sent by column grid.
    /// </summary>
    LookupColumn = 3,

    /// <summary>
    /// Request sent by autocomplete grid.
    /// </summary>
    LookupAutocomplete = 4,

    /// <summary>
    /// Request sent by edit grid. See also method RenderForm.
    /// </summary>
    LookupEdit = 5,

    /// <summary>
    /// Request sent by sub grid. See also enum GridControlEnum.ButtonModal
    /// </summary>
    LookupSub = 6, // TODO Rename to LookupModal

    /// <summary>
    /// Request sent by confirm delete grid.
    /// </summary>
    LookupConfirmDelete = 7,
}

/// <summary>
/// Request sent by GridActionEnum. This is a subcategory of GridEnum.
/// </summary>
public enum GridRequest2GridActionEnum
{
    None = 0,

    /// <summary>
    /// User clicked grid save button.
    /// </summary>
    GridSave = 1,

    /// <summary>
    /// User clicked grid reload button.
    /// </summary>
    GridReload = 2,

    /// <summary>
    /// User clicked row delete button.
    /// </summary>
    GridDelete = 3,

    /// <summary>
    /// User clciked grid new row button.
    /// </summary>
    GridNew = 4,

    /// <summary>
    /// User clicked row delete confirm button open.
    /// </summary>
    LookupConfirmDeleteOpen = 5,

    /// <summary>
    /// User clicked row delete confirm button ok.
    /// </summary>
    LookupConfirmDeleteOk = 6,

    /// <summary>
    /// User clicked lookup edit form open button.
    /// </summary>
    LookupEditOpen = 7,

    /// <summary>
    /// User clicked save button on lookup edit form.
    /// </summary>
    LookupEditSave = 8,

    /// <summary>
    /// User clicked lookup autocomplete ok button.
    /// </summary>
    LookupAutocompleteOk = 9,

    /// <summary>
    /// User clicked lookup sub window save button. See also enum GridControlEnum.ButtonLookupOk
    /// </summary>
    LookupSubOk = 10,

    /// <summary>
    /// User clicked lookup sub window save button.
    /// </summary>
    LookupSubSave = 11,

    /// <summary>
    /// User clicked lookup sub window delete row button.
    /// </summary>
    LookupSubDelete = 12,

    /// <summary>
    /// User clicked lookup sub window new row button.
    /// </summary>
    LookupSubNew = 13,

    /// <summary>
    /// User clicked custom button.
    /// </summary>
    ButtonCustom = 14,

    /// <summary>
    /// User clicked modal custom button.
    /// </summary>
    ButtonModalCustom = 15,

    /// <summary>
    /// User clicked lookup column button.
    /// </summary>
    LookupColumnOpen = 16,

    /// <summary>
    /// User clicked a breadcrumb button.
    /// </summary>
    ButtonBreadcrumb = 17,
}

public class GridRequest2Dto
{
    public List<GridRequest2EntryDto> List { get; set; } = default!;

    private GridRequest2EntryDto? ListGet(int index)
    {
        return index >= 0 && index < List.Count ? List[index] : null;
    }

    [JsonIgnore]
    public GridDto Grid => ListGet(0 + offset)!.Grid!;

    [JsonIgnore]
    public GridCellDto? Cell => ListGet(0 + offset)?.Cell;

    [JsonIgnore]
    public GridControlDto? Control => ListGet(0 + offset)?.Control;

    [JsonIgnore]
    public GridDto? ParentGrid => ListGet(1 + offset)?.Grid;

    [JsonIgnore]
    public GridCellDto? ParentCell => ListGet(1 + offset)?.Cell;

    [JsonIgnore]
    public GridControlDto? ParentControl => ListGet(1 + offset)?.Control;

    // [JsonIgnore]
    // public GridDto? GreatParentGrid => List[2].Grid; // Request does not send GreatParent data grid

    [JsonIgnore]
    public GridDto? LookupGrid => ListGet(-1 + offset)?.Grid;

    [JsonIgnore]
    public GridCellDto? LookupCell => ListGet(-1 + offset)?.Cell;

    [JsonIgnore]
    public GridControlDto? LookupControl => ListGet(-1 + offset)?.Control;

    [JsonIgnore]
    public GridCellDto? GreatParentCell => ListGet(2 + offset)?.Cell;

    [JsonIgnore]
    public GridControlDto? GreatParentControl => ListGet(2 + offset)?.Control;

    public GridRequestDto Parent()
    {
        return new() { Grid = ParentGrid ?? throw new Exception(), Cell = ParentCell, Control = ParentControl, ParentCell = GreatParentCell, ParentControl = GreatParentControl };
    }

    private int offset = 0;

    public GridRequest2Dto Parent2()
    {
        var result = new GridRequest2Dto { List = List, offset = offset + 1 };
        return result;
    }

    private GridRequest2GridEnum? gridEnum;

    [JsonIgnore]
    public GridRequest2GridEnum GridEnum
    {
        get
        {
            if (gridEnum == null)
            {
                gridEnum = GridEnumGet(this);
            }
            return gridEnum.Value;
        }
    }

    private static GridRequest2GridEnum GridEnumGet(GridRequest2Dto request)
    {
        request = new GridRequest2Dto { List = request.List, offset = 0 };
        var result = GridRequest2GridEnum.None;
        if (request.ParentGrid == null)
        {
            UtilServer.Assert(result == GridRequest2GridEnum.None);
            result = GridRequest2GridEnum.Grid;
        }
        if (request.ParentCell?.CellEnum == GridCellEnum.Header)
        {
            UtilServer.Assert(result == GridRequest2GridEnum.None);
            result = GridRequest2GridEnum.LookupFilter;
        }
        if (request.ParentControl?.ControlEnum == GridControlEnum.ButtonColumn)
        {
            UtilServer.Assert(result == GridRequest2GridEnum.None);
            result = GridRequest2GridEnum.LookupColumn;
        }
        if (request.ParentCell?.CellEnum == GridCellEnum.FieldAutocomplete)
        {
            UtilServer.Assert(result == GridRequest2GridEnum.None);
            result = GridRequest2GridEnum.LookupAutocomplete;
        }
        if (request.ParentControl?.ControlEnum == GridControlEnum.ButtonModal && request.ParentControl?.Name == "Edit")
        {
            UtilServer.Assert(result == GridRequest2GridEnum.None);
            result = GridRequest2GridEnum.LookupEdit;
        }
        if (request.ParentControl?.ControlEnum == GridControlEnum.ButtonModal && request.ParentControl?.Name == "Delete" && request.Control?.ControlEnum == GridControlEnum.ButtonModal)
        {
            UtilServer.Assert(result == GridRequest2GridEnum.None);
            result = GridRequest2GridEnum.LookupConfirmDelete;
        }
        if (request.ParentControl?.ControlEnum == GridControlEnum.ButtonModal && request.ParentControl?.Name == "Delete" && request.Control?.ControlEnum == GridControlEnum.ButtonLookupOk)
        {
            UtilServer.Assert(result == GridRequest2GridEnum.None);
            result = GridRequest2GridEnum.LookupConfirmDelete;
        }
        if (request.ParentControl?.ControlEnum == GridControlEnum.ButtonModalCustom)
        {
            UtilServer.Assert(result == GridRequest2GridEnum.None);
            result = GridRequest2GridEnum.LookupSub;
        }
        return result;
    }

    private GridRequest2GridActionEnum? gridActionEnum;

    [JsonIgnore]
    public GridRequest2GridActionEnum GridActionEnum
    {
        get
        {
            if (gridActionEnum == null)
            {
                gridActionEnum = GridActionEnumGet(this);
            }
            return gridActionEnum.Value;
        }
    }

    private static GridRequest2GridActionEnum GridActionEnumGet(GridRequest2Dto request)
    {
        request = new GridRequest2Dto { List = request.List, offset = 0 };
        var result = GridRequest2GridActionEnum.None;
        switch (request.GridEnum)
        {
            case GridRequest2GridEnum.Grid:
                if (request.Control?.ControlEnum == GridControlEnum.ButtonSave)
                {
                    UtilServer.Assert(result == GridRequest2GridActionEnum.None);
                    result = GridRequest2GridActionEnum.GridSave;
                }
                if ((request.Control?.ControlEnum == GridControlEnum.Button || request.Control?.ControlEnum == GridControlEnum.ButtonModal) && request.Control.Name == "Delete")
                {
                    UtilServer.Assert(result == GridRequest2GridActionEnum.None);
                    result = GridRequest2GridActionEnum.GridDelete;
                }
                if (request.Control?.ControlEnum == GridControlEnum.Button && request.Control?.Name == "New")
                {
                    UtilServer.Assert(result == GridRequest2GridActionEnum.None);
                    result = GridRequest2GridActionEnum.GridNew;
                }
                if (request.Control?.ControlEnum == GridControlEnum.ButtonCustom)
                {
                    UtilServer.Assert(result == GridRequest2GridActionEnum.None);
                    result = GridRequest2GridActionEnum.ButtonCustom;
                }
                if (request.Control?.ControlEnum == GridControlEnum.Breadcrumb)
                {
                    UtilServer.Assert(result == GridRequest2GridActionEnum.None);
                    result = GridRequest2GridActionEnum.ButtonBreadcrumb;
                }
                break;
            case GridRequest2GridEnum.LookupConfirmDelete:
                if (request.Control?.ControlEnum == GridControlEnum.ButtonModal && request.Control.Name == "Delete")
                {
                    UtilServer.Assert(result == GridRequest2GridActionEnum.None);
                    result = GridRequest2GridActionEnum.LookupConfirmDeleteOpen;
                }
                if (request.Control?.ControlEnum == GridControlEnum.ButtonLookupOk && request.Control.Name == "Delete")
                {
                    UtilServer.Assert(result == GridRequest2GridActionEnum.None);
                    result = GridRequest2GridActionEnum.LookupConfirmDeleteOk;
                }
                break;
            case GridRequest2GridEnum.LookupEdit:
                if (request.Control?.ControlEnum == GridControlEnum.ButtonModal && request.Control.Name == "Edit")
                {
                    UtilServer.Assert(result == GridRequest2GridActionEnum.None);
                    result = GridRequest2GridActionEnum.LookupEditOpen;
                }
                if (request.Control?.ControlEnum == GridControlEnum.ButtonSave)
                {
                    UtilServer.Assert(result == GridRequest2GridActionEnum.None);
                    result = GridRequest2GridActionEnum.LookupEditSave;
                }
                break;
            case GridRequest2GridEnum.LookupAutocomplete:
                if (request.Control?.ControlEnum == GridControlEnum.ButtonLookupOk)
                {
                    UtilServer.Assert(result == GridRequest2GridActionEnum.None);
                    result = GridRequest2GridActionEnum.LookupAutocompleteOk;
                }
                break;
            case GridRequest2GridEnum.LookupSub:
                if (request.Control?.ControlEnum == GridControlEnum.ButtonLookupOk)
                {
                    UtilServer.Assert(result == GridRequest2GridActionEnum.None);
                    result = GridRequest2GridActionEnum.LookupSubOk;
                }
                if (request.Control?.ControlEnum == GridControlEnum.ButtonSave)
                {
                    UtilServer.Assert(result == GridRequest2GridActionEnum.None);
                    result = GridRequest2GridActionEnum.LookupSubSave;
                }
                if ((request.Control?.ControlEnum == GridControlEnum.Button || request.Control?.ControlEnum == GridControlEnum.ButtonModal) && request.Control.Name == "Delete")
                {
                    UtilServer.Assert(result == GridRequest2GridActionEnum.None);
                    result = GridRequest2GridActionEnum.LookupSubDelete;
                }
                if (request.Control?.ControlEnum == GridControlEnum.Button && request.Control?.Name == "New")
                {
                    UtilServer.Assert(result == GridRequest2GridActionEnum.None);
                    result = GridRequest2GridActionEnum.LookupSubNew;
                }
                if (request.Control?.ControlEnum == GridControlEnum.ButtonCustom)
                {
                    UtilServer.Assert(result == GridRequest2GridActionEnum.None);
                    result = GridRequest2GridActionEnum.ButtonCustom;
                }
                if (request.Control?.ControlEnum == GridControlEnum.ButtonModalCustom)
                {
                    UtilServer.Assert(result == GridRequest2GridActionEnum.None);
                    result = GridRequest2GridActionEnum.ButtonModalCustom;
                }
                if (request.Control?.ControlEnum == GridControlEnum.Breadcrumb)
                {
                    UtilServer.Assert(result == GridRequest2GridActionEnum.None);
                    result = GridRequest2GridActionEnum.ButtonBreadcrumb;
                }
                break;
            case GridRequest2GridEnum.LookupColumn:
                if (request.Control?.ControlEnum == GridControlEnum.ButtonColumn)
                {
                    UtilServer.Assert(result == GridRequest2GridActionEnum.None);
                    result = GridRequest2GridActionEnum.LookupColumnOpen;
                }
                break;
        }
        return result;
    }
}

public class GridResponse2Dto
{
    public List<GridDto?> List { get; set; } = default!;

    [JsonIgnore]
    public GridDto? Grid
    {
        get => List[0]!;
        set { List ??= new() { null, null }; List[0] = value; }
    }

    [JsonIgnore]
    public GridDto? ParentGrid
    {
        get => List[1]!;
        set { List ??= new() { null, null }; List[1] = value; }
    }

    // public GridDto? GrandParentGrid // Response never changes GrandParent

    // public GridDto? LookupGrid // TODO Response to open lookup grid. Currently App.Web knows based on button when to open lookup grid.
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

    public List<GridControlDto> ControlList() // TODO Remove
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
        // RowCellList.Clear();
        if (State != null)
        {
            State.RowKeyList = null;
            State.IsNewList = null;
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

    public GridCellDto AddCell2(GridCellDto cell, Dynamic dataRow, GridConfig config)
    {
        var result = AddCell(cell);
        UtilServer.Assert(cell.DataRowIndex != null, "Cell DataRowIndex can not be null if used with RowKey!");
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
        string? rowKey = null;
        if (config.FieldNameRowKey != null)
        {
            rowKey = dataRow[config.FieldNameRowKey]?.ToString();
        }
        if (rowKey != null)
        {
            UtilServer.Assert(State.RowKeyList[dataRowIndex] == null || State.RowKeyList[dataRowIndex] == rowKey, "RowKey invalid!");
            State.RowKeyList[dataRowIndex] = rowKey;
        }
        // IsNew
        var isNew = dataRow.IsNew;
        if (isNew)
        {
            State.IsNewList ??= new();
            if (!State.IsNewList.Contains(dataRowIndex))
            {
                State.IsNewList.Add(dataRowIndex);
            }
        }
        return result;
    }

    /// <summary>
    /// Gets or sets PatchList. Response can contain partial patch instead of full load.
    /// </summary>
    public List<GridPatchDto>? PatchList { get; set; }
}

public class GridPatchDto
{
    public string? ControlName { get; set; }

    public bool? IsDisabled { get; set; }
}

public class GridStateDto
{
    public List<GridStateSortDto>? SortList { get; set; }

    /// <summary>
    /// Returns distinct SortList.
    /// </summary>
    public List<GridStateSortDto> SortListGet()
    {
        var result = new List<GridStateSortDto>();
        if (SortList != null)
        {
            foreach (var sort in SortList)
            {
                if (result.Any(item => item.FieldName == sort.FieldName) == false)
                {
                    result.Add(new() { FieldName = sort.FieldName, IsDesc = sort.IsDesc });
                }
            }
        }
        return result;
    }

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
    public List<string>? ColumnList { get; set; } // TODO Remove

    /// <summary>
    /// Gets or sets ColumnFilterMulti. This is the columns to include or exclude from GridConfig.ColumnList.
    /// </summary>
    public GridStateFilterMultiDto? ColumnFilterMulti { get; set; }

    /// <summary>
    /// Gets or sets ColumnWidthList. Used to resize columns.
    /// </summary>
    public List<double?>? ColumnWidthList { get; set; }

    /// <summary>
    /// Gets or sets RowKeyList. This is typically the data primary key. See also property GridConfig.FieldNameRowKey for config. (DataRowIndex, RowKey)
    /// </summary>
    public List<string?>? RowKeyList { get; set; }

    /// <summary>
    /// Gets or sets IsNewList. DataRow IsNew if DataRowIndex is contained in this list. (DataRowIndex)
    /// </summary>
    public List<int>? IsNewList { get; set; }

    /// <summary>
    /// Gets or sets RowKeyMasterList. This value is set by a master grid on its data row selection. (GridName, RowKey)
    /// </summary>
    public Dictionary<string, string?>? RowKeyMasterList { get; set; }

    public GridPaginationDto? Pagination { get; set; }

    public List<GridCellDto>? FieldSaveList { get; set; }
    
    public List<FieldCustomSaveDto>? FieldCustomSaveList { get; set; }

    /// <summary>
    /// Gets or sets IsPatch. If true, grid is partially patched (reloaded) when for example a checkbox is selected. Instead of full reload.
    /// </summary>
    public bool? IsPatch { get; set; }

    public List<GridStatePathDto>? PathList { get; set; }

    /// <summary>
    /// Gets or sets PathModalIndex. This is the index of the last modal path segment. Used for example for breadcrumb.
    /// </summary>
    public int? PathModalIndex { get; set; }

    /// <summary>
    /// Adds path (or modal name)
    /// </summary>
    public void PathListAdd(GridStatePathDto value)
    {
        PathList ??= new();
        PathList.Add(value);
        PathModalIndex = PathModalIndexGet();
    }

    /// <summary>
    /// Returns path starting after last modal segment.
    /// </summary>
    /// <param name="offset">For example 1 to skip home path segment.</param>
    public string? PathGet(int offset = 0)
    {
        var index = PathModalIndexGet();
        var pathList = PathList?.Skip(index + 1).ToList();
        var result = new StringBuilder();
        if (pathList != null)
        {
            for (int i = 0; i < pathList.Count; i++)
            {
                var path = pathList[i];
                if (i >= offset)
                {
                    result.Append(path.Name + "/");
                }
            }
        }
        return result.Length > 0 ? result.ToString() : null;
    }

    /// <summary>
    /// Returns last modal path segment.
    /// </summary>
    public string? PathModalNameGet()
    {
        var result = PathList?.LastOrDefault(item => item.IsModal == true)?.Name;
        return result;
    }

    /// <summary>
    /// Returns index of last modal path segment.
    /// </summary>
    public int PathModalIndexGet()
    {
        var result = PathList?.Select((item, index) => new { Item = item, Index = index }).LastOrDefault(item => item.Item.IsModal == true)?.Index ?? -1;
        return result;
    }
}

public class GridStatePathDto
{
    /// <summary>
    /// Gets or sets Name. This is the path segment name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets IsModal. If true, path segment transiations into modal window.
    /// </summary>
    public bool? IsModal { get; set; }

    /// <summary>
    /// Gets or sets IsModalCustom. If true, path segment transitions into custom modal window.
    /// </summary>
    public bool? IsModalCustom { get; set; }

    public GridCellIconDto? Icon { get; set; }
}

public class FieldCustomSaveDto
{
    public GridCellDto? Cell { get; set; }

    public GridControlDto? Control { get; set; }
}

public class GridPaginationDto
{
    public int? PageIndex { get; set; }

    public int? PageCount { get; set; }
    
    public int? PageIndexDeltaClick { get; set; }
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
    public List<string?> TextList { get; set; } = new();

    /// <summary>
    /// Gets or sets IsSelectAll. If false, include TextList entries. If true, exclude TextList entries. 
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

    public List<string?>? DropdownList { get; set; }

    /// <summary>
    /// Gets or sets ControlList. Applicable for CellEnum.Control
    /// </summary>
    public List<GridControlDto>? ControlList { get; set; }

    public GridCellIconDto? IconLeft { get; set; }

    public GridCellIconDto? IconRight { get; set; }

    public int? ColSpan { get; set; }
    
    public int? RowSpan { get; set; }

    public bool? SortIsDesc { get; set; }

    public int? SortIndex { get; set; }
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

    public GridCellIconDto? Icon { get; set; }

    public bool? IsDisabled { get; set; }
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
    ButtonReload = 1,

    /// <summary>
    /// Data grid save button.
    /// </summary>
    ButtonSave = 2,

    /// <summary>
    /// Lookup window cancel button. If this grid is a lookup window it gets closed.
    /// </summary>
    ButtonLookupCancel = 3,

    /// <summary>
    /// Lookup window ok button. If this grid is a lookup window it gets saved and closed after.
    /// </summary>
    ButtonLookupOk = 4,

    /// <summary>
    /// Lookup window sort button.
    /// </summary>
    ButtonLookupSort = 5,

    /// <summary>
    /// Button to open column select lookup window.
    /// </summary>
    ButtonColumn = 6,

    /// <summary>
    /// Button like for example delete.
    /// </summary>
    Button = 7,

    /// <summary>
    /// Custom button like for example create folder.
    /// </summary>
    ButtonCustom = 8,

    /// <summary>
    /// Select all row checkbox.
    /// </summary>
    CheckboxSelectMultiAll = 9, // GridCellEnum.CheckboxSelectMulti is data cell specific. ControlEnum.CheckboxSelectMultiAll is not data cell specific.

    /// <summary>
    /// Custom readonly label.
    /// </summary>
    Label = 10,

    /// <summary>
    /// Custom text field.
    /// </summary>
    FieldCustom = 11,

    /// <summary>
    /// Opens a lookup modal window. Calls method CommandGrid.Load(); to get modal grid data.
    /// </summary>
    ButtonModal = 12,

    /// <summary>
    /// Opens a lookup modal window. Calls method CommandGrid.Load(); to get modal grid data.
    /// </summary>
    ButtonModalCustom = 13,

    /// <summary>
    /// Data grid pagination. See also GridPaginationDto.
    /// </summary>
    Pagination = 14,

    /// <summary>
    /// Breadcrumb navigation. See also PathList.
    /// </summary>
    Breadcrumb = 15,
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
    
    Int = 2,

    Double = 3,
    
    Date = 4,
}

public class GridConfig
{
    private Dictionary<string, GridColumn> columnList = new();

    /// <summary>
    /// Gets or sets ColumnList. This is all data grid columns. See also method ColumnGet();
    /// </summary>
    public List<GridColumn> ColumnList
    {
        get
        {
            return columnList.Values.ToList();
        }
        set
        {
            columnList = new(value.Select(item => new KeyValuePair<string, GridColumn>(item.FieldName, item)));
        }
    }

    /// <summary>
    /// Returns data column by name.
    /// </summary>
    public GridColumn ColumnGet(string fieldName)
    {
        return columnList[fieldName];
    }

    /// <summary>
    /// Returns sorted column list to render data grid.
    /// </summary>
    public List<GridColumn> ColumnListGet(GridDto grid)
    {
        var result = ColumnList;
        var columnFilterMulti = grid.State?.ColumnFilterMulti;
        var columnFilterMultiDefault = DefaultColumnFilterMulti;
        if (columnFilterMulti == null && columnFilterMultiDefault != null)
        {
            // Apply default config column filter
            grid.State ??= new();
            grid.State.ColumnFilterMulti = columnFilterMultiDefault;
            columnFilterMulti = grid.State?.ColumnFilterMulti;
        }
        if (columnFilterMulti != null)
        {
            var isSelectAll = columnFilterMulti.IsSelectAll;
            result = result.Where(item => isSelectAll ^ columnFilterMulti.TextList.Contains(item.FieldName)).ToList();
        }
        result = result.OrderBy(item => item.Sort).ThenBy(item => item.FieldName).ToList();
        return result;
    }

    /// <summary>
    /// Gets or sets PageSize. Number of rows in one page.
    /// </summary>
    public int PageSize { get; set; } = 2;

    /// <summary>
    /// Gets or sets PageSize. Number of rows in one page in lookup filter.
    /// </summary>
    public int PageSizeFilter { get; set; } = 3;

    /// <summary>
    /// Gets or sets PageSize. Number of rows in one page in lookup column.
    /// </summary>
    public int PageSizeColumn { get; set; } = 4;

    /// <summary>
    /// Gets or sets PageSize. Number of rows in one page in lookup autocomplete.
    /// </summary>
    public int PageSizeAutocomplete { get; set; } = 5;

    /// <summary>
    /// Gets or sets FieldNameRowKey. This is the key to identifiy a data record.
    /// </summary>
    public string? FieldNameRowKey { get; set; }

    public bool IsAllowNew { get; set; } = false;

    public bool IsAllowDelete { get; set; } = false;

    /// <summary>
    /// Gets or sets IsAllowEditForm. If true, data row gets an edit command button.
    /// </summary>
    public bool IsAllowEditForm { get; set; } = false;
    
    public bool IsAllowDeleteConfirm { get; set; } = false;

    /// <summary>
    /// Gets or sets DefaultSortList. This is the initial default sort configuration.
    /// </summary>
    public List<GridStateSortDto>? DefaultSortList { get; set; }

    /// <summary>
    /// Gets or sets DefaultColumnFilterMulti. This is the initial default columns configuration to include or exclude columns from GridConfig.ColumnList.
    /// </summary>
    public GridStateFilterMultiDto? DefaultColumnFilterMulti { get; set; }

    /// <summary>
    /// Gets or sets IsSelectMulti. If true, user can select individual rows with a checkbox.
    /// </summary>
    public bool? IsSelectMulti { get; set; }

    /// <summary>
    /// Gets or sets IsSelectMultiPatch. If true, grid is partially patched (reloaded) when a checkbox is selected. Instead of full reload.
    /// </summary>
    public bool? IsSelectMultiPatch { get; set; }

    public string? ConvertTo(string fieldName, object value)
    {
        var result = value?.ToString();
        return result == "" ? null : result;
    }

    public object? ConvertFrom(string fieldName, string? value)
    {
        var columnEnum = ColumnGet(fieldName).ColumnEnum;
        if (value == null)
        {
            return null;
        }
        object? result;
        switch (columnEnum)
        {
            case GridColumnEnum.Text:
                result = value;
                break;
            case GridColumnEnum.Int:
                result = int.Parse(value);
                break;
            case GridColumnEnum.Double:
                result = double.Parse(value);
                break;
            case GridColumnEnum.Date:
                result = DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                break;
            default:
                throw new Exception("Type unknown!");
        }
        return result;
    }
}

public enum GridConfigEnum
{
    None = 0,

    /// <summary>
    /// Load data grid.
    /// </summary>
    Grid = 1,

    /// <summary>
    /// Load lookup filter.
    /// </summary>
    GridFilter = 2,

    /// <summary>
    /// Load lookup column.
    /// </summary>
    GridColumn = 3,

    /// <summary>
    /// Load lookup autocomplete.
    /// </summary>
    GridAutocomplete = 4,
}

public class GridColumn
{
    public string FieldName { get; set; } = default!;

    /// <summary>
    /// Gets or sets FieldNameSortCustom. If not null, use this column for custom sorting.
    /// </summary>
    public string? FieldNameSortCustom { get; set; }

    public GridColumnEnum ColumnEnum { get; set; }

    /// <summary>
    /// Gets or sets Sort. This is the column order.
    /// </summary>
    public int? Sort { get; set; }

    public bool IsAllowModify { get; set; }

    /// <summary>
    /// Gets or sets IsAutocomplete. If true, autocomplete lookup window is opened with distinct values of this column (by default).
    /// </summary>
    public bool IsAutocomplete { get; set; }
    
    public bool IsDropdown { get; set; }
}