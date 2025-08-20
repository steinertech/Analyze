using Microsoft.Extensions.Configuration;

public class Configuration
{
    public Configuration(IConfiguration configuration)
    {
        // var sources = ((ConfigurationManager)configuration).Sources;
        // See also AddUserSecrets, Function App > Environment variables > Connection strings, AddAzureKeyVault
        this.ConnectionStringStorage = configuration.GetConnectionString("Storage")!; 
        this.ConnectionStringCosmosDb = configuration.GetConnectionString("CosmosDb")!;
    }

    public string ConnectionStringStorage { get; }
    public string ConnectionStringCosmosDb { get; }
}

