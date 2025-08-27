using Azure.Data.Tables;

/// <summary>
/// Keeps TableStorage client connection.
/// </summary>
public class TableStorageClient
{
    public TableStorageClient(Configuration configuration)
    {
        var connectionString = configuration.ConnectionStringStorage;
        this.Client = new TableClient(connectionString, "App");
    }

    public TableClient Client { get; }
}
