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
    /// <param name="request">Grid with state (filter, sort and pagination) to apply.</param>
    protected virtual async Task<List<GridColumn>> ColumnList(GridRequestDto request)
    {
        var config = await Config();
        var result = config.ColumnList;
        // Apply (filter, sort and pagination) from grid.
        var resultDynamic = UtilGrid.DynamicFrom(result, (dataRowFrom, dataRowTo) => { dataRowTo["FieldName"] = dataRowFrom.FieldName; });
        resultDynamic = await UtilGrid.GridLoad(request, resultDynamic, null, config.PageSizeColumn);
        result = UtilGrid.DynamicTo<GridColumn>(resultDynamic, (dataRowFrom, dataRowTo) => { dataRowTo.FieldName = dataRowFrom["FieldName"]?.ToString()!; });
        return result;
    }

    /// <summary>
    /// Returns data row list to render grid.
    /// </summary>
    protected virtual Task<List<Dynamic>> GridLoad(GridRequestDto request, string? fieldNameDistinct, int pageSize)
    {
        var result = new List<Dynamic>();
        return Task.FromResult(result);
    }

    /// <summary>
    /// Returns data row list to render grid.
    /// </summary>
    protected virtual Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, string? fieldNameDistinct, int pageSize)
    {
        var result = new List<Dynamic>();
        return Task.FromResult(result);
    }

    protected virtual Task GridSave(GridRequestDto request, GridConfig config)
    {
        return Task.CompletedTask;
    }

    public async Task<GridResponseDto> Load(GridRequestDto request, GridRequest2Dto request2)
    {
        // Save
        if (request.Grid.State?.FieldSaveList?.Count() > 0)
        {
            var config = await Config();
            await GridSave(request, config);
            var dataRowList = await GridLoad(request, null, config.PageSize);
            UtilGrid.Render(request, dataRowList, config);
            request.Grid.State.FieldSaveList = null;
            return new GridResponseDto { Grid = request.Grid };
        }
        // Save (Delete)
        if (request.Control?.ControlEnum == GridControlEnum.ButtonCustom && request.Control?.Name == "Delete")
        {
            var config = await Config();
            await GridSave(request, config);
            var dataRowList = await GridLoad(request, null, config.PageSize);
            UtilGrid.Render(request, dataRowList, config);
            return new GridResponseDto { Grid = request.Grid };
        }
        // Save (Delete) Modal
        if (request.Control?.ControlEnum == GridControlEnum.ButtonModal && request.Control.Name == "Delete")
        {
            request.Grid.Clear();
            request.Grid.AddControl(new() { ControlEnum = GridControlEnum.LabelCustom, Text = "Delete row?" });
            request.Grid.AddRow();
            request.Grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupOk, Name = "Delete" });
            request.Grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupCancel });
            return new GridResponseDto { Grid = request.Grid };
        }
        // Save (Delete) Modal
        if (request.Control?.ControlEnum == GridControlEnum.ButtonLookupOk && request.Control.Name == "Delete" && request.ParentGrid != null)
        {
            var requestParent = new GridRequestDto { Grid = request.ParentGrid, Cell = request.ParentCell, Control = request.ParentControl };
            var config = await Config();
            await GridSave(request.Parent(), config);
            var dataRowList = await GridLoad(request, null, config.PageSize);
            UtilGrid.Render(request.Parent(), dataRowList, config);
            return new GridResponseDto { ParentGrid = request.ParentGrid };
        }
        // Load Grid
        if (request.ParentCell == null)
        {
            var config = await Config();
            var dataRowList = await GridLoad(request, null, config.PageSize);
            if (request.Control?.ControlEnum == GridControlEnum.ButtonCustom && request.Control?.Name == "New")
            {
                dataRowList.Insert(0, Dynamic.Create(config));
            }
            UtilGrid.Render(request, dataRowList, config);
            return new GridResponseDto { Grid = request.Grid };
        }
        // Lookup Filter
        if (request.ParentCell?.CellEnum == GridCellEnum.Header && request.ParentCell.FieldName != null && request.ParentGrid != null)
        {
            // Button Ok
            if (request.Control?.ControlEnum == GridControlEnum.ButtonLookupOk)
            {
                // Filter Save (State)
                UtilGrid.LookupFilterSave(request, request.ParentCell.FieldName);
                // Parent Grid Load
                var config = await Config();
                var dataRowList = await GridLoad(request.Parent(), null, config.PageSize);
                UtilGrid.Render(request.Parent(), dataRowList, config);
                return new GridResponseDto { ParentGrid = request.ParentGrid };
            }
            // Pagination, Filter
            if (request.Control?.ControlEnum == GridControlEnum.Pagination || request.Cell?.CellEnum == GridCellEnum.Filter)
            {
                // Filter Save (State)
                var isSave = UtilGrid.LookupFilterSave(request, request.ParentCell.FieldName);
                // Filter Load
                var config = await Config();
                {
                    var fieldName = request.ParentCell.FieldName;
                    var dataRowList = await GridLoad(request, fieldNameDistinct: fieldName, config.PageSizeFilter);
                    UtilGrid.LookupFilterLoad(request, dataRowList, fieldName);
                    UtilGrid.RenderLookup(request, dataRowList, fieldName: fieldName);
                }
                // Parent Grid Load
                if (isSave)
                {
                    var dataRowList = await GridLoad(request.Parent(), null, config.PageSize);
                    UtilGrid.Render(request.Parent(), dataRowList, config);
                }
                return new GridResponseDto { Grid = request.Grid, ParentGrid = isSave ? request.ParentGrid : null };
            }
            // Lookup Filter Load
            {
                var fieldName = request.ParentCell.FieldName;
                var config = await Config();
                var dataRowList = await GridLoad(request, fieldNameDistinct: fieldName, config.PageSizeFilter);
                UtilGrid.LookupFilterLoad(request, dataRowList, fieldName);
                UtilGrid.RenderLookup(request, dataRowList, fieldName: fieldName);
                return new GridResponseDto { Grid = request.Grid };
            }
        }
        // Lookup Column
        if (request.ParentControl?.ControlEnum == GridControlEnum.ButtonColumn && request.ParentGrid != null)
        {
            if (request.Control?.ControlEnum == GridControlEnum.ButtonLookupOk)
            {
                // Lookup Column Save (State)
                UtilGrid.LookupFilterSave(request, "FieldName", isFilterColumn: true);
                var config = await Config();
                var dataRowList = await GridLoad(request.Parent(), null, config.PageSize);
                UtilGrid.Render(request2.Parent(), dataRowList, config);
                return new GridResponseDto { ParentGrid = request.ParentGrid };
            }
            if (request.Control?.ControlEnum == GridControlEnum.Pagination)
            {
                // Lookup Column Save (State)
                {
                    UtilGrid.LookupFilterSave(request, "FieldName", isFilterColumn: true);
                    var config = await Config();
                    var dataRowList = await GridLoad(request.Parent(), null, config.PageSize);
                    UtilGrid.Render(request.Parent(), dataRowList, config);
                }
                // Lookup Column Load
                {
                    var dataRowList = await ColumnList(request);
                    var dataRowListDynamic = UtilGrid.DynamicFrom(dataRowList, (dataRowFrom, dataRowTo) => { dataRowTo["FieldName"] = dataRowFrom.FieldName; });
                    UtilGrid.LookupFilterLoad(request, dataRowListDynamic, "FieldName", isFilterColumn: true);
                    UtilGrid.RenderLookup(request, dataRowListDynamic, "FieldName");
                }
                return new GridResponseDto { Grid = request.Grid, ParentGrid = request.ParentGrid };
            }
            {
                // Lookup Column Load
                var dataRowList = await ColumnList(request);
                var dataRowListDynamic = UtilGrid.DynamicFrom(dataRowList, (dataRowFrom, dataRowTo) => { dataRowTo["FieldName"] = dataRowFrom.FieldName; });
                UtilGrid.LookupFilterLoad(request, dataRowListDynamic, "FieldName", isFilterColumn: true);
                UtilGrid.RenderLookup(request, dataRowListDynamic, "FieldName");
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
                await GridSave(request.Parent(), config);
                var dataRowList = await GridLoad(request.Parent(), null, config.PageSize);
                UtilGrid.Render(request.Parent(), dataRowList, config);
                request.ParentGrid.State.FieldSaveList = null;
                return new GridResponseDto { ParentGrid = request.ParentGrid };
            }
            {
                var config = await Config();
                var dataRowList = await GridLoad(request, fieldNameDistinct: fieldName, config.PageSizeAutocomplete);
                UtilGrid.RenderAutocomplete(request, dataRowList, fieldName: fieldName);
                return new GridResponseDto { Grid = request.Grid };
            }
        }
        // Lookup Modal Edit
        if (request.ParentControl?.ControlEnum == GridControlEnum.ButtonModal && request.ParentControl?.Name == "Edit")
        {
            var config = await Config();
            var dataRowList = await GridLoad(request, null, config.PageSize);
            UtilGrid.Render(request, dataRowList, config);
            return new GridResponseDto { Grid = request.Grid };
        }
        // Lookup Modal
        if (request.ParentControl?.ControlEnum == GridControlEnum.ButtonModal && request.ParentControl?.Name == "Open")
        {
            var config = await Config();
            var dataRowList = await GridLoad(request, null, config.PageSize);
            if (request.Control?.ControlEnum == GridControlEnum.ButtonCustom && request.Control?.Name == "New")
            {
                dataRowList.Insert(0, Dynamic.Create(config));
            }
            UtilGrid.Render(request, dataRowList, config);
            return new GridResponseDto { Grid = request.Grid };
        }
        throw new Exception("Load failed!");
    }

    public async Task<GridResponse2Dto> Load2(GridRequest2Dto request)
    {
        switch (request.GridEnum)
        {
            case GridRequest2GridEnum.Grid:
                var config = await Config();
                var dataRowList = await GridLoad2(request, null, config.PageSize);
                UtilGrid.Render2(request, dataRowList, config);
                var result = new GridResponse2Dto { Grid = request.Grid };
                return result;
            default:
                throw new Exception();
        }
    }
}