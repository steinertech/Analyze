using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;

public class UtilCosmosDb
{
    public UtilCosmosDb(Configuration configuration)
    {
        var connectionString = configuration.ConnectionStringCosmosDb;
        this.ConnectionString = connectionString;
        var options = new CosmosClientOptions { SerializerOptions = new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase } };
        this.client = new CosmosClient(connectionString, options);
        this.container = client.GetContainer("db", "container");
    }

    public string ConnectionString { get; }

    private CosmosClient client;

    private Container container;

    public IQueryable<T> Select<T>(string partitionKey, string? name) where T : DocumentDto
    {
        IQueryable<T> result = container.GetItemLinqQueryable<T>();
        result = result.Where(item => item.InternalPartitionKey == partitionKey);
        result = result.Where(item => item.InternalType == typeof(T).Name);
        if (name != null)
        {
            result = result.Where(item => item.InternalKey == typeof(T).Name + "/" + name);
        }
        return result;
    }

    public async Task<T> InsertAsync<T>(string partitionKey, T item) where T : DocumentDto
    {
        item.InternalPartitionKey = partitionKey;
        item.Id = Guid.NewGuid().ToString();
        item.InternalEtag = null;
        return await container.UpsertItemAsync(item);
    }

    public async Task<T> UpdateAsync<T>(string partitionKey, T item) where T : DocumentDto
    {
        item.InternalPartitionKey = partitionKey;
        var options = new ItemRequestOptions() { IfMatchEtag = item.InternalEtag }; // Concurrency
        return await container.ReplaceItemAsync(item, item.Id, requestOptions: options);
    }

    public async Task<T> DeleteAsync<T>(string partitionKey, T item) where T : DocumentDto
    {
        item.InternalPartitionKey = partitionKey;
        return await container.DeleteItemAsync<T>(item.Id, new PartitionKey(item.InternalPartitionKey));
    }
}

public static class UtilCosmosDbExtension
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
    /// Gets or sets PartitionKey. This is the partition key (Type or Organisation). Also used for resource token to provide read access for client.
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
