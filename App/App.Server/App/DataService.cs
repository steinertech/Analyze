public class DataService
{
    public DataService()
    {
        this.Instance = (int)Math.Abs((DateTime.UtcNow.Ticks % 1000));
    }

    public string VersionServer => UtilServer.Version;

    public int Instance { get; }

    public int Counter;

    public List<string> CounterList = new List<string>();
}

