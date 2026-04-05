using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class Function(DataService dataService, IServiceProvider serviceProvider, ILogger<Function> logger)
{
    [Function("data")]
    public async Task<IActionResult> RunData([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        return await UtilServer.Run(req, serviceProvider);
    }


    private HttpClient? httpClient;

    [Function("trigger")]
    public async Task RunTrigger([TimerTrigger("* * * * *")] TimerInfo timerInfo, FunctionContext context) // Package Microsoft.Azure.Functions.Worker.Extensions.Timer
    {
        var configuration = serviceProvider.GetService<Configuration>()!;
        
        logger.LogInformation($"RunTrigger (Instance={dataService.Instance}; TriggerUrl={configuration.TriggerUrl})"); // Log Analytics run query AppTraces | where Message contains "RunTrigger"

        dataService.Counter += 1;

        dataService.CounterList.Add(DateTime.UtcNow.ToString());

        // Keep warm Http
        if (configuration.TriggerUrl != null)
        {
            if (httpClient == null)
            {
                var factory = serviceProvider.GetService<IHttpClientFactory>()!;
                httpClient = factory.CreateClient();
            }
            var response = await httpClient.GetAsync(configuration.TriggerUrl);
            var responseText = await response.Content.ReadAsStringAsync();
            UtilServer.Assert(UtilServer.VersionServerFull == responseText);

            // Keep warm CosmosDb
            var cosmosDb = serviceProvider.GetService<CosmosDb>();
        }
    }
}

