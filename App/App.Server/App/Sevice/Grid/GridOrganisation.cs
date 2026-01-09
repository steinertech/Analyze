public class GridOrganisation(CommandContext context, CosmosDb cosmosDb) : GridBase
{
    protected override Task<GridConfig> Config()
    {
        var result = new GridConfig
        {
            ColumnList =
            [
                new() { FieldName = "Organisation", ColumnEnum = GridColumnEnum.Text },
                new() { FieldName = "Text", ColumnEnum = GridColumnEnum.Text, IsAllowModify = true }
            ],
            IsAllowNew = true,
            FieldNameRowKey = "Organisation",
            IsAllowDelete = true, // TODO Remove. Used to make command column appear
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
            dataRowTo["Text"] = dataRowFrom.Text;
        });
        result = await UtilGrid.GridLoad2(request, result, null, config, configEnum);
        return result;
    }

    protected override async Task GridSave2Custom(GridRequest2Dto request, GridButtonCustom? buttonCustomClick, List<FieldCustomSaveDto> fieldCustomSaveList, string? modalName)
    {
        // Button Select
        if (buttonCustomClick?.Control.Name == "Select")
        {
            var dataRowIndex = buttonCustomClick.Cell.DataRowIndex.GetValueOrDefault(-1);
            var rowKey = request.Grid.State?.RowKeyList?[dataRowIndex];
            if (rowKey != null)
            {
                await context.OrganisationSwitch(rowKey);
            }
        }
    }

    protected override async Task GridSave2(GridRequest2Dto request, List<Dynamic> sourceList, GridConfig config)
    {
        var email = (await context.UserAuthAsync()).Email;
        foreach (var item in sourceList)
        {
            // Update
            if (item.DynamicEnum == DynamicEnum.Update)
            {
                var organisation = await cosmosDb.SelectByNameAsync<OrganisationDto>(item.RowKey, isOrganisation: false);
                if (organisation?.EmailList?.Contains(email) != null)
                {
                    if (item.ValueModifiedGet<string>("Text", out _, out var valueText))
                    {
                        organisation.Text = valueText;
                    }
                    organisation = await cosmosDb.UpdateAsync(organisation, isOrganisation: false);
                }
            }
            // Insert
            if (item.DynamicEnum == DynamicEnum.Insert)
            {
                var organisation = new OrganisationDto() { Id = Guid.NewGuid().ToString(), EmailList = new([email]) };
                if (item.ValueModifiedGet<string>("Organisation", out _, out var valueOrganisation))
                {
                    organisation.Name = valueOrganisation;
                }
                if (item.ValueModifiedGet<string>("Text", out _, out var valueText))
                {
                    organisation.Text = valueText;
                }
                OrganisationDto.Sanitize(organisation);
                organisation = await cosmosDb.InsertAsync(organisation, isOrganisation: false);
            }
        }
    }

    protected override void GridRender2(GridRequest2Dto request, List<Dynamic> dataRowList, GridConfig config, string? modalName)
    {
        base.GridRender2(request, dataRowList, config, modalName);
        if (request.Grid.RowCellList != null)
        {
            foreach (var row in request.Grid.RowCellList)
            {
                var dataRowIndex = row.Last().DataRowIndex;
                if (dataRowIndex != null)
                {
                    row.AddControl(new() { ControlEnum = GridControlEnum.ButtonCustom, Text = "Select", Name = "Select" }); // TODO Without Render but with Config
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
