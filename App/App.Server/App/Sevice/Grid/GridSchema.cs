public class GridSchemaTable(TableStorage storage, TableStorageDynamic storageDynamic, CommandContext context) : GridBase
{
    protected override Task<GridConfig> Config2(GridRequest2Dto request, GridConfigEnum configEnum)
    {
        var result = new GridConfig
        {
            ColumnList =
            [
                new() { FieldName = "Id", ColumnEnum = GridColumnEnum.Text },
                new() { FieldName = "TableName", ColumnEnum = GridColumnEnum.Text, IsAllowModify = true },
            ],
            IsAllowNew = true,
            IsAllowDelete = true,
            FieldNameRowKey = "Id",
        };
        result.Calc = (Dynamic dataRow) => { dataRow["Id"] = dataRow["TableName"]; return Task.CompletedTask; };
        return Task.FromResult(result);
    }

    protected override async Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, string? fieldNameDistinct, GridConfig config, GridConfigEnum configEnum, string? modalName, GridLoadAutocomplete? autocomplete)
    {
        await context.UserAuthAsync();
        var list = await storage.SelectAsync<GridSchemaTableDto>();
        var result = UtilGridReflection.DynamicFrom(list);
        result = await UtilGrid.GridLoad2(request, result, fieldNameDistinct, config, configEnum);
        return result;
    }

    protected override async Task GridSave2(GridRequest2Dto request, List<Dynamic> sourceList, GridConfig config)
    {
        await context.UserAuthAsync();
        await storageDynamic.UpsertAsync<GridSchemaTableDto>(sourceList, config);
    }
}

public class GridSchemaField(TableStorage storage, TableStorageDynamic storageDynamic, CommandContext context) : GridBase
{
    protected override Task<GridConfig> Config2(GridRequest2Dto request, GridConfigEnum configEnum)
    {
        var result = new GridConfig
        {
            ColumnList =
            [
                new() { FieldName = "Id", ColumnEnum = GridColumnEnum.Text },
                new() { FieldName = "TableName", ColumnEnum = GridColumnEnum.Text, IsAllowModify = true },
                new() { FieldName = "FieldName", ColumnEnum = GridColumnEnum.Text, IsAllowModify = true },
                new() { FieldName = "FieldType", ColumnEnum = GridColumnEnum.Text, IsAllowModify = true, IsDropdown = true },
                new() { FieldName = "IsRowKey", ColumnEnum = GridColumnEnum.Bool, IsAllowModify = true },
                new() { FieldName = "Ref", ColumnEnum = GridColumnEnum.Text, IsAllowModify = true, IsAutocomplete = true },
                new() { FieldName = "RefDisplay1", ColumnEnum = GridColumnEnum.Text, IsAllowModify = true },
                new() { FieldName = "RefDisplay2", ColumnEnum = GridColumnEnum.Text, IsAllowModify = true },
                new() { FieldName = "Sort", ColumnEnum = GridColumnEnum.Int, IsAllowModify = true },
            ],
            IsAllowNew = true,
            IsAllowDelete = true,
            FieldNameRowKey = "Id",
            PageSize = 10,
        };
        var calc = static (Dynamic dataRow) =>
        {
            // Make unique
            dataRow["Id"] = dataRow["TableName"] + "." + dataRow["FieldName"];
            // DropDownList
            var dropDownList = Enum.GetValues<GridColumnEnum>().Select(item => item.ToString() ?? null).ToList();
            dataRow.DropdownListSet("FieldType", dropDownList);
            return Task.CompletedTask;
        };
        result.Calc = calc;
        return Task.FromResult(result);
    }

    protected override async Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, string? fieldNameDistinct, GridConfig config, GridConfigEnum configEnum, string? modalName, GridLoadAutocomplete? autocomplete)
    {
        await context.UserAuthAsync();
        var columnList = await storage.SelectAsync<GridSchemaFieldDto>();
        if (configEnum == GridConfigEnum.GridAutocomplete)
        {
            var tableNameList = columnList.Select(item => item.TableName).Distinct().ToList();
            var result = tableNameList.Select(item => { var result = new Dynamic(); result[fieldNameDistinct!] = item; return result; }).ToList();
            result = await UtilGrid.GridLoad2(request, result, fieldNameDistinct, config, GridConfigEnum.GridAutocomplete);
            return result;
        }
        else
        {
            var result = UtilGridReflection.DynamicFrom(columnList);
            result = await UtilGrid.GridLoad2(request, result, fieldNameDistinct, config, configEnum);
            var dropDownList = Enum.GetValues<GridColumnEnum>().Select(item => item.ToString() ?? null).ToList();
            result.ForEach(item => item.DropdownListSet("FieldType", dropDownList));
            return result;
        }
    }

    protected override async Task GridSave2(GridRequest2Dto request, List<Dynamic> sourceList, GridConfig config)
    {
        await context.UserAuthAsync();
        await storageDynamic.UpsertAsync<GridSchemaFieldDto>(sourceList, config);
    }

    protected override void GridRender2(GridRequest2Dto request, List<Dynamic> dataRowList, GridConfig config, string? modalName)
    {
        base.GridRender2(request, dataRowList, config, modalName);
    }
}

public class GridSchemaData(TableStorage storage, TableStorageDynamic storageDynamic, CommandContext context) : GridBase
{
    private string? TableName(GridRequest2Dto request, GridConfigEnum configEnum)
    {
        var result = configEnum == GridConfigEnum.Grid ? request.Grid.StateGet().RowKeyMasterList?["SchemaTable"] : request.ParentGrid?.StateGet().RowKeyMasterList?["SchemaTable"];
        return result;
    }

    private async Task<List<GridSchemaFieldDto>> SchemaColumnList(string tableName)
    {
        var result = await storage.SelectAsync<GridSchemaFieldDto>();
        result = result.Where(item => item.TableName == tableName).OrderBy(item => item.Sort).ThenBy(item => item.FieldName).ToList();
        return result;
    }

    private GridConfig Config(List<GridSchemaFieldDto> columnList)
    {
        var result = new GridConfig();
        var resultColumnList = new List<GridColumn>();
        foreach (var item in columnList)
        {
            if (item.FieldName != null)
            {
                bool isRef = item.Ref != null;
                resultColumnList.Add(new GridColumn { FieldName = item.FieldName, ColumnEnum = GridColumnEnum.Text, IsAllowModify = true, IsAutocomplete = isRef });
            }
        }
        result.ColumnList = resultColumnList;
        // RowKey
        var field = columnList.OrderBy(item => item.FieldName).FirstOrDefault(item => item.IsRowKey == true);
        if (field?.FieldName != null)
        {
            result.FieldNameRowKey = field.FieldName;
        }
        return result;
    }

    protected override async Task<GridConfig> Config2(GridRequest2Dto request, GridConfigEnum configEnum)
    {
        await context.UserAuthAsync();
        var tableName = TableName(request, configEnum);
        var columnList = await SchemaColumnList(tableName!);
        var result = Config(columnList);
        result.IsAllowNew = true;
        result.IsAllowDelete = true;
        // Calc
        var fieldNameRowKey = result.FieldNameRowKey;
        if (fieldNameRowKey != null)
        {
            var calc = (Dynamic dataRow) =>
            {
                // Make unique
                dataRow["Id"] = dataRow[fieldNameRowKey];
                dataRow["TableName"] = tableName;
                return Task.CompletedTask;
            };
            result.Calc = calc;
        }
        return result;
    }

    protected override void GridRender2(GridRequest2Dto request, List<Dynamic> dataRowList, GridConfig config, string? modalName)
    {
        var tableName = request.Grid.StateGet().RowKeyMasterList?["SchemaTable"];
        request.Grid.AddControl(new() { ControlEnum = GridControlEnum.Label, Text = tableName });
        base.GridRender2(request, dataRowList, config, modalName);
    }

    protected override async Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, string? fieldNameDistinct, GridConfig config, GridConfigEnum configEnum, string? modalName, GridLoadAutocomplete? autocomplete)
    {
        var resultAll = await storageDynamic.SelectAsync<GridSchemaDataDto>(); // Load all data
        var tableName = TableName(request, configEnum);
        var columnList = await SchemaColumnList(tableName!);
        if (configEnum == GridConfigEnum.GridAutocomplete)
        {
            tableName = columnList.Where(item => item.FieldName == fieldNameDistinct).Single().Ref;
            var columnListRef = await SchemaColumnList(tableName!);
            config = Config(columnListRef);
            autocomplete!.FieldName = config.FieldNameRowKey!;
            fieldNameDistinct = config.FieldNameRowKey;
        }
        var result = resultAll.Where(item => object.Equals(item["TableName"], tableName)).ToList(); // Filter by TableName
        foreach (var column in columnList)
        {
            if (!string.IsNullOrEmpty(column.Ref))
            {
                var tableNameRef = column.Ref;
                var resultRef = resultAll.Where(item => object.Equals(item["TableName"], tableNameRef)).ToList();
                var columnListRef = await SchemaColumnList(tableNameRef);
                var fieldNameRowKey = columnListRef.OrderBy(item => item.FieldName).First(item => item.IsRowKey == true).FieldName;
                foreach (var item in result)
                {
                    var fieldName = column.FieldName;
                    if (configEnum == GridConfigEnum.GridAutocomplete)
                    {
                        fieldName = fieldNameRowKey;
                    }
                    if (item.TryGetValue(fieldName!, out var value))
                    {
                        var dataRowRef = resultRef.SingleOrDefault(item => object.Equals(value, item!.GetValueOrDefault(fieldNameRowKey)));
                        var display = value?.ToString() + " - " + (dataRowRef != null ? dataRowRef!.GetValueOrDefault(column.RefDisplay1 ?? "")?.ToString() : null) + " " + (dataRowRef != null ? dataRowRef!.GetValueOrDefault(column.RefDisplay2 ?? "")?.ToString() : null);
                        item[fieldName!] = display;
                    }
                }
            }
        }
        result = await UtilGrid.GridLoad2(request, result, fieldNameDistinct, config, configEnum);
        return result;
    }

    protected override async Task GridSave2(GridRequest2Dto request, List<Dynamic> sourceList, GridConfig config)
    {
        var tableName = TableName(request, GridConfigEnum.Grid);
        var columnList = await SchemaColumnList(tableName!);
        foreach (var column in columnList)
        {
            var fieldName = column.FieldName;
            if (!string.IsNullOrEmpty(column.Ref))
            {
                foreach (var item in sourceList)
                {
                    item.ValueModifiedGet(fieldName!, out var value, out var valueOriginal);
                    value = value?.ToString()?.Split(" ")[0];
                    valueOriginal = valueOriginal?.ToString()?.Split(" - ")[0];
                    item.ValueModifiedSet(fieldName!, valueOriginal, value);
                }
            }
        }
        await storageDynamic.UpsertAsync<GridSchemaDataDto>(sourceList, config);
    }
}

public class GridSchemaTableDto : TableEntityDto
{
    public string? TableName { get; set; }
}

public class GridSchemaFieldDto : TableEntityDto // TODO Rename to GridSchemaColumnDto. Delete Db!
{
    public string? TableName { get; set; }

    public string? FieldName { get; set; }
    
    public string? FieldType { get; set; }
    
    public bool? IsRowKey { get; set; }

    /// <summary>
    /// Gets or sets Sort. This is the column order.
    /// </summary>
    public int? Sort { get; set; }

    public string? Ref { get; set; } // TODO Rename to RefTableName

    public string? RefDisplay1 { get; set; }
    
    public string? RefDisplay2 { get; set; }
}

public class GridSchemaDataDto : TableEntityDto
{

}