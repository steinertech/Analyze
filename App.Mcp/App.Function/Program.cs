using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using System.ComponentModel;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Configuration.AddUserSecrets(typeof(App.Function.Function).Assembly); // secrets.json // Package Microsoft.Extensions.Configuration.UserSecrets

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

// MCP Server
builder.EnableMcpToolMetadata();
builder.ConfigureMcpTool("MyTool");

// Semantic Kernel
builder.Services.AddSingleton((serviceProvider) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();

    var kernel = Kernel.CreateBuilder()
       .AddAzureOpenAIChatCompletion(
           deploymentName: configuration["openAiModelId"]!,
           endpoint: configuration["openAiEndpoint"]!,
           apiKey: configuration["openAiApiKey"]!)
       .Build();

    kernel.Plugins.AddFromType<MyPlugin>();
    return kernel!;
});

builder.Build().Run();

/// <summary>
/// Semantic kernel plugin.
/// </summary>
public class MyPlugin
{
    [KernelFunction("weather")]
    [Description("Gets current the weather in a city")]
    public string? GetWeather(string city)
    {
        if (city == "Zurich") // TODO Exact matching. Vector store.
        {
            return "Rainy";
        }
        if (city == "Sydney")
        {
            return "Sunny";
        }
        if (city == "Bern")
        {
            return "Cloudy";
        }
        return null;
    }
}

