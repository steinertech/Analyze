using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class Function(IServiceProvider serviceProvider)
{
    [Function("data")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        return await UtilServer.Run(req, serviceProvider);
    }
}

