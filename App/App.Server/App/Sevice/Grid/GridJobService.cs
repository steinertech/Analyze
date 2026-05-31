public class GridJobService(CommandContextService context, CosmosDbDynamicService cosmosDb) : GridServiceBase
{
    protected override Task<GridConfig> Config2(GridRequest2Dto request, GridEnum gridEnum)
    {
        var result = new GridConfig
        {
            ColumnList =
            [
                new() { FieldName = nameof(GridJobDto.Id), ColumnEnum = GridColumnEnum.Text, IsAllowModify = true },
                new() { FieldName = nameof(GridJobDto.Name), ColumnEnum = GridColumnEnum.Text, IsAllowModify = true },
                new() { FieldName = nameof(GridJobDto.Text), ColumnEnum = GridColumnEnum.Text, IsAllowModify = true },
            ],
            IsAllowNew = true,
            FieldNameRowKey = nameof(GridJobDto.Id),
            IsAllowDelete = true
        };
        return Task.FromResult(result);
    }

    protected override async Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, string? fieldNameDistinct, GridConfig config, GridEnum gridEnum, string? modalName, GridLoadAutocomplete? autocomplete)
    {
        await context.UserAuthAsync();
        var result = await cosmosDb.Select<GridJobDto>().ToListAsync();
        result = await UtilGrid.GridLoad2(request, result, fieldNameDistinct, config, gridEnum);
        return result;
    }

    protected override async Task GridSave2(GridRequest2Dto request, List<Dynamic> sourceList, GridConfig config)
    {
        await context.UserAuthAsync();
        await cosmosDb.UpsertAsync<GridJobDto>(sourceList, config);
    }
}

public class GridJobDto : DocumentDto
{
    public string? Text { get; set; }
}