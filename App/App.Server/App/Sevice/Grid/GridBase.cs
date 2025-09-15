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
        // Apply (filter, sort and pagination) from grid.
        var resultDynamic = UtilGrid.DynamicFrom(result, (dataRowFrom, dataRowTo) => { dataRowTo["FieldName"] = dataRowFrom.FieldName; });
        resultDynamic = await UtilGrid.LoadDataRowList(resultDynamic, grid, null, null);
        result = UtilGrid.DynamicTo<GridColumn>(resultDynamic, (dataRowFrom, dataRowTo) => { dataRowTo.FieldName = dataRowFrom["FieldName"]?.ToString(); });
        return result;
    }

    /// <summary>
    /// Returns data row list to render grid.
    /// </summary>
    protected virtual Task<List<Dynamic>> LoadDataRowList(GridDto grid, string? filterFieldName, GridConfig? config)
    {
        var result = new List<Dynamic>();
        return Task.FromResult(result);
    }

    public async Task<GridResponseDto> Load(GridRequestDto request)
    {
        // Load Grid
        if (request.ParentCell == null)
        {
            var config = await LoadConfig();
            var columnList = config.ColumnListGet(request.Grid);
            var dataRowList = await LoadDataRowList(request.Grid, null, config);
            UtilGrid.Render(request.Grid, dataRowList, columnList);
            return new GridResponseDto { Grid = request.Grid };
        }
        // Lookup Filter
        if (request.ParentCell?.CellEnum == GridCellEnum.Header && request.ParentCell.FieldName != null && request.ParentGrid != null )
        {
            // Button Ok
            if (request.Control?.ControlEnum == GridControlEnum.ButtonLookupOk)
            {
                // Filter Save (State)
                UtilGrid.LookupFilterSave(request.Grid, request.ParentGrid, request.ParentCell.FieldName);
                // Parent Grid Load
                var config = await LoadConfig();
                var columnList = config.ColumnListGet(request.ParentGrid);
                var dataRowList = await LoadDataRowList(request.ParentGrid, null, null);
                UtilGrid.Render(request.ParentGrid, dataRowList, columnList);
                return new GridResponseDto { ParentGrid = request.ParentGrid };
            }
            // Pagination
            if (request.Control?.ControlEnum == GridControlEnum.Pagination)
            {
                // Filter Save (State)
                var isSave = UtilGrid.LookupFilterSave(request.Grid, request.ParentGrid, request.ParentCell.FieldName);
                // Filter Load
                var config = await LoadConfig();
                {
                    var fieldName = request.ParentCell.FieldName;
                    var dataRowList = await LoadDataRowList(request.Grid, filterFieldName: fieldName, null);
                    UtilGrid.LookupFilterLoad(request.ParentGrid, request.Grid, dataRowList, fieldName);
                    UtilGrid.RenderLookup(request.Grid, dataRowList, fieldName: fieldName);
                }
                // Parent Grid Load
                if (isSave)
                {
                    var columnList = config.ColumnListGet(request.ParentGrid);
                    var dataRowList = await LoadDataRowList(request.ParentGrid, null, null);
                    UtilGrid.Render(request.ParentGrid, dataRowList, columnList);
                }
                return new GridResponseDto { Grid = request.Grid, ParentGrid = isSave ? request.ParentGrid : null };
            }
            // Lookup Filter Load
            {
                var fieldName = request.ParentCell.FieldName;
                var config = await LoadConfig();
                var dataRowList = await LoadDataRowList(request.Grid, filterFieldName: fieldName, null);
                UtilGrid.LookupFilterLoad(request.ParentGrid, request.Grid, dataRowList, fieldName);
                UtilGrid.RenderLookup(request.Grid, dataRowList, fieldName: fieldName);
                return new GridResponseDto { Grid = request.Grid };
            }
        }
        // Lookup Column
        if (request.ParentControl?.ControlEnum == GridControlEnum.ButtonColumn && request.ParentGrid != null)
        {
            if (request.Control?.ControlEnum == GridControlEnum.ButtonLookupOk)
            {
                // Lookup Column Save
                UtilGrid.LookupColumnSave(request.Grid, request.ParentGrid);
                var config = await LoadConfig();
                var columnList = config.ColumnListGet(request.ParentGrid);
                var dataRowList = await LoadDataRowList(request.ParentGrid, null, null);
                UtilGrid.Render(request.ParentGrid, dataRowList, columnList);
                return new GridResponseDto { ParentGrid = request.ParentGrid };
            }
            {
                // Lookup Column Load
                var dataRowList = await LoadColumnList(request.Grid);
                var dataRowListDynamic = UtilGrid.DynamicFrom(dataRowList, (dataRowFrom, dataRowTo) => { dataRowTo["FieldName"] = dataRowFrom.FieldName; });
                UtilGrid.LookupColumnLoad(request.ParentGrid, request.Grid, dataRowListDynamic);
                UtilGrid.RenderLookup(request.Grid, dataRowListDynamic, "FieldName");
                return new GridResponseDto { Grid = request.Grid };
            }
        }
        throw new Exception("Load failed!");
    }
}