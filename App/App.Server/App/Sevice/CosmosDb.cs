public class CosmosDb(CommandContext context, CosmosDbContainer cosmosDbContainer)
{
    private string PartitionKey<T>(bool isOrganisation) where T : DocumentDto
    {
        var name = isOrganisation == false ? typeof(T).Name : null; // CosmosDb for Organisation one PartitionKey only to lease a read only token to access all data.
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
        var name = isOrganisation == false ? typeof(T).Name : null;
        return context.Name(name, isOrganisation);
    }

    public IQueryable<IDictionary<string, object>> Select<T>(bool isOrganisation = true) where T : DocumentDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        return UtilCosmosDbDynamic.Select<T>(cosmosDbContainer.Container, partitionKey);
    }

    public Task<IDictionary<string, object>?> SelectByNameAsync<T>(string? name, bool isOrganisation = true) where T : DocumentDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        return UtilCosmosDbDynamic.SelectByNameAsync<T>(cosmosDbContainer.Container, partitionKey, name);
    }

    public async Task<IDictionary<string, object>> InsertAsync<T>(IDictionary<string, object> item, bool isOrganisation = true) where T : DocumentDto, new()
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        return await UtilCosmosDbDynamic.InsertAsync<T>(cosmosDbContainer.Container, partitionKey, item);
    }
}