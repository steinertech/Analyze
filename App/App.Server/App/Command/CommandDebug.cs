public class CommandDebug(CommandContext context, DataService dataService)
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

        if (context.RequestSessionId == null)
        {
            context.ResponseSessionId = Guid.NewGuid().ToString();
        }
        else
        {
            context.NotificationAdd("SessionId=" + context.RequestSessionId, NotificationEnum.Info);
        }

        return result;
    }
}

public class DebugDto
{
    public string? VersionServer { get; set; }
    
    public int? Instance { get; set; }

    public int? Counter { get; set; }

    public List<string>? CounterList { get; set; }
}
