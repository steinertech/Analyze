using Azure;
using Azure.Data.Tables;

public static class UtilTableStorage
{
    public static string RowKey(Type type, string? id)
    {
        if (id != null)
        {
            UtilServer.Assert(!id.Contains("|"));
        }
        return $"{type.Name}|{id}";
    }

    public static string Filter(Type type, string partitionKey, FormattableString? filter)
    {
        var result = TableClient.CreateQueryFilter($"PartitionKey eq {partitionKey}");
        result += " and " + TableClient.CreateQueryFilter($"Type eq {type.Name}");
        if (filter != null)
        {
            result += " and " + TableClient.CreateQueryFilter(filter);
        }
        return result;
    }

    public static string FilterById(Type type, string partitionKey, string? id)
    {
        var result = Filter(type, partitionKey, null);
        var rowKey = RowKey(type, id);
        result += " and " + TableClient.CreateQueryFilter($"RowKey eq {rowKey}");
        return result;
    }

    public async static Task<List<T>> SelectAsync<T>(TableClient client, string partitionKey, FormattableString? filter = null) where T : TableEntityDto
    {
        // Table Storage does not support LINQ with Async.
        // Table Storage does not support server side functions like skip take distinct and contains.
        // Read all PartitionKey data to memory for further processing.

        // Filter
        var filterLocal = Filter(typeof(T), partitionKey, filter);
        // Select
        var result = new List<T>();
        await foreach (var item in client.QueryAsync<T>(filterLocal)) // Select only one property: select: new[] { "Name" }
        {
            result.Add(item);
        }
        return result;
    }

    public async static Task<T?> SelectByIdAsync<T>(TableClient client, string partitionKey, string? id) where T : TableEntityDto
    {
        // Filter
        var filter = FilterById(typeof(T), partitionKey, id);
        // Select
        var result = new List<T>();
        await foreach (var item in client.QueryAsync<T>(filter)) // Select only one property: select: new[] { "Name" }
        {
            result.Add(item);
        }
        return result.SingleOrDefault();
    }

    public static async Task InsertAsync<T>(TableClient client, string partitionKey, T item) where T : TableEntityDto
    {
        item.PartitionKey = partitionKey;
        await client.AddEntityAsync(item);
    }

    public static async Task UpdateAsync<T>(TableClient client, string partitionKey, T item) where T : TableEntityDto
    {
        item.PartitionKey = partitionKey;
        await client.UpdateEntityAsync(item, ETag.All, TableUpdateMode.Replace);
    }

    public static async Task DeleteAsync<T>(TableClient client, string partitionKey, T item) where T : TableEntityDto
    {
        item.PartitionKey = partitionKey;
        await client.DeleteEntityAsync(item);
    }
}

public static class UtilTableStorageDynamic
{
    public async static Task<List<Dynamic>> SelectAsync<T>(TableClient client, string partitionKey, FormattableString? filter = null) where T : TableEntityDto
    {
        var result = new List<Dynamic>();
        var filterLocal = UtilTableStorage.Filter(typeof(T), partitionKey, filter);
        await foreach (var item in client.QueryAsync<TableEntity>(filterLocal))
        {
            result.Add(new Dynamic(item));
        }
        return result;
    }

    public async static Task<Dynamic?> SingleByIdAsync<T>(TableClient client, string partitionKey, string? id) where T : TableEntityDto
    {
        // Filter
        var filter = UtilTableStorage.FilterById(typeof(T), partitionKey, id);
        // Select
        var result = new List<Dynamic>();
        await foreach (var item in client.QueryAsync<TableEntity>(filter)) // Select only one property: select: new[] { "Name" }
        {
            result.Add(new Dynamic(item));
        }
        return result.SingleOrDefault();
    }

    public static async Task InsertAsync<T>(TableClient client, string partitionKey, Dynamic item) where T : TableEntityDto
    {
        var entity = new TableEntity(item);
        var id = entity["Id"]?.ToString();
        entity.PartitionKey = partitionKey;
        entity.RowKey = UtilTableStorage.RowKey(typeof(T), id);
        entity[nameof(TableEntityDto.Type)] = typeof(T).Name;
        await client.AddEntityAsync(entity);
    }

    public static async Task UpdateAsync<T>(TableClient client, string partitionKey, Dynamic item) where T : TableEntityDto
    {
        var entity = new TableEntity(item);
        var id = entity["Id"]?.ToString();
        entity.PartitionKey = partitionKey;
        entity.RowKey = UtilTableStorage.RowKey(typeof(T), id);
        entity[nameof(TableEntityDto.Type)] = typeof(T).Name;
        await client.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace);
    }
}

public partial class TableEntityDto : ITableEntity
{
    /// <summary>
    /// Gets or sets Id. This is the primary key. Unique within a partition key (Type/Id).
    /// </summary>
    public string? Id { get; init; }

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
            UtilServer.Assert(value == GetType().Name);
        }
    }

    /// <summary>
    /// Gets or sets PartitionKey. This is the partition key (Type or Organisation).
    /// </summary>
    public string? PartitionKey { get; set; }
    
    /// <summary>
    /// Gets or sets RowKey. This is unique within a partition key (Type/Id).
    /// </summary>
    public string? RowKey
    {
        get
        {
            return UtilTableStorage.RowKey(this.GetType(), Id);
        }
        set
        {
            // Can't be changed.
        }
    }

    public DateTimeOffset? Timestamp { get; set; }
    
    public ETag ETag { get; set; }
}