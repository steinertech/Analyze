using Microsoft.Azure.Cosmos.Linq;
using System.Linq.Dynamic.Core;

public static class UtilGrid
{
    public static List<Dynamic> DynamicFrom<T>(List<T> dataRowList, Action<T, Dynamic> convert)
    {
        var result = new List<Dynamic>();
        foreach (var dataRow in dataRowList)
        {
            var dataRowDictionary = new Dynamic();
            convert(dataRow, dataRowDictionary);
            result.Add(dataRowDictionary);
        }
        return result;
    }

    public static List<T> DynamicTo<T>(List<Dynamic> dataRowList, Action<Dynamic, T> convert) where T : new()
    {
        var result = new List<T>();
        foreach (var dataRowDictionary in dataRowList)
        {
            var dataRow = new T();
            convert(dataRowDictionary, dataRow);
            result.Add(dataRow);
        }
        return result;
    }

    /// <summary>
    /// Returns data row list with applied (filter, sort and pagination) to render data grid.
    /// </summary>
    /// <param name="dataRowList">DataRowList (or query).</param>
    /// <param name="grid">Grid with state to apply (filter, sort and pagination).</param>
    /// <param name="filterFieldName">Used to render filter lookup data grid.</param>
    public static async Task<List<Dynamic>> LoadDataRowList(List<Dynamic> dataRowList, GridDto grid, string? filterFieldName, GridConfig? config)
    {
        var query = dataRowList.AsQueryable();
        // Init Filter, Pagination
        grid.State ??= new();
        grid.State.FilterList ??= new();
        var filterList = grid.State.FilterList;
        grid.State.FilterMultiList ??= new();
        var filterMultiList = grid.State.FilterMultiList;
        grid.State.Pagination ??= new();
        var pagination = grid.State.Pagination;
        pagination.PageSize ??= config?.PageSize ?? 3;
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
        foreach (var filterMulti in grid.State.FilterMultiList)
        {
            var isInclude = filterMulti.IsSelectMultiAll ? "!" : ""; // Include or exclude
            var textListLower = filterMulti.TextList.Select(item => item?.ToLower()).ToList();
            query = query.Where($"{isInclude}@0.Contains(Convert.ToString({filterMulti.FieldName}).ToLower())", textListLower);
        }
        // FilterFieldName (Distinct)
        if (filterFieldName != null)
        {
            query = query
                .Select(item => item[filterFieldName])
                .Distinct()
                .OrderBy(item => item)
                .Select(item => new Dynamic { { filterFieldName, item } });
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
    /// Load parent grid state to lookup grid.
    /// </summary>
    public static void LookupColumnLoad(GridDto parentGrid, GridDto grid, List<Dynamic> dataRowList)
    {
        grid.State ??= new();
        grid.State.IsSelectMultiList = new();
        foreach (var dataRow in dataRowList)
        {
            var fieldName = dataRow["FieldName"];
            bool isSelect = parentGrid?.State?.ColumnList?.Contains(fieldName) == true;
            grid.State.IsSelectMultiList.Add(isSelect);
        }
    }

    /// <summary>
    /// Save lookup column state to parent grid.
    /// </summary>
    public static void LookupColumnSave(GridDto grid, GridDto parentGrid)
    {
        grid.State ??= new();
        parentGrid.State ??= new();
        parentGrid.State.ColumnList ??= new();
        if (grid.State.IsSelectMultiList != null && grid.State.RowKeyList != null)
        {
            for (int index = 0; index < grid.State.IsSelectMultiList.Count; index++)
            {
                var text = grid.State.RowKeyList[index]!;
                if (grid.State.IsSelectMultiList[index] == true)
                {
                    parentGrid.State.ColumnList.Add(text);
                }
                else
                {
                    parentGrid.State.ColumnList.Remove(text);
                }
            }
        }
    }

    /// <summary>
    /// Load filter state from parent grid to lookup grid.
    /// </summary>
    public static void LookupFilterLoad(GridDto parentGrid, GridDto grid, List<Dynamic> dataRowList, string fieldName)
    {
        grid.State ??= new();
        grid.State.IsSelectMultiList = new();
        var filterMulti = parentGrid.State?.FilterMultiList?.SingleOrDefault(item => item.FieldName == fieldName);
        grid.State.IsSelectMultiAll = filterMulti?.IsSelectMultiAll == false ? false : true;
        var isSelectMultiAll = grid.State.IsSelectMultiAll == true;
        foreach (var dataRow in dataRowList)
        {
            var text = dataRow[fieldName]?.ToString();
            bool isSelect = isSelectMultiAll;
            if (filterMulti?.TextList.Contains(text) == true)
            {
                isSelect = !isSelect;
            }
            grid.State.IsSelectMultiList.Add(isSelect);
        }
    }

    /// <summary>
    /// Save filter state from lookup grid to parent grid.
    /// </summary>
    /// <returns>Returns true, if something changed.</returns>
    public static bool LookupFilterSave(GridDto grid, GridDto parentGrid, string fieldName)
    {
        var result = false;
        grid.State ??= new();
        parentGrid.State ??= new();
        parentGrid.State.FilterMultiList ??= new();
        var textList = new List<(string? Text, bool IsSelect)>();
        if (grid.State.IsSelectMultiList != null && grid.State.RowKeyList != null)
        {
            for (int index = 0; index < grid.State.IsSelectMultiList.Count; index++)
            {
                var text = grid.State.RowKeyList[index];
                bool isSelect = grid.State.IsSelectMultiList[index] == true;
                textList.Add((text, isSelect));
            }
        }
        var filterMulti = parentGrid.State.FilterMultiList.SingleOrDefault(item => item.FieldName == fieldName);
        if (filterMulti == null)
        {
            parentGrid.State.FilterMultiList.Add(new GridStateFilterMultiDto() { FieldName = fieldName, TextList = new List<string?>(), IsSelectMultiAll = true });
        }
        filterMulti = parentGrid.State.FilterMultiList.Single(item => item.FieldName == fieldName);
        var isSelectMultiAll = grid.State.IsSelectMultiAll == true;
        if (filterMulti.IsSelectMultiAll != isSelectMultiAll)
        {
            filterMulti.IsSelectMultiAll = isSelectMultiAll;
            result = true;
        }
        foreach (var item in textList)
        {
            if (item.IsSelect && !filterMulti.TextList.Contains(item.Text))
            {
                result = true;
                filterMulti.TextList.Add(item.Text);
            }
            if (!item.IsSelect && filterMulti.TextList.Contains(item.Text))
            {
                result = true;
                filterMulti.TextList.Remove(item.Text);
            }
        }
        if (filterMulti.TextList.Count == 0 && filterMulti.IsSelectMultiAll == true)
        {
            parentGrid.State.FilterMultiList.Remove(filterMulti);
        }
        return result;
    }

    /// <summary>
    /// Render data grid.
    /// </summary>
    public static void Render(GridDto grid, List<Dynamic> dataRowList, List<GridColumn> columnList)
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
    /// Render lookup data grid. For filter and column lookup.
    /// </summary>
    public static void RenderLookup(GridDto grid, List<Dynamic> dataRowList, string fieldName)
    {
        grid.Clear();
        // Render Filter
        grid.AddCell(new() { CellEnum = GridCellEnum.Filter, FieldName = fieldName, TextPlaceholder = "Search" });
        // Render Select All
        grid.AddRow();
        grid.AddControl(new() { ControlEnum = GridControlEnum.CheckboxSelectMultiAll, Text = grid.State?.IsSelectMultiAll?.ToString() });
        grid.AddCellControl();
        grid.AddControl(new() { ControlEnum = GridControlEnum.LabelCustom, Text = "(Select All)" });
        // Render Data
        var dataRowIndex = 0;
        foreach (var dataRow in dataRowList)
        {
            grid.AddRow();
            var text = dataRow[fieldName]?.ToString();
            grid.AddCell(new() { CellEnum = GridCellEnum.CheckboxSelectMulti, DataRowIndex = dataRowIndex }, text);
            grid.AddControl(new() { ControlEnum = GridControlEnum.LabelCustom, Text = text });
            dataRowIndex += 1;
        }
        // Render Pagination
        grid.AddRow();
        grid.AddControl(new() { ControlEnum = GridControlEnum.Pagination });
        // Render Ok, Cancel
        grid.AddRow();
        grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupOk });
        grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupCancel });
        // Calc ColSpan
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