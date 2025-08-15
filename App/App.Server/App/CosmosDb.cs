using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Configuration;

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

    public IQueryable<IDictionary<string, object>> Select(Guid? tenantId = null, string? type = null, string? name = null)
    {
        IQueryable<IDictionary<string, object>> result = container.GetItemLinqQueryable<IDictionary<string, object>>();
        if (tenantId != null)
        {
            // PartitionKey tenant
            result = result.Where(item => (string)item["partitionKey"] == "TenantId/" + tenantId.ToString());
        }
        else
        {
            // PartitionKey class name
            if (type != null)
            {
                result = result.Where(item => (string)item["partitionKey"] == type);
            }
        }
        if (type != null)
        {
            result = result.Where(item => (string)item["type"] == type);
        }
        if (name != null)
        {
            if (type == null)
            {
                // Name needs to be defined with type!
                throw new Exception("Type not defined!");
            }
            result = result.Where(item => (string)item["key"] == type + "/" + name);
        }
        return result;
    }

    public IQueryable<T> Select<T>(Guid? tenantId = null, string? name = null) where T : DocumentDto // TODO Domain
    {
        IQueryable<T> result = container.GetItemLinqQueryable<T>();
        result = result.Where(item => item.Type == typeof(T).Name);
        if (tenantId != null)
        {
            // PartitionKey tenant
            result = result.Where(item => item.PartitionKey == "TenantId/" + tenantId.ToString());
        }
        else
        {
            // PartitionKey class name
            result = result.Where(item => item.PartitionKey == typeof(T).Name);
        }
        if (name != null)
        {
            result = result.Where(item => item.Key == typeof(T).Name + "/" + name);
        }
        return result;
    }

    private void PartitionKeySet<T>(T item, Guid? tenantId = null) where T : DocumentDto
    {
        // PartitionKey
        if (tenantId != null)
        {
            item.PartitionKey = "TenantId/" + tenantId.ToString(); // Used for read resource token.
        }
        else
        {
            item.PartitionKey = item.GetType().Name;
        }
    }

    public async Task<T> InsertAsync<T>(T item, Guid? tenantId = null) where T : DocumentDto
    {
        PartitionKeySet(item, tenantId);
        item.Id = Guid.NewGuid().ToString();
        item._etag = null;
        return await container.UpsertItemAsync(item);
    }

    public async Task<T> UpdateAsync<T>(T item, Guid? tenantId = null) where T : DocumentDto
    {
        PartitionKeySet(item, tenantId);
        var options = new ItemRequestOptions() { IfMatchEtag = item._etag }; // Concurrency
        return await container.ReplaceItemAsync(item, item.Id, requestOptions: options);
    }

    public async Task<List<T>> UpdateAsync<T>(List<T> list, Guid? tenantId = null) where T : DocumentDto
    {
        var result = new List<T>();
        foreach (var item in list)
        {
            result.Add(await UpsertAsync(item, tenantId));
        }
        return result;
    }

    public async Task<T> UpsertAsync<T>(T item, Guid? tenantId = null) where T : DocumentDto
    {
        // Upsert
        if (item.Id != null || item._etag != null)
        {
            // Item exists already
            return await UpdateAsync<T>(item, tenantId);
        }
        else
        {
            // Insert new item
            return await InsertAsync<T>(item, tenantId);
        }
    }

    public async Task<T> DeleteAsync<T>(T item) where T : DocumentDto
    {
        return await container.DeleteItemAsync<T>(item.Id, new PartitionKey(item.PartitionKey));
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

    public static async Task<T?> SingleOrDefaultAsync<T>(this IQueryable<T> querable, Guid? tenantId = null, string? name = null) where T : DocumentDto
    {
        var list = await querable.ToListAsync();
        var result = list.SingleOrDefault();
        return result;
    }

    public static async Task<List<IDictionary<string, object>>> ToListAsync(this IQueryable<IDictionary<string, object>> querable)
    {
        var result = new List<IDictionary<string, object>>();
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
}

public class DocumentDto
{
    /// <summary>
    /// Gets or sets id. This is the primary key. Unique within a partition key.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets _etag. Used for concurrency.
    /// </summary>
    public string? _etag { get; set; }

    /// <summary>
    /// Gets or sets PartitionKey. This is the partition key (Type or Tenant). Also used for resource token to provide read access for client.
    /// </summary>
    public string? PartitionKey { get; set; }

    /// <summary>
    /// Gets or sets Type. This is the class name.
    /// </summary>
    public string? Type
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
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets Key. This is unique within a partition key (Type/Name).
    /// </summary>
    public string? Key
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
