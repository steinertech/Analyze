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
    protected virtual Task<List<Dictionary<string, object>>> LoadDataRowList(GridDto grid, List<GridStateFilterDto> filterList, GridStateSortDto? sort, GridPaginationDto pagination)
    {
        var result = new List<Dictionary<string, object>>();
        return Task.FromResult(result);
    }

    /// <summary>
    /// Render Grid.
    /// </summary>
    protected void Render(GridDto grid, List<Dictionary<string, object>> dataRowList, List<GridColumnDto> columnList)
    {
        grid.Clear();
        // ColumnSortList
        var columnSortList = columnList.OrderBy(item => item.Sort).ToList();
        // RowKey
        var columnRowKey = columnList.Where(item => item.IsRowKey == true).SingleOrDefault();
        // Render Header
        grid.AddRow();
        foreach (var column in columnSortList)
        {
            grid.AddCell(new() { CellEnum = GridCellEnum.Header, FieldName = column.FieldName, Text = column.FieldName });
        }
        // Render Filter
        grid.AddRow();
        foreach (var column in columnSortList)
        {
            grid.AddCell(new() { CellEnum = GridCellEnum.Filter, FieldName = column.FieldName, TextPlaceholder = "Search" });
        }
        // Render Data
        var dataRowIndex = 0;
        foreach (var row in dataRowList)
        {
            grid.AddRow();
            foreach (var column in columnList.OrderBy(item => item.Sort))
            {
                var text = row[column.FieldName!].ToString();
                if (columnRowKey == null)
                {
                    grid.AddCell(new GridCellDto { CellEnum = GridCellEnum.Field, Text = text, FieldName = column.FieldName, DataRowIndex = dataRowIndex });
                }
                else
                {
                    var rowKey = row[columnRowKey.FieldName!].ToString();
                    grid.AddCell(new GridCellDto { CellEnum = GridCellEnum.Field, Text = text, FieldName = column.FieldName, DataRowIndex = dataRowIndex }, rowKey);
                }
            }
            dataRowIndex += 1;
        }
    }

    /// <summary>
    /// Render Header Lookup Grid.
    /// </summary>
    private void RenderHeaderLookup(GridDto grid, string fieldName, List<string?> rowList)
    {
        grid.Clear();
        // Render Filter
        grid.AddCell(new() { CellEnum = GridCellEnum.Filter, FieldName = fieldName, TextPlaceholder = "Search", ColSpan = 2 });
        // Render Select All
        grid.AddRow();
        grid.AddControl(new() { ControlEnum = GridControlEnum.CheckboxSelectMultiAll });
        grid.AddCellControl();
        grid.AddControl(new() { ControlEnum = GridControlEnum.LabelCustom, Text = "(Select All)" });
        // Render Data
        var dataRowIndex = 0;
        foreach (var row in rowList)
        {
            grid.AddRow();
            grid.AddCell(new() { CellEnum = GridCellEnum.CheckboxSelectMulti });
            grid.AddControl(new() { ControlEnum = GridControlEnum.LabelCustom, Text = row });
            dataRowIndex += 1;
        }
        // Render Pagination
        grid.AddRow();
        grid.AddCellControl(2);
        grid.AddControl(new() { ControlEnum = GridControlEnum.Pagination });
        // Render Ok, Cancel
        grid.AddRow();
        grid.AddCellControl(2);
        grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupOk });
        grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupCancel });
    }

    public async Task Load(GridDto grid, GridCellDto? parentCell, GridControlDto? parentControl, GridDto? parentGrid)
    {
        // Init Filter, Pagination
        grid.State ??= new();
        grid.State.FilterList ??= new();
        var filterList = grid.State.FilterList;
        grid.State.Pagination ??= new();
        var pagination = grid.State.Pagination;
        pagination.PageSize ??= 3;
        pagination.PageIndex ??= 0;
        pagination.PageIndexDeltaClick ??= 0;
        // Load Grid
        if (parentCell == null)
        {
            // Load
            var columnList = await LoadColumnList();
            var dataRowList = await LoadDataRowList(grid, filterList, grid.State?.Sort, pagination);
            Render(grid, dataRowList, columnList);
        }
        // Load Grid Header Lookup
        if (parentCell?.CellEnum == GridCellEnum.Header && parentGrid != null)
        {
            var dataRowList = await LoadDataRowList(parentGrid, filterList, grid.State?.Sort, pagination);
            var result = dataRowList.Select(item => item[parentCell.FieldName!].ToString()).Distinct().ToList();
            RenderHeaderLookup(grid, parentCell.FieldName!, result);
        }
    }
}

