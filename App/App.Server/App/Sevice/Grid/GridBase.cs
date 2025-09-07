public class GridBase
{
    /// <summary>
    /// Returns column list to render grid.
    /// </summary>
    protected virtual Task<List<GridColumnDto>> LoadColumnList()
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

    public async Task Load(GridDto grid, GridCellDto? parentCell, GridControlDto? parentControl, GridDto? parentGrid)
    {
        if (parentCell == null)
        {
            // Load Grid
            var columnList = await LoadColumnList();
            var dataRowList = await LoadDataRowList(grid, null);
            UtilGrid.Render(grid, dataRowList, columnList);
        }
        else
        {
            if (parentCell?.CellEnum == GridCellEnum.Header && parentGrid != null && parentCell.FieldName != null)
            {
                // Load Grid Header Lookup
                var dataRowList = await LoadDataRowList(grid, headerLookupFieldName: parentCell.FieldName);
                UtilGrid.RenderHeaderLookup(grid, dataRowList, headerLookupFieldName: parentCell.FieldName);
            }
            else
            {
                if (parentCell?.ControlList?.Where(item => item.ControlEnum == GridControlEnum.ButtonColumn).Any() == true)
                {
                    // Load Grid Column Picker
                    var query = (await LoadColumnList()).Select(item => new Dictionary<string, object?> { { "FieldName", item.FieldName } }).AsQueryable();
                    var dataRowList = await UtilGrid.LoadDataRowList(grid, query, null);
                    UtilGrid.RenderHeaderLookup(grid, dataRowList, "FieldName");
                }
                else
                {
                    throw new Exception("Load failed!");
                }
            }
        }
    }
}

