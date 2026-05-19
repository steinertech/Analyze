using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;
using System.Text;
using System.Text.Json;

public class CommandDebug(CommandContextService context, DataService dataService, AiService ai, ConfigurationService configuration, StorageService storage)
{
    public class A : Executor<string, string>
    {
        public A() : base("aFunc")
        {

        }

        public override async ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            //await context.RequestHaltAsync();
            return message.ToUpper();
        }
    }

    public class B : Executor<string, string>
    {
        public B() : base("bFunc")
        {

        }

        public override async ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            // await context.state.QueueStateUpdateAsync("MyBreak", "Hello");
            // await context.RequestHaltAsync();
            return message + "-B";
        }
    }

    public class C : Executor<string, string>
    {
        public C() : base("cFunc")
        {

        }

        public override async ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            var state = await context.ReadStateAsync<string>("MyBreak");
            // await context.RequestHaltAsync();
            return message + "-C";
        }
    }

    public class MyStore : ICheckpointStore<JsonElement>
    {
        public List<string> JsonList = new List<string>();

        public string? SessionId;
        
        public ValueTask<CheckpointInfo> CreateCheckpointAsync(string sessionId, JsonElement value, CheckpointInfo? parent = null)
        {
            if (SessionId == null)
            {
                SessionId = sessionId;
            }
            else
            {
                if (SessionId != sessionId)
                {
                    throw new Exception();
                }
            }

            var json = JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });
            JsonList.Add(json);
            var index = JsonList.IndexOf(json);
            var result = new CheckpointInfo(sessionId, index.ToString());
            return new ValueTask<CheckpointInfo>(result);
        }

        public ValueTask<JsonElement> RetrieveCheckpointAsync(string sessionId, CheckpointInfo key)
        {
            if (SessionId != sessionId)
            {
                throw new Exception();
            }
            var result =JsonSerializer.Deserialize<JsonElement>(JsonList[int.Parse(key.CheckpointId)]);
            return new ValueTask<JsonElement>(result);
        }

        public ValueTask<IEnumerable<CheckpointInfo>> RetrieveIndexAsync(string sessionId, CheckpointInfo? withParent = null)
        {
            if (SessionId != sessionId)
            {
                throw new Exception();
            }
            var result = JsonList.Select(item => new CheckpointInfo(sessionId, JsonList.IndexOf(item).ToString()) { });
            return new ValueTask<IEnumerable<CheckpointInfo>>(result);
        }
    }

    public async Task<DebugDto> Run()
    {
        StringBuilder d = new StringBuilder();

        // 1. Define Step A: Transform input string to Uppercase
        var aExecutor = new A().BindExecutor();

        // 2. Define Step B: Reverse the processed string text
        var bExecutor = new B().BindExecutor();
        var cExecutor = new C().BindExecutor();

        // 3. Orchestrate the graph using the WorkflowBuilder
        // Set 'uppercaseExecutor' as the root entry node
        WorkflowBuilder builder = new(aExecutor);

        // Connect Step A -> Step B, and mark Step B as the ultimate return value
        builder.AddEdge(aExecutor, bExecutor);
        builder.AddEdge(aExecutor, bExecutor, condition: (string? ctx) => true);
        builder.AddEdge(bExecutor, cExecutor);

        var workflow = builder.Build();

        d.AppendLine("Executing Microsoft Agent Workflow Engine...\n");

        // var store = new DirectoryInfo(@"C:\Temp\MyStore");
        var myJsonStore = new MyStore(); // new FileSystemJsonCheckpointStore(store);
        var manager = CheckpointManager.CreateJson(myJsonStore);

        {
            // 4. Run the workflow using the standard InProcessExecution engine
            var run = await InProcessExecution.RunAsync(workflow, "my", manager);

            var count = 0;
            // 5. Stream and display Pregel-style Superstep completion event data
            foreach (WorkflowEvent workflowEvent in run.NewEvents)
            {
                count += 1;
                d.AppendLine(count.ToString());
                if (workflowEvent is ExecutorCompletedEvent completedStep)
                {
                    d.AppendLine($"[{completedStep.ExecutorId}] processed data: \"{completedStep.Data}\"");
                }
                if (workflowEvent is SuperStepCompletedEvent superStepEvent)
                {
                    var savedCheckpoint = superStepEvent.CompletionInfo?.Checkpoint;
                    d.AppendLine($"[Engine] Superstep done. Saved Checkpoint ID: {savedCheckpoint?.CheckpointId}");
                }
            }
            await run.DisposeAsync();
        }

        {
            var ci = new CheckpointInfo(myJsonStore.SessionId!, (myJsonStore.JsonList.Count - 1).ToString());
            // 4. Run the workflow using the standard InProcessExecution engine
            manager = CheckpointManager.CreateJson(myJsonStore);
            var run = await InProcessExecution.ResumeStreamingAsync(workflow, ci, manager);

            var count = 0;
            // 5. Stream and display Pregel-style Superstep completion event data
            await foreach (WorkflowEvent workflowEvent in run.WatchStreamAsync())
            {
                count += 1;
                d.AppendLine(count.ToString());
                if (workflowEvent is ExecutorCompletedEvent completedStep)
                {
                    d.AppendLine($"[{completedStep.ExecutorId}] processed data: \"{completedStep.Data}\"");
                }
            }

            await run.DisposeAsync();
        }


        var d2 = d.ToString();

        var result = new DebugDto
        {
            VersionServer = UtilServer.VersionServer,
            Instance = dataService.Instance,
            Counter = dataService.Counter,
            CounterList = dataService.CounterList,
            McpUrl = configuration.McpUrl(),
            Text = await ai.AnalyzeDocumentAsync("Doc1.pdf", storage)
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
