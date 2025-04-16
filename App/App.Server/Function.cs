using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

public class Function(DataService dataService, IServiceProvider serviceProvider, ILogger<Function> logger)
{
    [Function("data")]
    public async Task<IActionResult> RunData([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        return await UtilServer.Run(req, serviceProvider);
    }

    [Function("trigger")]
    public void RunTrigger([TimerTrigger("* * * * *")] TimerInfo timerInfo, FunctionContext context) // Package Microsoft.Azure.Functions.Worker.Extensions.Timer
    {
        logger.LogInformation("RunTrigger");

        dataService.Counter += 1;

        dataService.CounterList.Add(DateTime.UtcNow.ToString());
    }
}

