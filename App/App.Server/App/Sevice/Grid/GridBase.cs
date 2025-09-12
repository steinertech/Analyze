public class GridBase
{
    /// <summary>
    /// Returns config to render data grid.
    /// </summary>
    protected virtual Task<GridConfig> LoadConfig()
    {
        GridConfig result = new() { ColumnList = new() };
        return Task.FromResult(result);
    }

    /// <summary>
    /// Returns column list for lookup grid.
    /// </summary>
    /// <param name="grid">Grid with state (filter, sort and pagination) to apply.</param>
    protected virtual async Task<List<GridColumn>> LoadColumnList(GridDto grid)
    {
        var config = await LoadConfig();
        var result = config.ColumnList;
        // Apply (filter, sort and pagination)
        var resultDynamic = UtilGrid.DynamicFrom(result, (dataRowFrom, dataRowTo) => { dataRowTo["FieldName"] = dataRowFrom.FieldName; });
        resultDynamic = await UtilGrid.LoadDataRowList(resultDynamic, grid, null);
        result = UtilGrid.DynamicTo<GridColumn>(resultDynamic, (dataRowFrom, dataRowTo) => { dataRowTo.FieldName = dataRowFrom["FieldName"]?.ToString(); });
        return result;
    }

    /// <summary>
    /// Returns data row list to render grid.
    /// </summary>
    protected virtual Task<List<Dictionary<string, object?>>> LoadDataRowList(GridDto grid, string? lookupFieldName)
    {
        var result = new List<Dictionary<string, object?>>();
        return Task.FromResult(result);
    }

    public async Task<GridResponseDto> Load(GridRequestDto request)
    {
        if (request.ParentCell == null)
        {
            // Load Grid
            var columnList = (await LoadConfig()).ColumnListGet(request.Grid);
            var dataRowList = await LoadDataRowList(request.Grid, null);
            UtilGrid.Render(request.Grid, dataRowList, columnList);
        }
        else
        {
            if (request.ParentCell?.CellEnum == GridCellEnum.Header && request.ParentGrid != null && request.ParentCell.FieldName != null)
            {
                if (request.Control?.ControlEnum == GridControlEnum.ButtonLookupOk && request.ParentGrid != null)
                {
                    UtilGrid.SaveHeaderLookup(request.Grid, request.ParentGrid, request.ParentCell.FieldName);
                    var parentColumnList = (await LoadConfig()).ColumnListGet(request.ParentGrid);
                    var parentDataRowList = await LoadDataRowList(request.ParentGrid, null);
                    UtilGrid.Render(request.ParentGrid, parentDataRowList, parentColumnList);
                    return new GridResponseDto { ParentGrid = request.ParentGrid };
                }
                // Load Grid Header Lookup
                var dataRowList = await LoadDataRowList(request.Grid, lookupFieldName: request.ParentCell.FieldName);
                UtilGrid.RenderCheckboxLookup(request.Grid, dataRowList, fieldName: request.ParentCell.FieldName);
            }
            else
            {
                if (request.ParentCell?.ControlList?.Where(item => item.ControlEnum == GridControlEnum.ButtonColumn).Any() == true)
                {
                    if (request.Control?.ControlEnum == GridControlEnum.ButtonLookupOk && request.ParentGrid != null)
                    {
                        UtilGrid.SaveColumnLookup(request.Grid, request.ParentGrid);
                        var parentColumnList = (await LoadConfig()).ColumnListGet(request.ParentGrid);
                        var parentDataRowList = await LoadDataRowList(request.ParentGrid, null);
                        UtilGrid.Render(request.ParentGrid, parentDataRowList, parentColumnList);
                        return new GridResponseDto { ParentGrid = request.ParentGrid };
                    }
                    // Load Grid Column
                    var dataRowList = await LoadColumnList(request.Grid);
                    var dataRowListDynamic = UtilGrid.DynamicFrom(dataRowList, (dataRowFrom, dataRowTo) => { dataRowTo["FieldName"] = dataRowFrom.FieldName; });
                    request.Grid.State ??= new();
                    request.Grid.State.IsSelectMultiList = new();
                    foreach (var dataRow in dataRowListDynamic)
                    {
                        var fieldName = dataRow["FieldName"];
                        bool isSelect = request.ParentGrid?.State?.ColumnList?.Contains(fieldName) == true;
                        request.Grid.State.IsSelectMultiList.Add(isSelect);
                    }
                    UtilGrid.RenderCheckboxLookup(request.Grid, dataRowListDynamic, "FieldName");
                }
                else
                {
                    throw new Exception("Load failed!");
                }
            }
        }
        return new GridResponseDto { Grid = request.Grid };
    }
}