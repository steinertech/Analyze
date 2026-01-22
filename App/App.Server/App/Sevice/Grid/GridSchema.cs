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

    protected override async Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, string? fieldNameDistinct, GridConfig config, GridConfigEnum configEnum, string? modalName)
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
                new() { FieldName = "Sort", ColumnEnum = GridColumnEnum.Int, IsAllowModify = true },
            ],
            IsAllowNew = true,
            IsAllowDelete = true,
            FieldNameRowKey = "Id",
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

    protected override async Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, string? fieldNameDistinct, GridConfig config, GridConfigEnum configEnum, string? modalName)
    {
        await context.UserAuthAsync();
        var list = await storage.SelectAsync<GridSchemaFieldDto>();
        var result = UtilGridReflection.DynamicFrom(list);
        result = await UtilGrid.GridLoad2(request, result, fieldNameDistinct, config, configEnum);
        var dropDownList = Enum.GetValues<GridColumnEnum>().Select(item => item.ToString() ?? null).ToList();
        result.ForEach(item => item.DropdownListSet("FieldType", dropDownList));
        return result;
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

    protected override async Task<GridConfig> Config2(GridRequest2Dto request, GridConfigEnum configEnum)
    {
        await context.UserAuthAsync();
        var result = new GridConfig()
        {
            IsAllowNew = true,
            IsAllowDelete = true,
        };
        var tableName = TableName(request, configEnum);
        var list = await storage.SelectAsync<GridSchemaFieldDto>();
        list = list.Where(item => item.TableName == tableName).OrderBy(item => item.Sort).ThenBy(item => item.FieldName).ToList();
        var columnList = new List<GridColumn>();
        foreach (var item in list)
        {
            if (item.FieldName != null)
            {
                columnList.Add(new GridColumn { FieldName = item.FieldName, ColumnEnum = GridColumnEnum.Text, IsAllowModify = true });
            }
        }
        result.ColumnList = columnList;
        // RowKey
        var field = list.OrderBy(item => item.FieldName).FirstOrDefault(item => item.IsRowKey == true);
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

    protected override void GridRender2(GridRequest2Dto request, List<Dynamic> dataRowList, GridConfig config, string? modalName)
    {
        var tableName = request.Grid.StateGet().RowKeyMasterList?["SchemaTable"];
        request.Grid.AddControl(new() { ControlEnum = GridControlEnum.Label, Text = tableName });
        base.GridRender2(request, dataRowList, config, modalName);
    }

    protected override async Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, string? fieldNameDistinct, GridConfig config, GridConfigEnum configEnum, string? modalName)
    {
        var result = await storageDynamic.SelectAsync<GridSchemaDataDto>();
        var tableName = TableName(request, configEnum);
        result = result.Where(item => object.Equals(item["TableName"], tableName)).ToList();
        result = await UtilGrid.GridLoad2(request, result, fieldNameDistinct, config, configEnum);
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
}

public class GridSchemaDataDto : TableEntityDto
{

}