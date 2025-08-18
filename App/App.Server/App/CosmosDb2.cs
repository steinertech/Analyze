public class CosmosDb2(CommandContext context, UtilCosmosDb utilCosmosDb)
{
    private Task<string> PartitionKeyAsync<T>(bool isGlobal) where T : DocumentDto
    {
        var name = isGlobal ? typeof(T).Name : null;
        return context.OrganisationNameAsync(name, isGlobal);
    }

    public async Task<IQueryable<T>> SelectAsync<T>(string? name = null, bool isGlobal = false) where T : DocumentDto
    {
        var partitionKey = await PartitionKeyAsync<T>(isGlobal);
        return utilCosmosDb.Select<T>(partitionKey, name);
    }

    public async Task<T> InsertAsync<T>(T item, bool isGlobal = false) where T : DocumentDto
    {
        var partitionKey = await PartitionKeyAsync<T>(isGlobal);
        return await utilCosmosDb.InsertAsync(partitionKey, item);
    }

    public async Task<T> UpdateAsync<T>(T item, bool isGlobal = false) where T : DocumentDto
    {
        var partitionKey = await PartitionKeyAsync<T>(isGlobal);
        return await utilCosmosDb.UpdateAsync(partitionKey, item);
    }

    public async Task<T> DeleteAsync<T>(T item, bool isGlobal = false) where T : DocumentDto
    {
        var partitionKey = await PartitionKeyAsync<T>(isGlobal);
        return await utilCosmosDb.DeleteAsync(partitionKey, item);
    }
}