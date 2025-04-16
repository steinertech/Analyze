using Microsoft.Extensions.Configuration;

public class DataService
{
    public DataService(IConfiguration configuration)
    {
        this.Instance = (int)Math.Abs((DateTime.UtcNow.Ticks % 1000));

        // var sources = ((ConfigurationManager)configuration).Sources;
        this.ConnectionStringStorage = configuration.GetConnectionString("Storage")!; // See also AddUserSecrets, AddAzureKeyVault
    }

    public string ConnectionStringStorage { get; }

    public string VersionServer => UtilServer.Version;

    public int Instance { get; }

    public int Counter;

    public List<string> CounterList = new List<string>();
}

