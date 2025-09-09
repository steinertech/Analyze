public class GridBase
{
    /// <summary>
    /// Returns column list to render grid.
    /// </summary>
    protected virtual Task<List<GridColumnDto>> LoadColumnList(GridDto grid)
    {
        var result = new List<GridColumnDto>();
        return Task.FromResult(result);
    }

    /// <summary>
    /// Returns data row list to render grid.
    /// </summary>
    protected virtual Task<List<Dictionary<string, object?>>> LoadDataRowList(GridDto grid, string? headerLookupFieldName)
    {
        var result = new List<Dictionary<string, object?>>();
        return Task.FromResult(result);
    }

    public async Task<GridLoadResultDto> Load(GridDto grid, GridCellDto? parentCell, GridControlDto? parentControl, GridDto? parentGrid)
    {
        if (parentCell == null)
        {
            // Load Grid
            var columnList = await LoadColumnList(grid);
            var dataRowList = await LoadDataRowList(grid, null);
            UtilGrid.Render(grid, dataRowList, columnList);
            return new() { Grid = grid };
        }
        if (parentCell?.CellEnum == GridCellEnum.Header && parentGrid != null && parentCell.FieldName != null)
        {
            // Load Grid Header Lookup
            var dataRowList = await LoadDataRowList(grid, headerLookupFieldName: parentCell.FieldName);
            UtilGrid.RenderCheckboxLookup(grid, dataRowList, fieldName: parentCell.FieldName);
            return new() { Grid = grid };
        }
        // Grid Column
        if (parentGrid != null && parentCell?.ControlList?.Where(item => item.ControlEnum == GridControlEnum.ButtonColumn).Any() == true)
        {
            if (grid.RowCellList != null)
            {
                // Save Grid Column
                UtilGrid.SaveColumnLookup(grid, parentGrid);
                var columnList = await LoadColumnList(parentGrid);
                var dataRowList = await LoadDataRowList(parentGrid, null);
                UtilGrid.Render(parentGrid, dataRowList, columnList);
                return new() { ParentGrid = parentGrid };
            }
            {
                // Load Grid Column
                var query = (await LoadColumnList(grid)).Select(item => new Dictionary<string, object?> { { "FieldName", item.FieldName } }).AsQueryable();
                var dataRowList = await UtilGrid.LoadDataRowList(grid, query, null);
                UtilGrid.RenderCheckboxLookup(grid, dataRowList, "FieldName");
                return new() { Grid = grid };
            }
        }
        throw new Exception("Load failed!");
    }
}

