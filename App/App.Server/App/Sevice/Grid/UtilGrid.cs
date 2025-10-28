﻿using Microsoft.Azure.Cosmos.Linq;
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
    /// <param name="request">Grid with state to apply (filter, sort and pagination).</param>
    /// <param name="dataRowList">DataRowList (or query).</param>
    /// <param name="fieldNameDistinct">Used for example for filter lookup data grid. Returns one column grid.</param>
    public static async Task<List<Dynamic>> GridLoad(GridRequestDto request, List<Dynamic> dataRowList, string? fieldNameDistinct, int pageSize)
    {
        var grid = request.Grid;
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
            query = query.Where(item => (item[fieldName]!.ToString() ?? "").ToLower().Contains(text.ToLower()) == true);
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
    /// Save grid state to data row list.
    /// </summary>
    /// <returns>Returns data rows to save to database. Data rows contain only fields which changed values and RowKey.</returns>
    public static List<Dynamic> GridSave(GridRequestDto request, GridConfig config)
    {
        var result = new List<Dynamic>();
        // Update, Insert
        if (request.Grid.State?.FieldSaveList != null && request.Grid.State.RowKeyList != null)
        {
            foreach (var field in request.Grid.State.FieldSaveList)
            {
                var rowKey = request.Grid.State.RowKeyList[field.DataRowIndex!.Value];
                var configColumn = config.ColumnGet(field.FieldName!);
                if (rowKey == null)
                {
                    if (config.IsAllowNew)
                    {
                        // User added new data row.
                        var dataRow = result.SingleOrDefault(item => item.RowKey == rowKey);
                        if (dataRow == null)
                        {
                            dataRow = new Dynamic();
                            result.Add(dataRow);
                            dataRow.DynamicEnum = DynamicEnum.Insert;
                        }
                        if (configColumn.IsAllowModify)
                        {
                            var text = field.TextModified;
                            dataRow[field.FieldName!] = config.ConvertFrom(field.FieldName!, text);
                        }
                    }
                }
                else
                {
                    // User modified existing data row.
                    var dataRow = result.SingleOrDefault(item => item.RowKey == rowKey);
                    if (dataRow == null)
                    {
                        dataRow = new Dynamic();
                        result.Add(dataRow);
                        dataRow.DynamicEnum = DynamicEnum.Update;
                        dataRow.RowKey = rowKey;
                    }
                    if (configColumn.IsAllowModify)
                    {
                        var text = field.TextModified;
                        dataRow[field.FieldName!] = config.ConvertFrom(field.FieldName!, text);
                    }
                }
            }
        }
        else
        {
            // Delete
            if ((request.Control?.ControlEnum == GridControlEnum.ButtonCustom || request.Control?.ControlEnum == GridControlEnum.ButtonModal) && request.Control.Name == "Delete" && request.Grid.State?.RowKeyList != null)
            {
                var rowKey = request.Grid.State.RowKeyList[request.Cell!.DataRowIndex!.Value];
                if (config.IsAllowDelete)
                {
                    var dataRow = new Dynamic();
                    result.Add(dataRow);
                    dataRow.DynamicEnum = DynamicEnum.Delete;
                    dataRow.RowKey = rowKey;
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Load filter state from parent grid to lookup grid.
    /// </summary>
    public static void LookupFilterLoad(GridRequestDto request, List<Dynamic> dataRowList, string fieldName, bool isFilterColumn = false)
    {
        var grid = request.Grid;
        var parentGrid = request.ParentGrid!;
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
    public static bool LookupFilterSave(GridRequestDto request, string fieldName, bool isFilterColumn = false)
    {
        var grid = request.Grid;
        var parentGrid = request.ParentGrid!;
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
                filter = new() { IsSelectAll = true };
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

    public static void RenderForm(GridRequestDto request, List<Dynamic> dataRowList, GridConfig config)
    {
        var grid = request.Grid;
        grid.Clear();
        var columnList = config.ColumnListGet(grid);
        // RowKey
        var columnRowKey = config.ColumnList.Where(item => item.FieldName == config.FieldNameRowKey).SingleOrDefault();
        // Render Data
        var dataRowIndex = 0;
        foreach (var dataRow in dataRowList)
        {
            foreach (var column in columnList)
            {
                grid.AddRow();
                grid.AddControl(new() { ControlEnum = GridControlEnum.LabelCustom, Text = column.FieldName });
                grid.AddRow();
                var text = dataRow[column.FieldName]?.ToString();
                var cellEnum = column.IsAutocomplete ? GridCellEnum.FieldAutocomplete : GridCellEnum.Field;
                if (columnRowKey == null)
                {
                    grid.AddCell(new GridCellDto { CellEnum = cellEnum, Text = text, FieldName = column.FieldName, DataRowIndex = dataRowIndex });
                }
                else
                {
                    var rowKey = dataRow[columnRowKey.FieldName]?.ToString();
                    grid.AddCell(new GridCellDto { CellEnum = cellEnum, Text = text, FieldName = column.FieldName, DataRowIndex = dataRowIndex, TextPlaceholder = rowKey == null ? "New" : null }, rowKey);
                }
                dataRowIndex += 1;
            }
        }
        // Render Save
        grid.AddRow();
        grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonSave });
        grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonReload });
        grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupCancel });
    }

    /// <summary>
    /// Render data grid.
    /// </summary>
    public static void Render(GridRequestDto request, List<Dynamic> dataRowList, GridConfig config)
    {
        if (request.ParentControl?.ControlEnum == GridControlEnum.ButtonModal && request.ParentControl?.Name == "Edit")
        {
            UtilGrid.RenderForm(request, dataRowList, config);
            return;
        }
        var grid = request.Grid;
        grid.Clear();
        var columnList = config.ColumnListGet(grid);
        // RowKey
        var columnRowKey = config.ColumnList.Where(item => item.FieldName == config.FieldNameRowKey).SingleOrDefault();
        // Render Column
        grid.AddRow();
        grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonColumn });
        if (config.IsAllowNew)
        {
            grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonCustom, Text = "New", Name = "New" });
        }
        // Render Header
        grid.AddRow();
        foreach (var column in columnList)
        {
            grid.AddCell(new() { CellEnum = GridCellEnum.Header, FieldName = column.FieldName, Text = column.FieldName });
        }
        if (config.IsAllowDelete)
        {
            grid.AddCell(new() { CellEnum = GridCellEnum.HeaderEmpty, Text = "Command" });
        }
        // Render Filter
        grid.AddRow();
        foreach (var column in columnList)
        {
            grid.AddCell(new() { CellEnum = GridCellEnum.Filter, FieldName = column.FieldName, TextPlaceholder = "Search" });
        }
        if (config.IsAllowDelete)
        {
            grid.AddCell(new() { CellEnum = GridCellEnum.FilterEmpty });
        }
        // Render Data
        var dataRowIndex = 0;
        foreach (var dataRow in dataRowList)
        {
            grid.AddRow();
            foreach (var column in columnList)
            {
                var text = dataRow[column.FieldName]?.ToString();
                var cellEnum = column.IsAutocomplete ? GridCellEnum.FieldAutocomplete : GridCellEnum.Field;
                if (columnRowKey == null)
                {
                    grid.AddCell(new GridCellDto { CellEnum = cellEnum, Text = text, FieldName = column.FieldName, DataRowIndex = dataRowIndex });
                }
                else
                {
                    var rowKey = dataRow[columnRowKey.FieldName]?.ToString();
                    grid.AddCell(new GridCellDto { CellEnum = cellEnum, Text = text, FieldName = column.FieldName, DataRowIndex = dataRowIndex, TextPlaceholder = rowKey == null ? "New" : null }, rowKey);
                }
            }
            // Render Delete
            if (config.IsAllowDelete)
            {
                if (config.IsAllowDeleteConfirm == false)
                {
                    grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonCustom, Text = "Delete", Name = "Delete" }, dataRowIndex);
                }
                else
                {
                    grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonModal, Text = "Delete", Name = "Delete" }, dataRowIndex);
                }
            }
            // Render Edit Form
            if (config.IsAllowEditForm)
            {
                grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonModal, Text = "Edit", Name = "Edit" }, dataRowIndex);
                grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonModal, Text = "Open", Name = "Open" }, dataRowIndex);
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
        // Render Modal Cancel
        if (request.ParentControl?.ControlEnum == GridControlEnum.ButtonModal)
        {
            grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupCancel });
        }
        //
        RenderCalcColSpan(request);
    }

    /// <summary>
    /// Render lookup data grid. For filter and column lookup.
    /// </summary>
    public static void RenderLookup(GridRequestDto request, List<Dynamic> dataRowList, string fieldName)
    {
        var grid = request.Grid;
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
        RenderCalcColSpan(request);
    }

    /// <summary>
    /// Calc ColSpan of last cell.
    /// </summary>
    private static void RenderCalcColSpan(GridRequestDto request)
    {
        var grid = request.Grid;
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
    public static void RenderAutocomplete(GridRequestDto request, List<Dynamic> dataRowList, string fieldName)
    {
        var grid = request.Grid;
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
        RenderCalcColSpan(request);
    }
}