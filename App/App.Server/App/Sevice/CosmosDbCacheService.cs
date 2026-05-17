public class CosmosDbCacheService(CosmosDbService cosmosDb, CacheService cache)
{
    private static string Key(Type type, string? name, bool isOrganisation)
    {
        UtilServer.Assert(!(name ?? "").Contains("/"));
        return $"{nameof(CosmosDbCacheService)}/Type/{type.Name}Name/{name}/IsOrganisation/{isOrganisation}";
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

    public async Task RemoveByNameAsync<T>(T item, bool isOrganisation = true) where T : DocumentDto
    {
        await cache.RemoveAsync(Key(typeof(T), item.Name, isOrganisation));
    }

    public async Task RemoveByNameAsync<T>(string? name, bool isOrganisation = true) where T : DocumentDto
    {
        await cache.RemoveAsync(Key(typeof(T), name, isOrganisation));
    }
}

public class CosmosDbDynamicCache(CosmosDbService cosmosDb, CacheService cache)
{
    public Task<Dynamic?> SelectByNameAsync<T>(string? name, bool isOrganisation = true) where T : DocumentDto
    {
        return Task.FromResult<Dynamic?>(null); // TODO
    }
}