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
        grid.State.FilterMultiList ??= new();
        var filterMultiList = grid.State.FilterMultiList;
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
        // FilterMulti
        if (grid.State?.FilterMultiList != null)
        {
            foreach (var filterMulti in grid.State.FilterMultiList)
            {
                var textListLower = filterMulti.TextList.Select(item => item.ToLower()).ToList();
                query = query.Where($"@0.Contains(Convert.ToString({filterMulti.FieldName}).ToLower())", textListLower);
            }
        }
        // HeaderLookupFieldName (Distinct)
        if (headerLookupFieldName != null)
        {
            query = query
                .Select(item => item[headerLookupFieldName])
                .Distinct()
                .OrderBy(item => item)
                .Select(item => new Dictionary<string, object?> { { headerLookupFieldName, item } });
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

    public static void SaveHeaderLookup(GridDto grid)
    {

    }

    public static void SaveColumnLookup(GridDto grid, GridDto parentGrid)
    {
        grid.State ??= new();
        parentGrid.State ??= new();
        parentGrid.State.ColumnList ??= new();
        foreach (var cell in grid.RowCellList!.SelectMany(item => item).Where(item => item.CellEnum == GridCellEnum.Field && item.DataRowIndex != null))
        {
            var isSelect = grid.State.IsSelectMultiGet(cell.DataRowIndex ?? -1) == true;
            if (isSelect)
            {
                parentGrid.State.ColumnList.Add(cell.Text!);
            }
        }
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
        //
        RenderCalcColSpan(grid);
    }

    /// <summary>
    /// Render checkbox lookup data grid. For header and column lookup.
    /// </summary>
    public static void RenderCheckboxLookup(GridDto grid, List<Dictionary<string, object?>> dataRowList, string fieldName)
    {
        grid.Clear();
        // Render Filter
        grid.AddCell(new() { CellEnum = GridCellEnum.Filter, FieldName = fieldName, TextPlaceholder = "Search" });
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
            grid.AddCell(new() { CellEnum = GridCellEnum.CheckboxSelectMulti, DataRowIndex = dataRowIndex });
            grid.AddCell(new() { CellEnum = GridCellEnum.Field, Text = dataRow[fieldName]?.ToString(), DataRowIndex = dataRowIndex }); // TODO Readonly
            dataRowIndex += 1;
        }
        // Render Pagination
        grid.AddRow();
        grid.AddControl(new() { ControlEnum = GridControlEnum.Pagination });
        // Render Ok, Cancel
        grid.AddRow();
        grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupOk });
        grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupCancel });
        //
        RenderCalcColSpan(grid);
    }

    /// <summary>
    /// Calc ColSpan of last cell.
    private static void RenderCalcColSpan(GridDto grid)
    {
        if (grid.RowCellList != null)
        {
            var cellCountMax = 0;
            foreach (var row in grid.RowCellList)
            {
                cellCountMax = Math.Max(cellCountMax, row.Count());
            }
            foreach (var row in grid.RowCellList)
            {
                var colCount = 0;
                GridCellDto? cellLast = null;
                foreach (var cell in row)
                {
                    colCount += cell.ColSpan.GetValueOrDefault(1);
                    cellLast = cell;
                }
                if (cellLast != null)
                {
                    cellLast.ColSpan = cellLast.ColSpan.GetValueOrDefault(1) + (cellCountMax - colCount);
                    cellLast.ColSpan = cellLast.ColSpan == 1 ? null : cellLast.ColSpan;
                    UtilServer.Assert(cellLast.ColSpan == null || cellLast.ColSpan > 0);
                }
            }
        }
    }
}