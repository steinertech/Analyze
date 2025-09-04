public class CosmosDbCache(CosmosDb cosmosDb, Cache cache)
{
    private static string Key(Type type, string? name, bool isOrganisation)
    {
        UtilServer.Assert(!(name ?? "").Contains("/"));
        return $"{nameof(CosmosDbCache)}/Type/{type.Name}Name/{name}/IsOrganisation/{isOrganisation}";
    }

    public async Task<T?> SelectByNameAsync<T>(string? name, bool isOrganisation = true) where T : DocumentDto
    {
        var result = await cache.GetAsync<T>(Key(typeof(T), name, isOrganisation));
        if (result != null)
        {
            return result;
        }
        result = await cosmosDb.SelectByNameAsync<T>(name, isOrganisation);
        await cache.SetAsync(Key(typeof(T), name, isOrganisation), result);
        return result;
    }

    public async Task RemoveByNameAsync<T>(string? name, bool isOrganisation = true) where T : DocumentDto
    {
        await cache.RemoveAsync(Key(typeof(T), name, isOrganisation));
    }
}

public class CosmosDbDynamicCache(CosmosDb cosmosDb, Cache cache)
{
    public Task<IDictionary<string, object>?> SelectByNameAsync<T>(string? name, bool isOrganisation = true) where T : DocumentDto
    {
        return Task.FromResult<IDictionary<string, object>?>(null); // TODO
    }
}