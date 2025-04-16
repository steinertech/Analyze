public class CommandDebug(DataService dataService)
{
    public DebugDto Run()
    {
        var result = new DebugDto
        {
            VersionServer = UtilServer.Version,
            Instance = dataService.Instance,
            Counter = dataService.Counter,
            CounterList = dataService.CounterList,
        };
        return result;
    }
}
