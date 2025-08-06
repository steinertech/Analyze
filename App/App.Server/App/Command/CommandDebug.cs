public class CommandDebug(DataService dataService, CommandContext context)
{
    public DebugDto Run()
    {
        var result = new DebugDto
        {
            VersionServer = UtilServer.VersionServer,
            Instance = dataService.Instance,
            Counter = dataService.Counter,
            CounterList = dataService.CounterList,
        };

        context.NotificationAdd("Hello from debug", NotificationEnum.Info);

        return result;
    }
}
