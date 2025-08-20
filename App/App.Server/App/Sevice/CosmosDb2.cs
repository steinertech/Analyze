public class CosmosDb2(CommandContext context, CosmosDbContainer cosmosDbContainer)
{
    private string PartitionKey<T>(bool isOrganisation) where T : DocumentDto
    {
        var name = isOrganisation == false ? typeof(T).Name : null;
        return context.Name(name, isOrganisation);
    }

    public IQueryable<T> Select<T>(string? name = null, bool isOrganisation = true) where T : DocumentDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        return UtilCosmosDb.Select<T>(cosmosDbContainer.Container, partitionKey, name);
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