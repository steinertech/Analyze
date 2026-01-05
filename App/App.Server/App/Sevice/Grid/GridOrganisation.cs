public class GridOrganisation(CosmosDb cosmosDb): GridBase
{
    protected override Task<GridConfig> Config()
    {
        var result = new GridConfig
        {
            ColumnList =
            [
                new() { FieldName = "OrganisationName", ColumnEnum = GridColumnEnum.Text }
            ],
            IsAllowNew = true
        };
        return Task.FromResult(result);
    }

    protected override async Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, string? fieldNameDistinct, GridConfig config, GridConfigEnum configEnum, string? modalName)
    {
        var list = await cosmosDb.Select<OrganisationDto>(isOrganisation: false).ToListAsync();
        var result = UtilGrid.DynamicFrom(list, (dataRowFrom, dataRowTo) =>
        {
            dataRowTo["OrganisationName"] = dataRowFrom.Name;
        });
        result = await UtilGrid.GridLoad2(request, result, null, config, configEnum);
        return result;
    }
}

public class GridOrganisationEmail(CosmosDb cosmosDb) : GridBase
{
    protected override Task<GridConfig> Config()
    {
        var result = new GridConfig
        {
            ColumnList =
            [
                new() { FieldName = "Email", ColumnEnum = GridColumnEnum.Text }
            ],
            IsAllowNew = true
        };
        return Task.FromResult(result);
    }

    protected override async Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, string? fieldNameDistinct, GridConfig config, GridConfigEnum configEnum, string? modalName)
    {
        var organisation = await cosmosDb.SelectByNameAsync<OrganisationDto>("a2", isOrganisation: false);
        var list = organisation?.EmailList ?? new();
        var result = UtilGrid.DynamicFrom(list, (dataRowFrom, dataRowTo) =>
        {
            dataRowTo["Email"] = dataRowFrom;
        });
        result = await UtilGrid.GridLoad2(request, result, null, config, configEnum);
        return result;
    }
}
