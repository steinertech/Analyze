public class TableStorage(CommandContext context, TableStorageClient tableStorageClient)
{
    private string PartitionKey<T>(bool isOrganisation) where T : TableEntityDto
    {
        return context.Name(typeof(T).Name, isOrganisation, "|"); // TableStorage does not allow "/" character.
    }

    public Task<List<T>> SelectAsync<T>(FormattableString? filter = null, bool isOrganisation = true) where T : TableEntityDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        return UtilTableStorage.SelectAsync<T>(tableStorageClient.Client, partitionKey, filter);
    }

    public Task<T?> SelectByIdAsync<T>(string? id, bool isOrganisation = true) where T : TableEntityDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        return UtilTableStorage.SelectByIdAsync<T>(tableStorageClient.Client, partitionKey, id);
    }

    public async Task InsertAsync<T>(T item, bool isOrganisation = true) where T : TableEntityDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        await UtilTableStorage.InsertAsync(tableStorageClient.Client, partitionKey, item);
    }

    public async Task UpdateAsync<T>(T item, bool isOrganisation = true) where T : TableEntityDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        await UtilTableStorage.UpdateAsync(tableStorageClient.Client, partitionKey, item);
    }

    public async Task DeleteAsync<T>(T item, bool isOrganisation = true) where T : TableEntityDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        await UtilTableStorage.DeleteAsync(tableStorageClient.Client, partitionKey, item);
    }
}

public class TableStorageDynamic(CommandContext context, TableStorageClient tableStorageClient)
{
    private string PartitionKey<T>(bool isOrganisation) where T : TableEntityDto
    {
        var name = isOrganisation == false ? typeof(T).Name : null;
        return context.Name(name, isOrganisation);
    }

    public Task<List<Dynamic>> SelectAsync<T>(FormattableString? filter = null, bool isOrganisation = true) where T : TableEntityDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        return UtilTableStorageDynamic.SelectAsync<T>(tableStorageClient.Client, partitionKey, filter);
    }

    public Task<Dynamic?> SelectByIdAsync<T>(string? id, bool isOrganisation = true) where T : TableEntityDto
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        return UtilTableStorageDynamic.SingleByIdAsync<T>(tableStorageClient.Client, partitionKey, id);
    }

    public async Task InsertAsync<T>(Dynamic item, bool isOrganisation = true) where T : TableEntityDto, new()
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        await UtilTableStorageDynamic.InsertAsync<T>(tableStorageClient.Client, partitionKey, item);
    }
}