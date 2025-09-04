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
        this.IsCache = configuration.GetValue<bool>("IsCache", false);
        this.IsCacheShared = configuration.GetValue<bool>("IsCacheShared", false);
    }

    public string ConnectionStringStorage { get; }
    
    public string ConnectionStringCosmosDb { get; }

    /// <summary>
    /// Gets IsDevelopment. If true, running for example on GitHub Codespaces. 
    /// See also files secrets.json and local.settings.json and generate.ts method configuration()
    /// IsDevelopment has to be identically on client and server BEFORE first request.
    /// </summary>
    public bool IsDevelopment { get; }

    /// <summary>
    /// Gets or sets IsCache. If false, all caching is disabled.
    /// </summary>
    public bool IsCache { get; }

    /// <summary>
    /// Gets or sets IsCacheShared. If true, cache (like Redis) is shared between server instances.
    /// If false, each server instance has it's own cache.
    /// </summary>
    public bool IsCacheShared { get; }
}

