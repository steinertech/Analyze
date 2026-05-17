public class CommandDebug(CommandContext context, DataService dataService, OpenAi openAi, Configuration configuration, Storage storage)
{
    public async Task<DebugDto> Run()
    {
        var result = new DebugDto
        {
            VersionServer = UtilServer.VersionServer,
            Instance = dataService.Instance,
            Counter = dataService.Counter,
            CounterList = dataService.CounterList,
            AiChat = await openAi.CompleteChatAsync(),
            McpUrl = configuration.McpUrl(),
            Text = await openAi.AnalyzeDocumentAsync("Doc1.pdf", storage)
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

    public string? AiChat { get; set; }
    
    public string? McpUrl { get; set; }

    public string? Text { get; set; }
}
