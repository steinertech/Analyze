using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

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
        var configuration = serviceProvider.GetRequiredService<ConfigurationService>();
        
        logger.LogInformation($"RunTrigger (Instance={dataService.Instance}; TriggerUrl={configuration.TriggerUrl})"); // Log Analytics run query AppTraces | where Message contains "RunTrigger"

        dataService.Counter += 1;

        dataService.CounterList.Add(DateTime.UtcNow.ToString());

        // Keep warm Http
        if (configuration.TriggerUrl != null)
        {
            if (httpClient == null)
            {
                var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                httpClient = factory.CreateClient();
            }
            var response = await httpClient.GetAsync(configuration.TriggerUrl);
            var responseText = await response.Content.ReadAsStringAsync();
            UtilServer.Assert(UtilServer.VersionServerFull == responseText);

            // Keep warm CosmosDb
            {
                var cosmosDb = serviceProvider.GetRequiredService<CosmosDbService>();
            }

            // Job command
            {
                foreach (var domain in configuration.DomainListGet())
                {
                    var scope = serviceProvider.CreateScope();
                    var commandContext = scope.ServiceProvider.GetRequiredService<CommandContextService>();
                    commandContext.DomainSet(domain);
                    var cosmosDb = scope.ServiceProvider.GetRequiredService<CosmosDbService>();
                    var organisationList = await cosmosDb.Select<OrganisationDto>(isOrganisation: false).ToListAsync();
                    organisationList = organisationList.Where(item => item.IsFolderCreate == true).ToList();
                    foreach (var organisation in organisationList)
                    {
                        var sessionId = Guid.NewGuid().ToString();
                        var session = new SessionDto { Id = Guid.NewGuid().ToString(), SessionId = sessionId, Name = sessionId, Email = "Job", IsSignIn = true, OrganisationName = organisation.Name };
                        session = await cosmosDb.InsertAsync(session, isOrganisation: false);
                        try
                        {
                            var request = new RequestDto { CommandName = "CommandJob", };
                            var httpRequest = new HttpRequestMessage(HttpMethod.Post, configuration.TriggerUrl)
                            {
                                Content = JsonContent.Create(request),
                            };
                            if (configuration.IsDevelopment == false)
                            {
                                httpRequest.Headers.Add("Cookie", $"SessionId={sessionId}");
                            }
                            else
                            {
                                request.DevelopmentSessionId = sessionId;
                            }
                            httpRequest.Headers.Add("Origin", $"https://{domain}");
                            await httpClient.SendAsync(httpRequest); // Call for every domain and organisation
                        }
                        finally
                        {
                            session = await cosmosDb.DeleteAsync<SessionDto>(session.Id!, isOrganisation: false);
                        }
                    }
                }
            }
        }
    }
}

