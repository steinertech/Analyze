using Microsoft.Azure.Cosmos.Linq;
using System.Linq.Dynamic.Core;

public static class UtilGrid
{
    /// <summary>
    /// Returns data row list (filter, sort and pagination) from query to render data grid.
    /// </summary>
    public static async Task<List<Dictionary<string, object?>>> LoadDataRowList(GridDto grid, IQueryable<Dictionary<string, object?>> query, string? headerLookupFieldName)
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
        var sort = grid.State.Sort;
        // Filter
        foreach (var filter in grid.State.FilterList)
        {
            var fieldName = filter.FieldName!;
            query = query.Where(item => (item![fieldName!]!.ToString() ?? "").ToLower().Contains(filter.Text.ToLower()) == true);
        }
        // HeaderLookupFieldName (Distinct)
        if (headerLookupFieldName != null)
        {
            query = query.Select(item => new Dictionary<string, object?> { { headerLookupFieldName, item[headerLookupFieldName] } }).Distinct();
        }
        // Pagination (PageCount)
        var rowCount = await query.CountAsync();
        pagination.PageCount = (int)Math.Ceiling((double)rowCount / (double)pagination.PageSize!);
        pagination.PageIndex += pagination.PageIndexDeltaClick;
        if (pagination.PageIndex >= pagination.PageCount)
        {
            pagination.PageIndex = pagination.PageCount - 1;
        }
        if (pagination.PageIndex < 0)
        {
            pagination.PageIndex = 0;
        }
        // Sort
        if (sort != null)
        {
            query = query.OrderBy($"""{sort.FieldName}{(sort.IsDesc ? " DESC" : "")}""");
        }
        // Pagination
        query = query
            .Skip(pagination.PageIndex!.Value * pagination.PageSize.Value)
            .Take(pagination.PageSize.Value);
        // Result
        var result = query.ToList();
        return result;
    }

    /// <summary>
    /// Render data grid.
    /// </summary>
    public static void Render(GridDto grid, List<Dictionary<string, object?>> dataRowList, List<GridColumnDto> columnList)
    {
        grid.Clear();
        // ColumnSortList
        var columnSortList = columnList.OrderBy(item => item.Sort).ToList();
        // RowKey
        var columnRowKey = columnList.Where(item => item.IsRowKey == true).SingleOrDefault();
        // Render Column
        grid.AddRow();
        grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonColumn });
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
        foreach (var dataRow in dataRowList)
        {
            grid.AddRow();
            foreach (var column in columnList.OrderBy(item => item.Sort))
            {
                var text = dataRow[column.FieldName!]?.ToString();
                if (columnRowKey == null)
                {
                    grid.AddCell(new GridCellDto { CellEnum = GridCellEnum.Field, Text = text, FieldName = column.FieldName, DataRowIndex = dataRowIndex });
                }
                else
                {
                    var rowKey = dataRow[columnRowKey.FieldName!]?.ToString();
                    grid.AddCell(new GridCellDto { CellEnum = GridCellEnum.Field, Text = text, FieldName = column.FieldName, DataRowIndex = dataRowIndex }, rowKey);
                }
            }
            dataRowIndex += 1;
        }
        // Render Pagination
        grid.AddRow();
        grid.AddControl(new() { ControlEnum = GridControlEnum.Pagination });
    }

    /// <summary>
    /// Render header lookup data grid.
    /// </summary>
    public static void RenderHeaderLookup(GridDto grid, List<Dictionary<string, object?>> dataRowList, string headerLookupFieldName)
    {
        grid.Clear();
        // Render Filter
        grid.AddCell(new() { CellEnum = GridCellEnum.Filter, FieldName = headerLookupFieldName, TextPlaceholder = "Search", ColSpan = 2 });
        // Render Select All
        grid.AddRow();
        grid.AddControl(new() { ControlEnum = GridControlEnum.CheckboxSelectMultiAll });
        grid.AddCellControl();
        grid.AddControl(new() { ControlEnum = GridControlEnum.LabelCustom, Text = "(Select All)" });
        // Render Data
        var dataRowIndex = 0;
        foreach (var dataRow in dataRowList)
        {
            grid.AddRow();
            grid.AddCell(new() { CellEnum = GridCellEnum.CheckboxSelectMulti });
            grid.AddControl(new() { ControlEnum = GridControlEnum.LabelCustom, Text = dataRow[headerLookupFieldName]?.ToString() });
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
}