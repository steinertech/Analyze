public class GridBase
{
    /// <summary>
    /// Returns config to render data grid.
    /// </summary>
    protected virtual Task<GridConfig> Config() // TODO Remove
    {
        var result = new GridConfig();
        return Task.FromResult(result);
    }

    /// <summary>
    /// Returns config to render data grid.
    /// </summary>
    protected virtual Task<GridConfig> Config2(GridRequest2Dto request)
    {
        var result = new GridConfig();
        return Task.FromResult(result);
    }

    /// <summary>
    /// Returns column list for lookup grid.
    /// </summary>
    /// <param name="request">Grid with state (filter, sort and pagination) to apply.</param>
    protected virtual async Task<List<GridColumn>> ColumnList(GridRequestDto request) // TODO Remove
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
    /// Returns column list for lookup grid.
    /// </summary>
    /// <param name="request">Grid with state (filter, sort and pagination) to apply.</param>
    internal async Task<List<GridColumn>> ColumnList2(GridRequest2Dto request)
    {
        var config = await Config2(request.Parent2());
        var result = config.ColumnList;
        // Apply (filter, sort and pagination) from grid.
        var resultDynamic = UtilGrid.DynamicFrom(result, (dataRowFrom, dataRowTo) => { dataRowTo["FieldName"] = dataRowFrom.FieldName; });
        resultDynamic = await UtilGrid.GridLoad2(request, resultDynamic, null, config, GridConfigEnum.GridColumn);
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
    /// Returns patch list. Allows partial patch instead of full reload. Used for example to update control IsDisable. Subsequent load and render is skippet.
    /// </summary>
    protected virtual Task<List<GridPatchDto>> GridLoad2Patch(GridRequest2Dto request)
    {
        return Task.FromResult<List<GridPatchDto>>(new());
    }

    /// <summary>
    /// Returns data row list to render grid.
    /// </summary>
    protected virtual Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, string? fieldNameDistinct, GridConfig config, GridConfigEnum configEnum, string? modalName)
    {
        return Task.FromResult<List<Dynamic>>(new());
    }

    protected virtual Task GridSave(GridRequestDto request, GridConfig config)
    {
        return Task.CompletedTask;
    }

    protected virtual Task GridSave2(GridRequest2Dto request, List<Dynamic> sourceList, GridConfig config) // TODO Rename to Save2
    {
        // UtilGrid.GridSave2(sourceList, destList, config);

        return Task.CompletedTask;
    }

    /// <summary>
    /// User clicked custom button or modified custom field.
    /// </summary>
    protected virtual Task GridSave2Custom(GridRequest2Dto request, GridButtonCustom? buttonCustomClick, List<FieldCustomSaveDto> fieldCustomSaveList, string? modalName) 
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
        if (request.Control?.ControlEnum == GridControlEnum.Button && request.Control?.Name == "Delete")
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
            request.Grid.AddControl(new() { ControlEnum = GridControlEnum.Label, Text = "Delete row?" });
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
            if (request.Control?.ControlEnum == GridControlEnum.Button && request.Control?.Name == "New")
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
                UtilGrid.RenderLookupAutocomplete(request, dataRowList, fieldName: fieldName);
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
        if (request.ParentControl?.ControlEnum == GridControlEnum.ButtonModal && request.ParentControl?.Name == "Sub")
        {
            var config = await Config();
            var dataRowList = await GridLoad(request, null, config.PageSize);
            if (request.Control?.ControlEnum == GridControlEnum.Button && request.Control?.Name == "New")
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
            // Grid
            case GridRequest2GridEnum.Grid:
            case GridRequest2GridEnum.LookupSub:
                {
                    if (request.GridEnum == GridRequest2GridEnum.LookupSub)
                    {
                        ArgumentNullException.ThrowIfNull(request.ParentGrid);
                    }
                    // Reset State on Reload
                    if (request.GridActionEnum == GridRequest2GridActionEnum.GridReload)
                    {
                        var state = request.Grid.State;
                        request.Grid.State = new(); // Clear State
                        request.Grid.State.PathList = state?.PathList; // Keep
                        request.Grid.State.PathModalIndex = state?.PathModalIndex; // Keep
                        request.Grid.State.RowKeyMasterList = state?.RowKeyMasterList; // Keep
                    }
                    // Reset SelectMulti on Pagination
                    bool isSelectMultiReset = 
                        request.GridActionEnum == GridRequest2GridActionEnum.Pagination || 
                        request.GridActionEnum == GridRequest2GridActionEnum.Header || 
                        request.GridActionEnum == GridRequest2GridActionEnum.Filter;
                    if (isSelectMultiReset)
                    {
                        if (request.Grid.State?.IsSelectMultiList != null)
                        {
                            request.Grid.State.IsSelectMultiList = null;
                        }
                    }
                    // Breadcrumb
                    if (request.GridActionEnum == GridRequest2GridActionEnum.ButtonBreadcrumb)
                    {
                        var text = request.Control?.Text;
                        if (text != null)
                        {
                            var index = int.Parse(text);
                            var pathSourceList = request.Grid.StateGet().PathList;
                            var pathModalIndex = request.Grid.StateGet().PathModalIndexGet();
                            var pathDestList = pathSourceList?.Take(pathModalIndex + 1 + index + 1).ToList();
                            request.Grid.StateGet().PathList = pathDestList;
                        }
                    }
                    if (request.GridActionEnum == GridRequest2GridActionEnum.ButtonModalCustom)
                    {
                        request.Grid.StateGet().PathListAdd(new() { Name = request.Control?.Name, IsModal = true, IsModalCustom = true });
                    }
                    var config = await Config2(request);
                    var modalName = request.Grid.State?.PathModalNameGet();
                    var buttonCustomClick = request.GridActionEnum == GridRequest2GridActionEnum.ButtonCustom ? new GridButtonCustom() { Cell = request.Cell!, Control = request.Control! } : null;
                    var fieldCustomSaveList = request.Grid.State?.FieldCustomSaveList ?? new();
                    // Save
                    var isSave =
                        request.GridActionEnum == GridRequest2GridActionEnum.GridSave ||
                        request.GridActionEnum == GridRequest2GridActionEnum.LookupSubSave ||
                        request.GridActionEnum == GridRequest2GridActionEnum.GridDelete ||
                        request.GridActionEnum == GridRequest2GridActionEnum.LookupSubOk;
                    if (isSave)
                    {
                        var sourceList = UtilGrid.GridSave2(request, config);
                        if (sourceList.Count() > 0)
                        {
                            await GridSave2(request, sourceList, config);
                        }
                    }
                    // Load Patch
                    var isPatch =
                        request.Cell?.CellEnum == GridCellEnum.CheckboxSelectMulti ||
                        request.Control?.ControlEnum == GridControlEnum.CheckboxSelectMultiAll ||
                        (request.Control?.ControlEnum == GridControlEnum.ButtonCustom && request.Control?.IsPatch == true);
                    if (isPatch)
                    {
                        var patchList = await GridLoad2Patch(request);
                        request.Grid.PatchList = patchList;
                    }
                    else
                    {
                        // Save Custom
                        var isSaveCustom = buttonCustomClick != null || fieldCustomSaveList.Count() > 0;
                        if (isSaveCustom)
                        {
                            await GridSave2Custom(request, buttonCustomClick, fieldCustomSaveList, modalName);
                        }
                        // Lookup Button Ok
                        if (request.Control?.ControlEnum == GridControlEnum.ButtonLookupOk)
                        {
                            modalName = request.ParentGrid?.State?.PathModalNameGet();
                            // Load
                            var dataRowList = await GridLoad2(request.Parent2(), null, config, GridConfigEnum.Grid, modalName);
                            // Render
                            GridRender2(request.Parent2(), dataRowList, config, modalName);
                            return new GridResponse2Dto { ParentGrid = request.ParentGrid };
                        }
                        {
                            // Load
                            var dataRowList = await GridLoad2(request, null, config, GridConfigEnum.Grid, modalName);
                            if (request.GridActionEnum == GridRequest2GridActionEnum.GridNew || request.GridActionEnum == GridRequest2GridActionEnum.LookupSubNew)
                            {
                                if (config.IsAllowNew)
                                {
                                    dataRowList.Insert(0, Dynamic.Create(config, isNew: true)); // Multi new data rows possible
                                }
                            }
                            // Render
                            if (request.GridActionEnum != GridRequest2GridActionEnum.LookupSubOk)
                            {
                                GridRender2(request, dataRowList, config, modalName);
                            }
                            // IsPatch
                            request.Grid.StateGet().IsPatch = config.IsSelectMultiPatch;
                        }
                    }
                    // Result
                    var result = new GridResponse2Dto { Grid = request.Grid };
                    return result;
                }
            // Lookup Filter
            case GridRequest2GridEnum.LookupFilter:
                {
                    ArgumentNullException.ThrowIfNull(request.ParentGrid);
                    ArgumentNullException.ThrowIfNull(request.ParentCell?.FieldName);
                    var modalName = request.ParentGrid?.State?.PathModalNameGet();
                    // Button Ok
                    if (request.Control?.ControlEnum == GridControlEnum.ButtonLookupOk)
                    {
                        // Filter Save (State)
                        var isSave = UtilGrid.LookupFilterSave2(request, request.ParentCell.FieldName);
                        // Parent Load
                        if (isSave)
                        {
                            var config = await Config2(request);
                            var dataRowList = await GridLoad2(request.Parent2(), null, config, GridConfigEnum.Grid, modalName);
                            GridRender2(request.Parent2(), dataRowList, config, modalName);
                        }
                        var parentGrid = isSave ? request.ParentGrid : null;
                        return new GridResponse2Dto { ParentGrid = parentGrid };
                    }
                    // Pagination
                    if (request.Control?.ControlEnum == GridControlEnum.Pagination)
                    {
                        // Filter Save (State)
                        var isSave = UtilGrid.LookupFilterSave2(request, request.ParentCell.FieldName);
                        // Filter Load
                        var config = await Config2(request);
                        {
                            var fieldName = request.ParentCell.FieldName;
                            var dataRowList = await GridLoad2(request, fieldNameDistinct: fieldName, config, GridConfigEnum.GridFilter, modalName);
                            UtilGrid.LookupFilterLoad2(request, dataRowList, fieldName);
                            UtilGrid.RenderLookup2(request, dataRowList, fieldName: fieldName);
                        }
                        // Parent Load
                        if (isSave)
                        {
                            var dataRowList = await GridLoad2(request.Parent2(), null, config, GridConfigEnum.Grid, modalName);
                            GridRender2(request.Parent2(), dataRowList, config, modalName);
                        }
                        var parentGrid = isSave ? request.ParentGrid : null;
                        return new GridResponse2Dto { Grid = request.Grid, ParentGrid = parentGrid };
                    }
                    // Load
                    {
                        var fieldName = request.ParentCell.FieldName;
                        var config = await Config2(request.Parent2());
                        var dataRowList = await GridLoad2(request, fieldNameDistinct: fieldName, config, GridConfigEnum.GridFilter, modalName);
                        UtilGrid.LookupFilterLoad2(request, dataRowList, fieldName);
                        UtilGrid.RenderLookup2(request, dataRowList, fieldName: fieldName);
                        return new GridResponse2Dto { Grid = request.Grid };
                    }
                }
            // Lookup Column
            case GridRequest2GridEnum.LookupColumn:
                {
                    ArgumentNullException.ThrowIfNull(request.ParentGrid);
                    var modalName = request.ParentGrid?.State?.PathModalNameGet();
                    // Button Ok
                    if (request.Control?.ControlEnum == GridControlEnum.ButtonLookupOk)
                    {
                        // Reset SelectMulti on LookupColumn ok
                        if (request.ParentGrid?.State?.IsSelectMultiList != null)
                        {
                            request.ParentGrid.State.IsSelectMultiList = null;
                        }
                        // Column Save (State)
                        var isSave = UtilGrid.LookupFilterSave2(request, "FieldName", isFilterColumn: true);
                        // Parent Load
                        if (isSave)
                        {
                            var config = await Config2(request);
                            var dataRowList = await GridLoad2(request.Parent2(), null, config, GridConfigEnum.Grid, modalName);
                            GridRender2(request.Parent2(), dataRowList, config, modalName);
                        }
                        GridDto? parentGrid = isSave ? request.ParentGrid : null;
                        return new GridResponse2Dto { ParentGrid = parentGrid };
                    }
                    // Pagination
                    if (request.Control?.ControlEnum == GridControlEnum.Pagination)
                    {
                        // Reset SelectMulti on LookupColumn ok
                        if (request.ParentGrid?.State?.IsSelectMultiList != null)
                        {
                            request.ParentGrid.State.IsSelectMultiList = null;
                        }
                        // Column Save (State)
                        var isSave = UtilGrid.LookupFilterSave2(request, "FieldName", isFilterColumn: true);
                        // Column Load
                        {
                            var dataRowList = await ColumnList2(request);
                            var dataRowListDynamic = UtilGrid.DynamicFrom(dataRowList, (dataRowFrom, dataRowTo) => { dataRowTo["FieldName"] = dataRowFrom.FieldName; });
                            UtilGrid.LookupFilterLoad2(request, dataRowListDynamic, "FieldName", isFilterColumn: true);
                            UtilGrid.RenderLookup2(request, dataRowListDynamic, "FieldName");
                        }
                        // Parent Load
                        if (isSave)
                        {
                            var config = await Config2(request);
                            var dataRowList = await GridLoad2(request.Parent2(), null, config, GridConfigEnum.Grid, modalName);
                            GridRender2(request.Parent2(), dataRowList, config, modalName);
                        }
                        GridDto? parentGrid = isSave ? request.ParentGrid : null;
                        return new GridResponse2Dto { Grid = request.Grid, ParentGrid = parentGrid };
                    }
                    // Load
                    {
                        var dataRowList = await ColumnList2(request);
                        var dataRowListDynamic = UtilGrid.DynamicFrom(dataRowList, (dataRowFrom, dataRowTo) => { dataRowTo["FieldName"] = dataRowFrom.FieldName; });
                        UtilGrid.LookupFilterLoad2(request, dataRowListDynamic, "FieldName", isFilterColumn: true);
                        UtilGrid.RenderLookup2(request, dataRowListDynamic, "FieldName");
                        return new GridResponse2Dto { Grid = request.Grid };
                    }
                }
            // Autocomplete
            case GridRequest2GridEnum.LookupAutocomplete:
                {
                    ArgumentNullException.ThrowIfNull(request.ParentGrid);
                    ArgumentNullException.ThrowIfNull(request.ParentCell?.FieldName);
                    var config = await Config2(request);
                    if (request.GridActionEnum == GridRequest2GridActionEnum.LookupAutocompleteOk)
                    {
                        var isSave = UtilGrid.LookupAutocompleteSave2(request);
                        if (isSave)
                        {
                            // Parent Save
                            var sourceList = UtilGrid.GridSave2(request.Parent2(), config);
                            if (sourceList.Count() > 0)
                            {
                                await GridSave2(request.Parent2(), sourceList, config);
                            }
                            // Parent Load
                            var dataRowList = await GridLoad2(request.Parent2(), null, config, GridConfigEnum.Grid, null); // TODO ModalName
                            GridRender2(request.Parent2(), dataRowList, config, null);
                        }
                        GridDto? parentGrid = isSave ? request.ParentGrid : null;
                        return new GridResponse2Dto { Grid = request.Grid, ParentGrid = parentGrid };
                    }
                    // Load
                    {
                        var fieldName = request.ParentCell.FieldName;
                        var dataRowList = await GridLoad2(request, fieldNameDistinct: fieldName, config, GridConfigEnum.GridAutocomplete, null); // TODO ModalName
                        UtilGrid.RenderLookupAutocomplete2(request, dataRowList, fieldName: fieldName);
                        return new GridResponse2Dto { Grid = request.Grid };
                    }
                }
            // LookupEdit
            case GridRequest2GridEnum.LookupEdit:
                {
                    ArgumentNullException.ThrowIfNull(request.ParentGrid);
                    if (request.GridActionEnum == GridRequest2GridActionEnum.LookupEditOpen)
                    {
                        var pathList = request.ParentGrid.State?.PathList;
                        request.Grid.StateGet().PathList = pathList != null ? new(pathList) : null;
                        request.Grid.StateGet().PathListAdd(new() { Name = "Edit", IsModal = true, IsModalCustom = false });
                    }
                    var config = await Config2(request);
                    var modalName = request.Grid.State?.PathModalNameGet();
                    var modalNameParent = request.ParentGrid?.State?.PathModalNameGet();
                    // Save
                    if (request.GridActionEnum == GridRequest2GridActionEnum.LookupEditSave)
                    {
                        var sourceList = UtilGrid.GridSave2(request, config);
                        if (sourceList.Count() > 0)
                        {
                            await GridSave2(request, sourceList, config);
                        }
                    }
                    // Load
                    var dataRowList = await GridLoad2(request, null, config, GridConfigEnum.Grid, modalNameParent);
                    GridRender2(request, dataRowList, config, modalName);
                    return new GridResponse2Dto { Grid = request.Grid };
                }
            // LookupDelete
            case GridRequest2GridEnum.LookupConfirmDelete:
                {
                    if (request.GridActionEnum == GridRequest2GridActionEnum.LookupConfirmDeleteOpen)
                    {
                        var pathList = request.ParentGrid?.State?.PathList;
                        request.Grid.StateGet().PathList = pathList != null ? new(pathList) : null;
                        request.Grid.StateGet().PathListAdd(new() { Name = "Delete", IsModal = true, IsModalCustom = false });
                    }
                    var config = await Config2(request);
                    var modalName = request.Grid.State?.PathModalNameGet();
                    var modalNameParent = request.ParentGrid?.State?.PathModalNameGet();
                    if (request.GridActionEnum == GridRequest2GridActionEnum.LookupConfirmDeleteOk)
                    {
                        ArgumentNullException.ThrowIfNull(request.ParentGrid);
                        // Save
                        var sourceList = UtilGrid.GridSave2(request.Parent2(), config);
                        if (sourceList.Count() > 0)
                        {
                            await GridSave2(request.Parent2(), sourceList, config);
                        }
                        // Load Parent
                        var dataRowList = await GridLoad2(request.Parent2(), null, config, GridConfigEnum.Grid, modalNameParent);
                        GridRender2(request.Parent2(), dataRowList, config, modalNameParent);
                        return new GridResponse2Dto { ParentGrid = request.ParentGrid };
                    }
                    // Load
                    GridRender2(request, new(), config, modalName);
                    return new GridResponse2Dto { Grid = request.Grid };
                }
            default:
                throw new Exception();
        }
    }

    protected virtual void GridRender2(GridRequest2Dto request, List<Dynamic> dataRowList, GridConfig config, string? modalName)
    {
        UtilGrid.Render2(request, dataRowList, config, modalName);
    }
}

public class GridButtonCustom
{
    public GridCellDto Cell { get; set; } = default!;

    public GridControlDto Control { get; set; } = default!;
}

public static class GridExtension
{
    public static GridControlDto AddControl(this GridDto grid, GridControlDto control, int? dataRowIndex = null)
    {
        grid.RowCellList = grid.RowCellList ?? new();
        if (grid.RowCellList.LastOrDefault() == null)
        {
            grid.RowCellList.Add(new());
        }
        var row = grid.RowCellList.Last();
        row.AddControl(control, dataRowIndex);
        return control;
    }

    public static GridControlDto AddControl(this List<GridCellDto> row, GridControlDto control, int? dataRowIndex = null)
    {
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