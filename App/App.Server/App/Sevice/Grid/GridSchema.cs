public class GridSchemaTable(TableStorage storage, TableStorageDynamic storageDynamic, CommandContext context) : GridBase
{
    protected override Task<GridConfig> Config2(GridRequest2Dto request, GridConfigEnum configEnum)
    {
        var result = new GridConfig
        {
            ColumnList =
            [
                new() { FieldName = "TableName", ColumnEnum = GridColumnEnum.Text, IsAllowModify = true },
            ],
            IsAllowNew = true,
            IsAllowDelete = true,
            FieldNameRowKey = "TableName",
        };
        return Task.FromResult(result);
    }

    protected override async Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, string? fieldNameDistinct, GridConfig config, GridConfigEnum configEnum, string? modalName)
    {
        await context.UserAuthAsync();
        var list = await storage.SelectAsync<GridSchemaTableDto>();
        var result = UtilGridReflection.DynamicFrom(list);
        result = await UtilGrid.GridLoad2(request, result, null, config, GridConfigEnum.Grid);
        return result;
    }

    protected override async Task GridSave2(GridRequest2Dto request, List<Dynamic> sourceList, GridConfig config)
    {
        await context.UserAuthAsync();
        await storageDynamic.UpsertAsync<GridSchemaTableDto>(sourceList, config);
    }
}

public class GridSchemaTableDto : TableEntityDto
{
    public string? TableName { get; set; }
}

public class GridSchemaFieldDto : TableEntityDto
{
    public string? FieldName { get; set; }

    /// <summary>
    /// Gets or sets Sort. This is the column order.
    /// </summary>
    public int? Sort { get; set; }
}
