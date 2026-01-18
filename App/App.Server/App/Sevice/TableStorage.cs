public class TableStorage(CommandContext context, TableStorageClient tableStorageClient)
{
    private string PartitionKey<T>(bool isOrganisation) where T : TableEntityDto
    {
        // See also method TableStorageDynamic.PartitionKey();
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
        // See also method TableStorage.PartitionKey();
        return context.Name(typeof(T).Name, isOrganisation, "|"); // TableStorage does not allow "/" character.
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

    public async Task UpdateAsync<T>(Dynamic item, bool isOrganisation = true) where T : TableEntityDto, new()
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        await UtilTableStorageDynamic.UpdateAsync<T>(tableStorageClient.Client, partitionKey, item);
    }

    public async Task InsertAsync<T>(Dynamic item, bool isOrganisation = true) where T : TableEntityDto, new()
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        await UtilTableStorageDynamic.InsertAsync<T>(tableStorageClient.Client, partitionKey, item);
    }

    public async Task DeleteAsync<T>(Dynamic item, bool isOrganisation = true) where T : TableEntityDto, new()
    {
        var partitionKey = PartitionKey<T>(isOrganisation);
        await UtilTableStorageDynamic.DeleteAsync<T>(tableStorageClient.Client, partitionKey, item);
    }

    public async Task UpsertAsync<T>(List<Dynamic> sourceList, GridConfig config, Action<Dynamic>? calc = null, bool isOrganisation = true) where T : TableEntityDto, new()
    {
        foreach (var source in sourceList)
        {
            switch (source.DynamicEnum)
            {
                case DynamicEnum.Update:
                    {
                        var dest = await SelectByIdAsync<T>(source.RowKey);
                        ArgumentNullException.ThrowIfNull(dest);
                        foreach (var (fieldName, value) in source)
                        {
                            if (source.ValueModifiedGet(fieldName, out _, out var valueOriginal))
                            {
                                if (dest.ContainsKey(fieldName) && !object.Equals(valueOriginal, dest[fieldName]))
                                {
                                    throw new Exception("Value modified by someone else. Reload an try again.");
                                }
                            }
                            dest[fieldName] = value;
                        }
                        if (calc != null)
                        {
                            calc(dest);
                        }
                        await UpdateAsync<T>(dest, isOrganisation);
                    }
                    break;
                case DynamicEnum.Insert:
                    if (config.IsAllowNew)
                    {
                        var dest = new Dynamic();
                        foreach (var (fieldName, value) in source)
                        {
                            dest[fieldName] = value;
                        }
                        if (calc != null)
                        {
                            calc(dest);
                        }
                        await InsertAsync<T>(dest, isOrganisation);
                    }
                    break;
                case DynamicEnum.Delete:
                    if (config.IsAllowDelete)
                    {
                        var dest = await SelectByIdAsync<T>(source.RowKey);
                        ArgumentNullException.ThrowIfNull(dest);
                        await DeleteAsync<T>(dest);
                    }
                    break;
                default:
                    throw new Exception();
            }
        }
    }
}