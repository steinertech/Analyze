public class GridBase
{
    /// <summary>
    /// Returns config to render data grid.
    /// </summary>
    protected virtual Task<GridConfig> Config()
    {
        GridConfig result = new() { ColumnList = new() };
        return Task.FromResult(result);
    }

    /// <summary>
    /// Returns column list for lookup grid.
    /// </summary>
    /// <param name="grid">Grid with state (filter, sort and pagination) to apply.</param>
    protected virtual async Task<List<GridColumn>> ColumnList(GridDto grid)
    {
        var config = await Config();
        var result = config.ColumnList;
        // Apply (filter, sort and pagination) from grid.
        var resultDynamic = UtilGrid.DynamicFrom(result, (dataRowFrom, dataRowTo) => { dataRowTo["FieldName"] = dataRowFrom.FieldName; });
        resultDynamic = await UtilGrid.GridLoad(resultDynamic, grid, null, config.PageSizeColumn);
        result = UtilGrid.DynamicTo<GridColumn>(resultDynamic, (dataRowFrom, dataRowTo) => { dataRowTo.FieldName = dataRowFrom["FieldName"]?.ToString(); });
        return result;
    }

    /// <summary>
    /// Returns data row list to render grid.
    /// </summary>
    protected virtual Task<List<Dynamic>> GridLoad(GridDto grid, string? fieldNameDistinct, int pageSize)
    {
        var result = new List<Dynamic>();
        return Task.FromResult(result);
    }

    protected virtual async Task<List<Dynamic>?> GridSave(GridDto grid, Func<Task<List<Dynamic>>> load, GridConfig config)
    {
        var dataRowList = await load();
        UtilGrid.GridSave(grid, dataRowList, config);
        return dataRowList;
    }

    public async Task<GridResponseDto> Load(GridRequestDto request)
    {
        // Save
        if (request.Grid.State?.FieldSaveList?.Count() > 0)
        {
            var config = await Config();
            var load = async () => await GridLoad(request.Grid, null, config.PageSize);
            var dataRowList = await GridSave(request.Grid, load, config);
            if (dataRowList == null)
            {
                dataRowList = await load();
            }
            UtilGrid.Render(request.Grid, dataRowList, config);
            request.Grid.State.FieldSaveList = null;
            return new GridResponseDto { Grid = request.Grid };
        }
        // Load Grid
        if (request.ParentCell == null)
        {
            var config = await Config();
            var dataRowList = await GridLoad(request.Grid, null, config.PageSize);
            UtilGrid.Render(request.Grid, dataRowList, config);
            return new GridResponseDto { Grid = request.Grid };
        }
        // Lookup Filter
        if (request.ParentCell?.CellEnum == GridCellEnum.Header && request.ParentCell.FieldName != null && request.ParentGrid != null)
        {
            // Button Ok
            if (request.Control?.ControlEnum == GridControlEnum.ButtonLookupOk)
            {
                // Filter Save (State)
                UtilGrid.LookupFilterSave(request.Grid, request.ParentGrid, request.ParentCell.FieldName);
                // Parent Grid Load
                var config = await Config();
                var dataRowList = await GridLoad(request.ParentGrid, null, config.PageSize);
                UtilGrid.Render(request.ParentGrid, dataRowList, config);
                return new GridResponseDto { ParentGrid = request.ParentGrid };
            }
            // Pagination, Filter
            if (request.Control?.ControlEnum == GridControlEnum.Pagination || request.Cell?.CellEnum == GridCellEnum.Filter)
            {
                // Filter Save (State)
                var isSave = UtilGrid.LookupFilterSave(request.Grid, request.ParentGrid, request.ParentCell.FieldName);
                // Filter Load
                var config = await Config();
                {
                    var fieldName = request.ParentCell.FieldName;
                    var dataRowList = await GridLoad(request.Grid, fieldNameDistinct: fieldName, config.PageSizeFilter);
                    UtilGrid.LookupFilterLoad(request.ParentGrid, request.Grid, dataRowList, fieldName);
                    UtilGrid.RenderLookup(request.Grid, dataRowList, fieldName: fieldName);
                }
                // Parent Grid Load
                if (isSave)
                {
                    var dataRowList = await GridLoad(request.ParentGrid, null, config.PageSize);
                    UtilGrid.Render(request.ParentGrid, dataRowList, config);
                }
                return new GridResponseDto { Grid = request.Grid, ParentGrid = isSave ? request.ParentGrid : null };
            }
            // Lookup Filter Load
            {
                var fieldName = request.ParentCell.FieldName;
                var config = await Config();
                var dataRowList = await GridLoad(request.Grid, fieldNameDistinct: fieldName, config.PageSizeFilter);
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
                // Lookup Column Save (State)
                UtilGrid.LookupFilterSave(request.Grid, request.ParentGrid, "FieldName", isFilterColumn: true);
                var config = await Config();
                var dataRowList = await GridLoad(request.ParentGrid, null, config.PageSize);
                UtilGrid.Render(request.ParentGrid, dataRowList, config);
                return new GridResponseDto { ParentGrid = request.ParentGrid };
            }
            if (request.Control?.ControlEnum == GridControlEnum.Pagination)
            {
                // Lookup Column Save (State)
                {
                    UtilGrid.LookupFilterSave(request.Grid, request.ParentGrid, "FieldName", isFilterColumn: true);
                    var config = await Config();
                    var dataRowList = await GridLoad(request.ParentGrid, null, config.PageSize);
                    UtilGrid.Render(request.ParentGrid, dataRowList, config);
                }
                // Lookup Column Load
                {
                    var dataRowList = await ColumnList(request.Grid);
                    var dataRowListDynamic = UtilGrid.DynamicFrom(dataRowList, (dataRowFrom, dataRowTo) => { dataRowTo["FieldName"] = dataRowFrom.FieldName; });
                    UtilGrid.LookupFilterLoad(request.ParentGrid, request.Grid, dataRowListDynamic, "FieldName", isFilterColumn: true);
                    UtilGrid.RenderLookup(request.Grid, dataRowListDynamic, "FieldName");
                }
                return new GridResponseDto { Grid = request.Grid, ParentGrid = request.ParentGrid };
            }
            {
                // Lookup Column Load
                var dataRowList = await ColumnList(request.Grid);
                var dataRowListDynamic = UtilGrid.DynamicFrom(dataRowList, (dataRowFrom, dataRowTo) => { dataRowTo["FieldName"] = dataRowFrom.FieldName; });
                UtilGrid.LookupFilterLoad(request.ParentGrid, request.Grid, dataRowListDynamic, "FieldName", isFilterColumn: true);
                UtilGrid.RenderLookup(request.Grid, dataRowListDynamic, "FieldName");
                return new GridResponseDto { Grid = request.Grid };
            }
        }
        // Autocomplete
        if (request.ParentCell?.CellEnum == GridCellEnum.FieldAutocomplete && request.ParentCell.FieldName != null)
        {
            var fieldName = request.ParentCell.FieldName;
            if (request.Control?.ControlEnum == GridControlEnum.ButtonLookupOk && request.Grid.State?.IsSelectList != null && request.Grid.State.RowKeyList != null && request.ParentGrid?.State != null)
            {
                var dataRowIndex = request.Grid.State.IsSelectList.Select((item, index) => (Value: item, Index: index)).Single(item => item.Value == true).Index;
                var text = request.Grid.State.RowKeyList[dataRowIndex];
                request.ParentGrid.State.FieldSaveList ??= new();
                request.ParentGrid.State.FieldSaveList.Add(new() { DataRowIndex = request.ParentCell.DataRowIndex, FieldName = fieldName, Text = request.ParentCell.Text, TextModified = text });
                var config = await Config();
                var load = async () => await GridLoad(request.ParentGrid, null, config.PageSize);
                var dataRowList = await GridSave(request.ParentGrid, load, config);
                if (dataRowList == null)
                {
                    dataRowList = await load();
                }
                UtilGrid.Render(request.ParentGrid, dataRowList, config);
                request.ParentGrid.State.FieldSaveList = null;
                return new GridResponseDto { ParentGrid = request.ParentGrid };
            }
            {
                var config = await Config();
                var dataRowList = await GridLoad(request.Grid, fieldNameDistinct: fieldName, config.PageSizeAutocomplete);
                UtilGrid.RenderAutocomplete(request.Grid, dataRowList, fieldName: fieldName);
                return new GridResponseDto { Grid = request.Grid };
            }
        }
        throw new Exception("Load failed!");
    }
}