using Microsoft.Extensions.Configuration;

public class Configuration
{
    public Configuration(IConfiguration configuration)
    {
        // var sources = ((ConfigurationManager)configuration).Sources;
        // See also AddUserSecrets, Function App > Environment variables > Connection strings, AddAzureKeyVault
        this.ConnectionStringStorage = configuration.GetConnectionString("Storage")!; 
        this.ConnectionStringCosmosDb = configuration.GetConnectionString("CosmosDb")!;
        this.IsDevelopment = configuration.GetValue<bool>("IsDevelopment", false);
    }

    public string ConnectionStringStorage { get; }
    
    public string ConnectionStringCosmosDb { get; }

    /// <summary>
    /// Gets IsDevelopment. If true, running for example in GitHub Codespaces. See also file secrets.json
    /// </summary>
    public bool IsDevelopment { get; }
}

