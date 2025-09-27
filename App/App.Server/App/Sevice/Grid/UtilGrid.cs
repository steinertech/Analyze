using Microsoft.Azure.Cosmos.Linq;
using System.Globalization;
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
    /// <param name="fieldNameDistinct">Used for example for filter lookup data grid. Returns one column grid.</param>
    /// <param name="config">Used for example for PageSize.</param>
    public static async Task<List<Dynamic>> GridLoad(List<Dynamic> dataRowList, GridDto grid, string? fieldNameDistinct, int pageSize)
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
        pagination.PageIndex ??= 0;
        pagination.PageIndexDeltaClick ??= 0;
        var sort = grid.State.Sort;
        // Filter
        foreach (var (fieldName, text) in grid.State.FilterList)
        {
            query = query.Where(item => (item![fieldName!]!.ToString() ?? "").ToLower().Contains(text.ToLower()) == true);
        }
        // FilterMulti
        foreach (var (fieldName, filterMulti) in grid.State.FilterMultiList)
        {
            var isInclude = filterMulti.IsSelectAll ? "!" : ""; // Include or exclude
            var textListLower = filterMulti.TextList.Select(item => item?.ToLower()).ToList();
            query = query.Where($"{isInclude}@0.Contains(Convert.ToString({fieldName}).ToLower())", textListLower);
        }
        // FieldName (Distinct)
        if (fieldNameDistinct != null)
        {
            query = query
                .Select(item => item[fieldNameDistinct])
                .Distinct()
                .OrderBy(item => item)
                .Select(item => new Dynamic { { fieldNameDistinct, item } });
        }
        // Pagination (PageCount)
        var rowCount = await query.CountAsync();
        pagination.PageCount = (int)Math.Ceiling((double)rowCount / (double)pageSize!);
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
            .Skip(pagination.PageIndex!.Value * pageSize)
            .Take(pageSize);
        // Result
        var result = query.ToList();
        return result;
    }

    /// <summary>
    /// Save grid state to dataRowList.
    /// </summary>
    /// <returns>Returns updated dataRowList.</returns>
    public static List<Dynamic> GridSave(GridDto grid, List<Dynamic> dataRowList, GridConfig config)
    {
        if (grid.State?.FieldSaveList != null && grid.State.RowKeyList != null)
        {
            foreach (var field in grid.State.FieldSaveList)
            {
                var rowKey = grid.State.RowKeyList[field.DataRowIndex!.Value];
                Dynamic dataRow;
                if (rowKey == null)
                {
                    // User added new data row.
                    dataRow = new() { DynamicEnum = DynamicEnum.Insert };
                    dataRowList.Insert(0, dataRow);
                }
                else
                {
                    // User modified existing data row.
                    dataRow = dataRowList.Single(item => item.ContainsKey(config.FieldNameRowKey) && item[config.FieldNameRowKey]?.ToString() == rowKey);
                    dataRow.DynamicEnum = DynamicEnum.Update;
                }
                var configColumn = config.ColumnList.Single(item => item.FieldName == field.FieldName);
                if (configColumn.IsAllowModify)
                {
                    var text = field.TextModified;
                    object? value;
                    if (string.IsNullOrEmpty(text))
                    {
                        value = null;
                    }
                    else
                    {
                        switch (configColumn.ColumnEnum)
                        {
                            case GridColumnEnum.Text:
                                value = text;
                                break;
                            case GridColumnEnum.Int:
                                value = int.Parse(text);
                                break;
                            case GridColumnEnum.Double:
                                value = double.Parse(text);
                                break;
                            case GridColumnEnum.Date:
                                value = DateTime.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                                break;
                            default:
                                throw new Exception("Type unknown!");
                        }
                    }
                    dataRow[field.FieldName!] = value;
                }
            }
        }
        return dataRowList;
    }

    /// <summary>
    /// Load filter state from parent grid to lookup grid.
    /// </summary>
    public static void LookupFilterLoad(GridDto parentGrid, GridDto grid, List<Dynamic> dataRowList, string fieldName, bool isFilterColumn = false)
    {
        grid.State ??= new();
        grid.State.IsSelectMultiList = new();
        GridStateFilterMultiDto? filter;
        if (isFilterColumn == false)
        {
            filter = parentGrid.State?.FilterMultiList != null && parentGrid.State.FilterMultiList.TryGetValue(fieldName, out var resultFilter) ? resultFilter : null;
        }
        else
        {
            filter = parentGrid.State?.ColumnFilterMulti;
        }
        grid.State.IsSelectMultiAll = filter?.IsSelectAll == false ? false : true;
        grid.State.IsSelectMultiIndeterminate = filter?.TextList.Count() > 0 ? true : false;
        var isSelectMultiAll = grid.State.IsSelectMultiAll == true;
        foreach (var dataRow in dataRowList)
        {
            var text = dataRow[fieldName]?.ToString();
            bool isSelect = isSelectMultiAll;
            if (filter?.TextList.Contains(text) == true)
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
    public static bool LookupFilterSave(GridDto grid, GridDto parentGrid, string fieldName, bool isFilterColumn = false)
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
        GridStateFilterMultiDto? filter;
        if (isFilterColumn == false)
        {
            filter = parentGrid.State.FilterMultiList.TryGetValue(fieldName, out var resultFilter) ? resultFilter : null;
            if (filter == null)
            {
                filter = new() { TextList = new List<string?>(), IsSelectAll = true };
                parentGrid.State.FilterMultiList[fieldName] = filter;
            }
        }
        else
        {
            parentGrid.State.ColumnFilterMulti ??= new();
            filter = parentGrid.State.ColumnFilterMulti;
        }
        var isSelectMultiAll = grid.State.IsSelectMultiAll == true;
        if (filter.IsSelectAll != isSelectMultiAll)
        {
            filter.IsSelectAll = isSelectMultiAll;
            filter.TextList = new();
            result = true;
        }
        foreach (var item in textList)
        {
            if ((item.IsSelect ^ isSelectMultiAll) && !filter.TextList.Contains(item.Text))
            {
                result = true;
                filter.TextList.Add(item.Text);
            }
            if (!(item.IsSelect ^ isSelectMultiAll) && filter.TextList.Contains(item.Text))
            {
                result = true;
                filter.TextList.Remove(item.Text);
            }
        }
        if (filter.TextList.Count == 0 && filter.IsSelectAll == true)
        {
            parentGrid.State.FilterMultiList.Remove(fieldName);
        }
        return result;
    }

    /// <summary>
    /// Render data grid.
    /// </summary>
    public static void Render(GridDto grid, List<Dynamic> dataRowList, GridConfig config)
    {
        grid.Clear();
        var columnList = config.ColumnListGet(grid);
        // ColumnSortList
        var columnSortList = columnList.OrderBy(item => item.Sort).ToList();
        // RowKey
        var columnRowKey = columnList.Where(item => item.FieldName == config.FieldNameRowKey).SingleOrDefault();
        // Render Column
        grid.AddRow();
        grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonColumn });
        if (config.IsAllowNew)
        {
            grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonCustom, Text = "New", Name = "New" });
        }
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
            foreach (var column in columnList.OrderBy(item => item.Sort).ThenBy(item => item.FieldName))
            {
                var text = dataRow.ContainsKey(column.FieldName!) ? dataRow[column.FieldName!]?.ToString() : null;
                var cellEnum = column.IsAutocomplete ? GridCellEnum.FieldAutocomplete : GridCellEnum.Field;
                if (columnRowKey == null)
                {
                    grid.AddCell(new GridCellDto { CellEnum = cellEnum, Text = text, FieldName = column.FieldName, DataRowIndex = dataRowIndex });
                }
                else
                {
                    var rowKey = dataRow.ContainsKey(columnRowKey.FieldName!) ? dataRow[columnRowKey.FieldName!]?.ToString() : null;
                    grid.AddCell(new GridCellDto { CellEnum = cellEnum, Text = text, FieldName = column.FieldName, DataRowIndex = dataRowIndex, TextPlaceholder = rowKey == null ? "New" : null }, rowKey);
                }
            }
            dataRowIndex += 1;
        }
        // Render Pagination
        grid.AddRow();
        grid.AddControl(new() { ControlEnum = GridControlEnum.Pagination });
        // Render Save
        grid.AddRow();
        grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonSave });
        grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonReload });
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
    /// </summary>
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

    /// <summary>
    /// Render autocomplete data grid.
    /// </summary>
    public static void RenderAutocomplete(GridDto grid, List<Dynamic> dataRowList, string fieldName)
    {
        grid.Clear();
        // Render Data
        var dataRowIndex = 0;
        foreach (var dataRow in dataRowList)
        {
            grid.AddRow();
            var text = dataRow[fieldName]?.ToString();
            grid.AddCell(new() { CellEnum = GridCellEnum.Field, Text = text, DataRowIndex = dataRowIndex }, rowKey: text);
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
}