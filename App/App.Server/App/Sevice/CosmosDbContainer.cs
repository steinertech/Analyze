using Microsoft.Azure.Cosmos;

/// <summary>
/// Keeps CosmosDb container connection.
/// </summary>
public class CosmosDbContainer
{
    public CosmosDbContainer(Configuration configuration)
    {
        var connectionString = configuration.ConnectionStringCosmosDb;
        var options = new CosmosClientOptions { SerializerOptions = new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase } };
        var client = new CosmosClient(connectionString, options);
        this.Container = client.GetContainer("db", "container");
    }

    public Container Container { get; }
}
