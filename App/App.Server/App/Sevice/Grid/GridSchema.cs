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
        var calc = (Dynamic dest) => { dest["Id"] = dest["TableName"]; }; // Make unique
        await storageDynamic.UpsertAsync<GridSchemaTableDto>(sourceList, config, calc);
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
                new() { FieldName = "Sort", ColumnEnum = GridColumnEnum.Int, IsAllowModify = true },
            ],
            IsAllowNew = true,
            IsAllowDelete = true,
            FieldNameRowKey = "Id",
        };
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
        var calc = (Dynamic dest) => { dest["Id"] = dest["TableName"] + "." + dest["FieldName"]; }; // Make unique
        await storageDynamic.UpsertAsync<GridSchemaFieldDto>(sourceList, config, calc);
    }

    protected override void GridRender2(GridRequest2Dto request, List<Dynamic> dataRowList, GridConfig config, string? modalName)
    {
        base.GridRender2(request, dataRowList, config, modalName);
    }
}

public class GridSchemaData(TableStorage storage, TableStorageDynamic storageDynamic, CommandContext context) : GridBase
{
    protected override async Task<GridConfig> Config2(GridRequest2Dto request, GridConfigEnum configEnum)
    {
        await context.UserAuthAsync();
        var result = new GridConfig()
        {
            IsAllowNew = true,
            IsAllowDelete = true,
        };
        var tableName = request.Grid.StateGet().RowKeyMasterList?["SchemaTable"];
        var list = await storage.SelectAsync<GridSchemaFieldDto>();
        list = list.Where(item => item.TableName == tableName).ToList();
        var columnList = new List<GridColumn>();
        foreach (var item in list)
        {
            if (item.FieldName != null)
            {
                columnList.Add(new GridColumn { FieldName = item.FieldName, ColumnEnum = GridColumnEnum.Text });
            }
        }
        result.ColumnList = columnList;
        return result;
    }

    protected override void GridRender2(GridRequest2Dto request, List<Dynamic> dataRowList, GridConfig config, string? modalName)
    {
        var tableName = request.Grid.StateGet().RowKeyMasterList?["SchemaTable"];
        request.Grid.AddControl(new() { ControlEnum = GridControlEnum.Label, Text = tableName });
        base.GridRender2(request, dataRowList, config, modalName);
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

    /// <summary>
    /// Gets or sets Sort. This is the column order.
    /// </summary>
    public int? Sort { get; set; }
}
