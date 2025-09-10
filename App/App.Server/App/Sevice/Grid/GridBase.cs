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

    public async Task<GridResponseDto> Load(GridRequestDto request)
    {
        if (request.ParentCell == null)
        {
            // Load Grid
            var columnList = await LoadColumnList(request.Grid);
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
                    var parentColumnList = await LoadColumnList(request.ParentGrid);
                    var parentDataRowList = await LoadDataRowList(request.ParentGrid, null);
                    UtilGrid.Render(request.ParentGrid, parentDataRowList, parentColumnList);
                    return new GridResponseDto { ParentGrid = request.ParentGrid };
                }
                // Load Grid Header Lookup
                var dataRowList = await LoadDataRowList(request.Grid, headerLookupFieldName: request.ParentCell.FieldName);
                UtilGrid.RenderCheckboxLookup(request.Grid, dataRowList, fieldName: request.ParentCell.FieldName);
            }
            else
            {
                if (request.ParentCell?.ControlList?.Where(item => item.ControlEnum == GridControlEnum.ButtonColumn).Any() == true)
                {
                    if (request.Control?.ControlEnum == GridControlEnum.ButtonLookupOk && request.ParentGrid != null)
                    {
                        UtilGrid.SaveColumnLookup(request.Grid, request.ParentGrid);
                        var parentColumnList = await LoadColumnList(request.ParentGrid);
                        var parentDataRowList = await LoadDataRowList(request.ParentGrid, null);
                        UtilGrid.Render(request.ParentGrid, parentDataRowList, parentColumnList);
                        return new GridResponseDto { ParentGrid = request.ParentGrid };
                    }
                    // Load Grid Column
                    var query = (await LoadColumnList(request.Grid)).Select(item => new Dictionary<string, object?> { { "FieldName", item.FieldName } }).AsQueryable();
                    var dataRowList = await UtilGrid.LoadDataRowList(request.Grid, query, null);
                    UtilGrid.RenderCheckboxLookup(request.Grid, dataRowList, "FieldName");
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