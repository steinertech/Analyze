using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.InMemory;
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

#pragma warning disable SKEXP0010 // Allow AddAzureOpenAIEmbeddingGenerator
    var builder = Kernel.CreateBuilder()
       .AddAzureOpenAIChatCompletion(
           deploymentName: configuration["openAiModelId"]!,
           endpoint: configuration["openAiEndpoint"]!,
           apiKey: configuration["openAiApiKey"]!)
       .AddAzureOpenAIEmbeddingGenerator(
           deploymentName: "text-embedding-3-small",
           endpoint: configuration["openAiEndpoint"]!,
           apiKey: configuration["openAiApiKey"]!)
       .Build();
#pragma warning restore SKEXP0010

    builder.Plugins.AddFromType<MyPlugin>();

    // Embedding and vectorize
    var embeddingService = builder.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
    var vectorStore = new InMemoryVectorStore();
    var collection = vectorStore.GetCollection<string, DataModel>("MyCollection");
    collection.EnsureCollectionExistsAsync().Wait();
    // Add embeddings
    collection.UpsertAsync(new DataModel { Key = "1", Text = "This is my house", Embedding = embeddingService.GenerateAsync("This is my house").Result.Vector });
    collection.UpsertAsync(new DataModel { Key = "2", Text = "I'm going to the gas station", Embedding = embeddingService.GenerateAsync("I'm going to the gas station").Result.Vector });
    collection.UpsertAsync(new DataModel { Key = "3", Text = "Tomorrow is Sunday", Embedding = embeddingService.GenerateAsync("Tomorrow is Sunday").Result.Vector });

    var queryEmbedding = embeddingService.GenerateAsync("Ich brauche Benzin").Result;
    var result = Util.ConvertToList(collection.SearchAsync(queryEmbedding.Vector, 5));

    var queryEmbedding2 = embeddingService.GenerateAsync(["Das Haus"]).Result; // Calculate vector for query. Like this it allows for token count on vectorization.
    var result2 = Util.ConvertToList(collection.SearchAsync(queryEmbedding2[0].Vector, 5));
    var tokenCount = queryEmbedding2.Usage!.TotalTokenCount;

    return builder!;
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

public sealed class DataModel
{
    [VectorStoreKey]
    public string Key { get; set; } = default!;

    [VectorStoreData]
    public string Text { get; set; } = default!;

    [VectorStoreVector(1536)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}

public static class Util
{
    public static List<T> ConvertToList<T>(IAsyncEnumerable<T> source) // TODO Remove and use it in async function only.
    {
        var list = new List<T>();
        var enumerator = source.GetAsyncEnumerator();
        while (true)
        {
            if (!enumerator.MoveNextAsync().AsTask().GetAwaiter().GetResult())
                break;

            list.Add(enumerator.Current);
        }
        return list;
    }
}