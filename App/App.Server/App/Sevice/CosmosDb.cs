public class CosmosDb(CommandContext context, CosmosDbContainer cosmosDbContainer)
{
    private string PartitionKey<T>(bool isOrganisation) where T : DocumentDto
    {
        var name = isOrganisation == false ? typeof(T).Name : null;
        return context.Name(name, isOrganisation);
    }

    public IQueryable<T> Select<T>(bool isOrganisation = true) where T : DocumentDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        return UtilCosmosDb.Select<T>(cosmosDbContainer.Container, partitionKey);
    }

    public Task<T?> SelectByNameAsync<T>(string? name, bool isOrganisation = true) where T : DocumentDto
    {
        if (string.IsNullOrEmpty(name))
        {
            return Task.FromResult<T?>(null);
        }
        var partitionKey = PartitionKey<T>(isOrganisation);
        return UtilCosmosDb.Select<T>(cosmosDbContainer.Container, partitionKey, name).SingleOrDefaultAsync();
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

    public async Task<T> DeleteAsync<T>(T item, bool isOrganisation = true) where T : DocumentDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        return await UtilCosmosDb.DeleteAsync(cosmosDbContainer.Container, partitionKey, item);
    }
}

public class CosmosDbDynamic(CommandContext context, CosmosDbContainer cosmosDbContainer)
{
    private string PartitionKey<T>(bool isOrganisation) where T : DocumentDto
    {
        var name = isOrganisation == false ? typeof(T).Name : null;
        return context.Name(name, isOrganisation);
    }

    public IQueryable<IDictionary<string, object>> Select<T>(string? name = null, bool isOrganisation = true) where T : DocumentDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        return UtilCosmosDbDynamic.Select<T>(cosmosDbContainer.Container, partitionKey, name);
    }

    public async Task<IDictionary<string, object>> InsertAsync<T>(IDictionary<string, object> item, bool isOrganisation = true) where T : DocumentDto, new()
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        return await UtilCosmosDbDynamic.InsertAsync<T>(cosmosDbContainer.Container, partitionKey, item);
    }
}