public class GridAi(CommandContext context, CosmosDbDynamic cosmosDb, OpenAi openAi) : GridBase
{
    protected override Task<GridConfig> Config2(GridRequest2Dto request, GridConfigEnum configEnum)
    {
        var result = new GridConfig
        {
            ColumnList =
            [
                new() { FieldName = "Id", ColumnEnum = GridColumnEnum.Text, IsAllowModify = true },
                new() { FieldName = "Name", ColumnEnum = GridColumnEnum.Text, IsAllowModify = true },
                new() { FieldName = "PartitionKey", ColumnEnum = GridColumnEnum.Text, IsAllowModify = true },
                new() { FieldName = "NameKey", ColumnEnum = GridColumnEnum.Text, IsAllowModify = true },
                new() { FieldName = "Text", ColumnEnum = GridColumnEnum.Text, IsAllowModify = true },
                new() { FieldName = "Vector", ColumnEnum = GridColumnEnum.Vector, IsAllowModify = true },
                new() { FieldName = "IsVector", ColumnEnum = GridColumnEnum.Bool, IsAllowModify = true }
            ],
            IsAllowNew = true,
            FieldNameRowKey = "Id",
            IsAllowDelete = true,
            Calc = async (dataRow) =>
            {
                dataRow["Vector"] = null;
                var text = (string?)dataRow["Text"];
                if (text != null)
                {
                    var vector = await openAi.GenerateEmbeddingAsync(text);
                    dataRow["Vector"] = vector;
                }
            }
        };
        return Task.FromResult(result);
    }

    protected override async Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, string? fieldNameDistinct, GridConfig config, GridConfigEnum configEnum, string? modalName, GridLoadAutocomplete? autocomplete)
    {
        await context.UserAuthAsync();
        var result = await cosmosDb.Select<GridAiDto>().ToListAsync();
        result = await UtilGrid.GridLoad2(request, result, fieldNameDistinct, config, configEnum);
        return result;
    }

    protected override async Task GridSave2(GridRequest2Dto request, List<Dynamic> sourceList, GridConfig config)
    {
        await context.UserAuthAsync();
        await cosmosDb.UpsertAsync<GridAiDto>(sourceList, config);
    }
}

public class GridAiDto : DocumentDto
{

}