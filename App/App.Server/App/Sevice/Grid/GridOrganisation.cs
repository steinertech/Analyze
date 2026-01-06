public class GridOrganisation(CommandContext context, CosmosDb cosmosDb): GridBase
{
    protected override Task<GridConfig> Config()
    {
        var result = new GridConfig
        {
            ColumnList =
            [
                new() { FieldName = "Organisation", ColumnEnum = GridColumnEnum.Text }
            ],
            IsAllowNew = true,
            FieldNameRowKey = "Organisation"
        };
        return Task.FromResult(result);
    }

    protected override async Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, string? fieldNameDistinct, GridConfig config, GridConfigEnum configEnum, string? modalName)
    {
        var email = (await context.UserAuthAsync()).Email;
        var list = await cosmosDb.Select<OrganisationDto>(isOrganisation: false).ToListAsync();
        list = list.Where(item => item.EmailList?.Contains(email) == true).ToList();
        var result = UtilGrid.DynamicFrom(list, (dataRowFrom, dataRowTo) =>
        {
            dataRowTo["Organisation"] = dataRowFrom.Name;
        });
        result = await UtilGrid.GridLoad2(request, result, null, config, configEnum);
        return result;
    }

    protected override async Task GridSave2(GridRequest2Dto request, List<Dynamic> sourceList, GridConfig config)
    {
        var email = (await context.UserAuthAsync()).Email;
        foreach (var item in sourceList)
        {
            if (item.DynamicEnum == DynamicEnum.Insert)
            {
                if (item.ValueModifiedGet<string>("Organisation", out var value, out var valueModified))
                {
                    var organisation = new OrganisationDto() { Id = Guid.NewGuid().ToString(), Name = valueModified, EmailList = new([email]) };
                    organisation = await cosmosDb.InsertAsync(organisation, isOrganisation: false);
                }
            }
        }
    }
}

public class GridOrganisationEmail(CommandContext context, CosmosDb cosmosDb) : GridBase
{
    protected override Task<GridConfig> Config()
    {
        var result = new GridConfig
        {
            ColumnList =
            [
                new() { FieldName = "Email", ColumnEnum = GridColumnEnum.Text }
            ],
            FieldNameRowKey = "Email",
            IsAllowNew = true,
            IsAllowDelete = true,
        };
        return Task.FromResult(result);
    }

    protected override async Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, string? fieldNameDistinct, GridConfig config, GridConfigEnum configEnum, string? modalName)
    {
        var email = (await context.UserAuthAsync()).Email;
        var result = new List<Dynamic>();
        var masterRowKey = request.Grid.StateGet().RowKeyMasterList?["Organisation"];
        if (masterRowKey != null)
        {
            var organisation = await cosmosDb.SelectByNameAsync<OrganisationDto>(masterRowKey, isOrganisation: false);
            if (organisation != null)
            {
                var list = organisation.EmailList;
                if (list?.Contains(email) == true)
                {
                    result = UtilGrid.DynamicFrom(list, (dataRowFrom, dataRowTo) =>
                    {
                        dataRowTo["Email"] = dataRowFrom;
                    });
                    result = await UtilGrid.GridLoad2(request, result, null, config, configEnum);
                }
            }
        }
        return result;
    }

    protected override async Task GridSave2(GridRequest2Dto request, List<Dynamic> sourceList, GridConfig config)
    {
        var email = (await context.UserAuthAsync()).Email;
        var masterRowKey = request.Grid.StateGet().RowKeyMasterList?["Organisation"];
        if (masterRowKey != null)
        {
            var organisation = await cosmosDb.SelectByNameAsync<OrganisationDto>(masterRowKey, isOrganisation: false);
            if (organisation != null)
            {
                var list = organisation.EmailList;
                if (list?.Contains(email) == true)
                {
                    foreach (var item in sourceList)
                    {
                        if (item.DynamicEnum == DynamicEnum.Insert)
                        {
                            if (item.ValueModifiedGet<string>("Email", out var value, out var valueModified))
                            {
                                organisation.EmailList ??= new();
                                organisation.EmailList.Add(valueModified);
                                organisation = await cosmosDb.UpdateAsync(organisation, isOrganisation: false);
                            }
                        }
                        if (item.DynamicEnum == DynamicEnum.Delete)
                        {
                            var emailRemove = item.RowKeyGet();
                            organisation.EmailList ??= new();
                            organisation.EmailList.Remove(emailRemove);
                            organisation = await cosmosDb.UpdateAsync(organisation, isOrganisation: false);
                        }
                    }
                }
            }
        }
    }
}
