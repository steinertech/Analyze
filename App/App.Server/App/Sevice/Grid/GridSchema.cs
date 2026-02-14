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

    protected override async Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, GridConfigEnum configEnum, string? modalName, GridLoadArg arg)
    {
        await context.UserAuthAsync();
        var list = await storage.SelectAsync<GridSchemaTableDto>();
        var result = UtilGridReflection.DynamicFrom(list);
        result = await UtilGrid.GridLoad2(request, result, arg.FieldNameDistinct, arg.Config, configEnum);
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

    protected override async Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, GridConfigEnum configEnum, string? modalName, GridLoadArg arg)
    {
        await context.UserAuthAsync();
        var fieldList = await storage.SelectAsync<GridSchemaFieldDto>();
        if (configEnum == GridConfigEnum.GridAutocomplete)
        {
            var tableNameList = fieldList.Select(item => item.TableName).Distinct().ToList();
            var result = tableNameList.Select(item => { var result = new Dynamic(); result[arg.FieldNameDistinct!] = item; return result; }).ToList();
            result = await UtilGrid.GridLoad2(request, result, arg.FieldNameDistinct, arg.Config, GridConfigEnum.GridAutocomplete);
            return result;
        }
        else
        {
            var result = UtilGridReflection.DynamicFrom(fieldList);
            result = await UtilGrid.GridLoad2(request, result, arg.FieldNameDistinct, arg.Config, configEnum);
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

    private async Task<GridConfig> Config(string tableName)
    {
        await context.UserAuthAsync();
        var result = new GridConfig()
        {
            IsAllowNew = true,
            IsAllowDelete = true,
        };
        var fieldList = await storage.SelectAsync<GridSchemaFieldDto>();
        fieldList = fieldList.Where(item => item.TableName == tableName).OrderBy(item => item.Sort).ThenBy(item => item.FieldName).ToList();
        var columnList = new List<GridColumn>();
        foreach (var item in fieldList)
        {
            if (item.FieldName != null)
            {
                bool isRef = item.Ref != null;
                columnList.Add(new GridColumn { FieldName = item.FieldName, ColumnEnum = GridColumnEnum.Text, IsAllowModify = true, IsAutocomplete = isRef });
            }
        }
        result.ColumnList = columnList;
        // RowKey
        var field = fieldList.OrderBy(item => item.FieldName).FirstOrDefault(item => item.IsRowKey == true); // Single
        if (field?.FieldName != null)
        {
            result.FieldNameRowKey = field.FieldName;
        }
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

    protected override async Task<GridConfig> Config2(GridRequest2Dto request, GridConfigEnum configEnum)
    {
        var tableName = TableName(request, configEnum);
        ArgumentNullException.ThrowIfNull(tableName);
        var result = await Config(tableName);
        return result;
    }

    protected override void GridRender2(GridRequest2Dto request, List<Dynamic> dataRowList, GridConfig config, string? modalName)
    {
        var tableName = request.Grid.StateGet().RowKeyMasterList?["SchemaTable"];
        request.Grid.AddControl(new() { ControlEnum = GridControlEnum.Label, Text = tableName });
        base.GridRender2(request, dataRowList, config, modalName);
    }

    protected override async Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, GridConfigEnum configEnum, string? modalName, GridLoadArg arg)
    {
        var result = await storageDynamic.SelectAsync<GridSchemaDataDto>(); // Load all data
        var tableName = TableName(request, configEnum);
        if (configEnum == GridConfigEnum.GridAutocomplete)
        {
            var fieldList = await storage.SelectAsync<GridSchemaFieldDto>();
            var field = fieldList.Where(item => item.TableName == tableName && item.FieldName == arg.FieldNameDistinct).Single();
            tableName = field.Ref; // TableName ref
            arg.FieldNameDistinct = fieldList.Where(item => item.TableName == tableName && item.IsRowKey == true).Single().FieldName; // TableName ref RowKey
            ArgumentNullException.ThrowIfNull(tableName);
            arg.Config = await Config(tableName);
        }
        result = result.Where(item => object.Equals(item["TableName"], tableName)).ToList(); // Filter by TableName
        result = await UtilGrid.GridLoad2(request, result, arg.FieldNameDistinct, arg.Config, configEnum);
        return result;
    }

    protected override async Task GridSave2(GridRequest2Dto request, List<Dynamic> sourceList, GridConfig config)
    {
        await storageDynamic.UpsertAsync<GridSchemaDataDto>(sourceList, config);
    }
}

public class GridSchemaTableDto : TableEntityDto
{
    public string? TableName { get; set; }
}

public class GridSchemaFieldDto : TableEntityDto
{
    public string? TableName { get; set; }

    public string? FieldName { get; set; }
    
    public string? FieldType { get; set; }
    
    public bool? IsRowKey { get; set; }

    /// <summary>
    /// Gets or sets Sort. This is the column order.
    /// </summary>
    public int? Sort { get; set; }

    public string? Ref { get; set; }
}

public class GridSchemaDataDto : TableEntityDto
{

}