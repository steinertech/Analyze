public class CommandDebug(CommandContextService context, DataService dataService, AiService ai, ConfigurationService configuration, StorageService storage)
{
    public async Task<DebugDto> Run()
    {
        var result = new DebugDto
        {
            VersionServer = UtilServer.VersionServer,
            Instance = dataService.Instance,
            Counter = dataService.Counter,
            CounterList = dataService.CounterList,
            McpUrl = configuration.McpUrl(),
            Text = await ai.AnalyzeDocumentAsync("Doc1.pdf", storage)
        };

        result.Text += "; Workflow=" + await ai.WorkflowRun();

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

    public string? AiChat { get; set; }
    
    public string? McpUrl { get; set; }

    public string? Text { get; set; }
}
