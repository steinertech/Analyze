using Microsoft.Azure.Cosmos;

/// <summary>
/// Keeps CosmosDb container connection.
/// </summary>
public class CosmosDbContainer
{
    public CosmosDbContainer(Configuration configuration)
    {
        var connectionString = configuration.ConnectionStringCosmosDb;
        var options = new CosmosClientOptions 
        { 
            SerializerOptions = new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase },
            // ConnectionMode = ConnectionMode.Direct,
            // MaxRequestsPerTcpConnection = 2,
            // MaxTcpConnectionsPerEndpoint = 10,
        };
        client = new CosmosClient(connectionString, options);
    }

    private readonly CosmosClient client;

    public Container Container =>  client.GetContainer("db", "container");
}
