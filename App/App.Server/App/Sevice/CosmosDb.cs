public class CosmosDb(CommandContext context, CosmosDbContainer cosmosDbContainer)
{
    private string PartitionKey<T>(bool isOrganisation) where T : DocumentDto
    {
        // See also method CosmosDbDynamic.PartitionKey();
        var name = isOrganisation == false ? typeof(T).Name : null; // CosmosDb for one Organisation one PartitionKey only. Lease a read only token to access all data.
        return context.Name(name, isOrganisation);
    }

    public IQueryable<T> Select<T>(bool isOrganisation = true) where T : DocumentDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        return UtilCosmosDb.Select<T>(cosmosDbContainer.Container, partitionKey);
    }

    public Task<T?> SelectByIdAsync<T>(string id, bool isOrganisation = true) where T : DocumentDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        return UtilCosmosDb.SelectByIdAsync<T>(cosmosDbContainer.Container, partitionKey, id);
    }

    public Task<T?> SelectByNameAsync<T>(string? name, bool isOrganisation = true) where T : DocumentDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        return UtilCosmosDb.SelectByNameAsync<T>(cosmosDbContainer.Container, partitionKey, name);
    }

    public async Task<T> InsertAsync<T>(T item, bool isOrganisation = true) where T : DocumentDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        return await UtilCosmosDb.InsertAsync(cosmosDbContainer.Container, partitionKey, item);
    }

    public async Task<T> UpdateAsync<T>(T item, bool isOrganisation = true) where T : DocumentDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        return await UtilCosmosDb.UpdateAsync(cosmosDbContainer.Container, partitionKey, item);
    }

    public async Task<T> DeleteAsync<T>(string id, bool isOrganisation = true) where T : DocumentDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        return await UtilCosmosDb.DeleteAsync<T>(cosmosDbContainer.Container, partitionKey, id);
    }
}

public class CosmosDbDynamic(CommandContext context, CosmosDbContainer cosmosDbContainer)
{
    private string PartitionKey<T>(bool isOrganisation) where T : DocumentDto
    {
        // See also method CosmosDb.PartitionKey();
        var name = isOrganisation == false ? typeof(T).Name : null; // CosmosDb for one Organisation one PartitionKey only. Lease a read only token to access all data.
        return context.Name(name, isOrganisation);
    }

    public IQueryable<Dynamic> Select<T>(bool isOrganisation = true) where T : DocumentDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        return UtilCosmosDbDynamic.Select<T>(cosmosDbContainer.Container, partitionKey);
    }

    public Task<Dynamic?> SelectByIdAsync<T>(string? id, bool isOrganisation = true) where T : DocumentDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        return UtilCosmosDbDynamic.SelectByIdAsync<T>(cosmosDbContainer.Container, partitionKey, id);
    }

    public Task<Dynamic?> SelectByNameAsync<T>(string? name, bool isOrganisation = true) where T : DocumentDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        return UtilCosmosDbDynamic.SelectByNameAsync<T>(cosmosDbContainer.Container, partitionKey, name);
    }

    public async Task<Dynamic> InsertAsync<T>(Dynamic item, bool isOrganisation = true) where T : DocumentDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        return await UtilCosmosDbDynamic.InsertAsync<T>(cosmosDbContainer.Container, partitionKey, item);
    }

    public async Task<Dynamic> UpdateAsync<T>(Dynamic item, bool isOrganisation = true) where T : DocumentDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        return await UtilCosmosDbDynamic.UpdateAsync<T>(cosmosDbContainer.Container, partitionKey, item);
    }

    public async Task<Dynamic> DeleteAsync<T>(Dynamic item, bool isOrganisation = true) where T : DocumentDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        return await UtilCosmosDbDynamic.DeleteAsync<T>(cosmosDbContainer.Container, partitionKey, item);
    }

    public async Task UpsertAsync<T>(List<Dynamic> sourceList, GridConfig config, bool isOrganisation = true) where T : DocumentDto
    {
        foreach (var source in sourceList)
        {
            switch (source.DynamicEnum)
            {
                case DynamicEnum.Update:
                    {
                        var dest = await SelectByIdAsync<T>(source.RowKey);
                        ArgumentNullException.ThrowIfNull(dest);
                        foreach (var (fieldName, value) in source)
                        {
                            if (source.ValueModifiedGet(fieldName, out _, out var valueOriginalSource))
                            {
                                var valueOriginalDest = dest.GetValueOrDefault(fieldName);
                                if (dest.ContainsKey(fieldName) && !object.Equals(valueOriginalSource, valueOriginalDest))
                                {
                                    throw new Exception("Value modified by someone else. Reload an try again.");
                                }
                            }
                            dest[fieldName] = value;
                        }
                        if (config.Calc != null)
                        {
                            await config.Calc(dest);
                        }
                        var result = await UpdateAsync<T>(dest, isOrganisation);
                    }
                    break;
                case DynamicEnum.Insert:
                    if (config.IsAllowNew)
                    {
                        var dest = new Dynamic();
                        foreach (var (fieldName, value) in source)
                        {
                            dest[fieldName] = value;
                        }
                        if (config.Calc != null)
                        {
                            await config.Calc(dest);
                        }
                        var result = await InsertAsync<T>(dest, isOrganisation);
                    }
                    break;
                case DynamicEnum.Delete:
                    if (config.IsAllowDelete)
                    {
                        var dest = await SelectByIdAsync<T>(source.RowKey);
                        ArgumentNullException.ThrowIfNull(dest);
                        var result = await DeleteAsync<T>(dest, isOrganisation);
                    }
                    break;
                default:
                    throw new Exception();
            }
        }
    }
}