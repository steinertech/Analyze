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

    public async Task<GridResponseDto> Load(GridRequestDto request)
    {
        if (request.ParentCell == null)
        {
            // Load Grid
            var columnList = await LoadColumnList();
            var dataRowList = await LoadDataRowList(request.Grid, null);
            UtilGrid.Render(request.Grid, dataRowList, columnList);
        }
        else
        {
            if (request.ParentCell?.CellEnum == GridCellEnum.Header && request.ParentGrid != null && request.ParentCell.FieldName != null)
            {
                // Load Grid Header Lookup
                var dataRowList = await LoadDataRowList(request.Grid, headerLookupFieldName: request.ParentCell.FieldName);
                UtilGrid.RenderHeaderLookup(request.Grid, dataRowList, headerLookupFieldName: request.ParentCell.FieldName);
            }
            else
            {
                if (request.ParentCell?.ControlList?.Where(item => item.ControlEnum == GridControlEnum.ButtonColumn).Any() == true)
                {
                    // Load Grid Column Picker
                    var query = (await LoadColumnList()).Select(item => new Dictionary<string, object?> { { "FieldName", item.FieldName } }).AsQueryable();
                    var dataRowList = await UtilGrid.LoadDataRowList(request.Grid, query, null);
                    UtilGrid.RenderHeaderLookup(request.Grid, dataRowList, "FieldName");
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