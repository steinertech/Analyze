public class GridJobService(CommandContextService context, CosmosDbService cosmosDb, CosmosDbDynamicService cosmosDbDynamic) : GridServiceBase
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
                new() { FieldName = nameof(GridJobDto.Status), ColumnEnum = GridColumnEnum.Text, IsAllowModify = true },
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
        var dataRowList = await cosmosDb.Select<GridJobDto>().ToListAsync();
        var result = UtilGridReflection.DynamicFrom(dataRowList);
        result = await UtilGrid.GridLoad2(request, result, fieldNameDistinct, config, gridEnum);
        foreach (var dataRow in result)
        {
            if (dataRow.TryGetValue(nameof(GridJobDto.Status), out var value))
            {
                if (value is GridJobStatus.Scheduled)
                {
                    dataRow.IconSet(nameof(GridJobDto.Status), "i-clock");
                }
                if (value is GridJobStatus.Running)
                {
                    dataRow.IconSet(nameof(GridJobDto.Status), "i-play");
                }
                if (value is GridJobStatus.Completed)
                {
                    dataRow.IconSet(nameof(GridJobDto.Status), "i-success");
                }
                if (value is GridJobStatus.Failed)
                {
                    dataRow.IconSet(nameof(GridJobDto.Status), "i-error");
                }
            }
        }
        return result;
    }

    protected override async Task GridSave2(GridRequest2Dto request, List<Dynamic> sourceList, GridConfig config)
    {
        await context.UserAuthAsync();
        await cosmosDbDynamic.UpsertAsync<GridJobDto>(sourceList, config);
    }
}

public enum GridJobStatus
{
    None = 0,

    Scheduled = 1,

    Running = 2,

    Completed = 3,
    
    Failed = 4,
}

public class GridJobDto : DocumentDto
{
    public string? Text { get; set; }

    public GridJobStatus? Status { get; set; }
}