using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

public class CosmosDb
{
    public CosmosDb(IConfiguration configuration)
    {
        this.Configuration = configuration;

        // var sources = ((ConfigurationManager)configuration).Sources;
        connectionString = configuration.GetConnectionString("CosmosDb")!;
        var options = new CosmosClientOptions { SerializerOptions = new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase } };
        this.client = new CosmosClient(connectionString, options);
        this.container = client.GetContainer("db", "container");
    }

    public IConfiguration Configuration { get; }

    private string connectionString { get; }

    private CosmosClient client;

    private Container container;

    public IQueryable<IDictionary<string, object>> SelectDictionary<T>(CommandContext context) where T : DocumentDto
    {
        IQueryable<IDictionary<string, object>> result = container.GetItemLinqQueryable<IDictionary<string, object>>();
        result = result.Where(item => (string)item["partitionKey"] == PartionKey<T>(context));
        result = result.Where(item => (string)item["type"] == typeof(T).Name);
        return result;
    }

    public Task<IDictionary<string, object>?> SelectDictionarySingleOrDefaultAsync<T>(CommandContext context, string? name) where T : DocumentDto
    {
        var result = SelectDictionary<T>(context);
        result = result.Where(item => (string)item["key"] == typeof(T).Name + "/" + name);
        return result.SingleDictionaryOrDefaultAsync();
    }

    public IQueryable<T> Select<T>(CommandContext context) where T : DocumentDto
    {
        IQueryable<T> result = container.GetItemLinqQueryable<T>();
        result = result.Where(item => item.InternalPartitionKey == PartionKey<T>(context));
        result = result.Where(item => item.InternalType == typeof(T).Name);
        return result;
    }

    public Task<T?> SelectSingleOrDefaultAsync<T>(CommandContext context, string? name) where T : DocumentDto
    {
        var result = Select<T>(context);
        result = result.Where(item => item.InternalKey == typeof(T).Name + "/" + name);
        return result.SingleOrDefaultAsync();
    }

    private static string PartionKey<T>(CommandContext context) where T : DocumentDto
    {
        var domain = context.Domain;
        if (context.TenantId != null)
        {
            // PartitionKey tenant
            return $"{domain}/Tenant/{context.TenantId}"; // One resource token for read access to all type within a tenant
        }
        else
        {
            // PartitionKey class name
            return $"{domain}/Type/{typeof(T).Name}"; // Gets never a resource token
        }
    }

    public async Task<T> InsertAsync<T>(CommandContext context, T item) where T : DocumentDto
    {
        item.InternalPartitionKey = PartionKey<T>(context);
        item.Id = Guid.NewGuid().ToString();
        item.InternalEtag = null;
        return await container.UpsertItemAsync(item);
    }

    public async Task<IDictionary<string, object>> InsertDictionaryAsync<T>(CommandContext context, IDictionary<string, object> item) where T : DocumentDto, new()
    {
        item["partitionKey"] = PartionKey<T>(context);
        item["type"] = typeof(T).Name;
        item["id"] = Guid.NewGuid().ToString();
        item["key"] = typeof(T).Name + "/" + item["name"];
        item["_etag"] = null!;
        var result = await container.UpsertItemAsync(item);
        return result.Resource;
    }

    public async Task<T> UpdateAsync<T>(CommandContext context, T item) where T : DocumentDto
    {
        item.InternalPartitionKey = PartionKey<T>(context);
        var options = new ItemRequestOptions() { IfMatchEtag = item.InternalEtag }; // Concurrency
        return await container.ReplaceItemAsync(item, item.Id, requestOptions: options);
    }

    public async Task<List<T>> UpdateAsync<T>(CommandContext context, List<T> list) where T : DocumentDto
    {
        var result = new List<T>();
        foreach (var item in list)
        {
            result.Add(await UpsertAsync(context, item));
        }
        return result;
    }

    public async Task<T> UpsertAsync<T>(CommandContext context, T item) where T : DocumentDto
    {
        // Upsert
        if (item.Id != null || item.InternalEtag != null)
        {
            // Item exists already
            return await UpdateAsync<T>(context, item);
        }
        else
        {
            // Insert new item
            return await InsertAsync<T>(context, item);
        }
    }

    public async Task<T> DeleteAsync<T>(T item) where T : DocumentDto
    {
        return await container.DeleteItemAsync<T>(item.Id, new PartitionKey(item.InternalPartitionKey));
    }
}

public static class DocumentDbExtension
{
    public static async Task<List<T>> ToListAsync<T>(this IQueryable<T> querable) where T : DocumentDto
    {
        var result = new List<T>();
        using var feed = querable.ToFeedIterator();
        while (feed.HasMoreResults)
        {
            var response = await feed.ReadNextAsync();
            foreach (var item in response)
            {
                result.Add(item);
            }
        }
        return result;
    }

    public static async Task<T?> SingleOrDefaultAsync<T>(this IQueryable<T> querable) where T : DocumentDto
    {
        var list = await querable.ToListAsync();
        var result = list.SingleOrDefault();
        return result;
    }

    public static async Task<List<T>> ToDictionaryListAsync<T>(this IQueryable<T> querable) where T : IDictionary<string, object>
    {
        var result = new List<T>();
        using var feed = querable.ToFeedIterator();
        while (feed.HasMoreResults)
        {
            var response = await feed.ReadNextAsync();
            foreach (var item in response)
            {
                result.Add(item);
            }
        }
        return result;
    }

    public static async Task<T?> SingleDictionaryOrDefaultAsync<T>(this IQueryable<T> querable) where T : IDictionary<string, object>
    {
        var list = await querable.ToDictionaryListAsync();
        var result = list.SingleOrDefault();
        return result;
    }
}

public class DocumentDto
{
    /// <summary>
    /// Gets or sets id. This is the primary key. Unique within a partition key.
    /// </summary>
    [JsonProperty(PropertyName = "id")]
    public string? Id { get; internal set; }

    /// <summary>
    /// Gets or sets _etag. Used for concurrency.
    /// </summary>
    [JsonProperty(PropertyName = "_etag")]
    internal string? InternalEtag { get; set; }

    /// <summary>
    /// Gets or sets PartitionKey. This is the partition key (Type or Tenant). Also used for resource token to provide read access for client.
    /// </summary>
    [JsonProperty(PropertyName = "partitionKey")]
    internal string? InternalPartitionKey { get; set; }

    /// <summary>
    /// Gets or sets Type. This is the class name.
    /// </summary>
    [JsonProperty(PropertyName = "type")]
    internal string? InternalType
    {
        get
        {
            return GetType().Name;
        }
        set
        {
            // Can't be changed.
        }
    }

    /// <summary>
    /// Gets or sets Name. This (Class type + Name) is unique within a partition key. See also property Key.
    /// </summary>
    [JsonProperty(PropertyName = "name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets Key. This is unique within a partition key (Type/Name).
    /// </summary>
    [JsonProperty(PropertyName = "key")]
    internal string? InternalKey
    {
        get
        {
            return GetType().Name + "/" + Name;
        }
        set
        {
            // Can't be changed.
        }
    }
}
