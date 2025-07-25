// Import packages
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Chat;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

// Populate values from your OpenAI deployment
var configurationBuilder = new ConfigurationBuilder().AddUserSecrets<Program>();
var configuration = configurationBuilder.Build();
var modelId = configuration["modelId"]!;
var endpoint = configuration["endpoint"]!;
var apiKey = configuration["apiKey"]!;

// Create a kernel with Azure OpenAI chat completion
var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);


// Add enterprise components
// builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));

// Build the kernel
Kernel kernel = builder.Build();
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// Add a plugin (the LightsPlugin class is defined below)
kernel.Plugins.AddFromType<LightsPlugin>("Lights");

// Add a dynamic plugin for country codes
var countryParam = new KernelParameterMetadata("country") { Description = "Country for which to get telephone country code", IsRequired = true, ParameterType = typeof(string) };
var countryCodeFunction = kernel.CreateFunctionFromMethod((KernelArguments paramList) =>
    {
        var country = paramList["country"];
        return "+00";
    },
    functionName: "CountryCode",
    description: "Input parameters",
    parameters: [countryParam]
);
var telephonePlugin = kernel.CreatePluginFromFunctions("Telephone", [countryCodeFunction]);
kernel.Plugins.Add(telephonePlugin);

// Enable planning
OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() // Enable function calling
};

// Create a history store the conversation
var history = new ChatHistory();

// Initiate a back-and-forth chat
string? userInput;
do
{
    // Collect user input
    Console.Write("User > ");
    userInput = Console.ReadLine()!;

    // Add user input
    history.AddUserMessage(userInput);
    // history.AddSystemMessage("For function calling parameter translate city name always to English");

    // Serialize and deserialze chat history. (Example for session handling)
    var options = new JsonSerializerOptions { WriteIndented = true };
    string json = JsonSerializer.Serialize(history, options);
    history = JsonSerializer.Deserialize<ChatHistory>(json, options)!;

    // Get the response from the AI
    var result = await chatCompletionService.GetChatMessageContentAsync(
        history,
        executionSettings: openAIPromptExecutionSettings,
        kernel: kernel);

    // Print the results
    Console.WriteLine("Assistant > " + result);

    Console.WriteLine($"Assistant > TokenCount={(result.Metadata!["Usage"] as ChatTokenUsage)?.TotalTokenCount}");

    // Add the message from the agent to the chat history
    history.AddMessage(result.Role, result.Content ?? string.Empty);
} while (userInput is not null);

public class LightsPlugin
{
    // Mock data for the lights
    private readonly List<LightModel> lights = new()
   {
      new LightModel { Id = 1, Name = "Table Lamp", IsOn = false },
      new LightModel { Id = 2, Name = "Porch light", IsOn = false },
      new LightModel { Id = 3, Name = "Chandelier", IsOn = true }
   };

    [KernelFunction("get_lights")]
    [Description("Gets a list of lights and their current state")]
    public async Task<List<LightModel>> GetLightsAsync()
    {
        return lights;
    }

    [KernelFunction("change_state")]
    [Description("Changes the state of the light")]
    public async Task<LightModel?> ChangeStateAsync(int id, bool isOn)
    {
        var light = lights.FirstOrDefault(light => light.Id == id);

        if (light == null)
        {
            return null;
        }

        // Update the light with the new state
        light.IsOn = isOn;

        return light;
    }

    [KernelFunction("weather")]
    [Description("Gets the weather in a city")]
    public async Task<string?> GetWeather(Kernel kernel, string city)
    {
        // Option 1
        // Translate city name to English
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var result = await chatCompletionService.GetChatMessageContentAsync(
            chatHistory: new ChatHistory($"Translate the city name {city} to English. Respond with only the translated name."),
            executionSettings: new PromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.None() },
            kernel: kernel);
        var cityEnglish = result.Content;

        // Option 2
        // Add instruction to LLM above
        // history.AddSystemMessage("For function calling parameter translate city name always to English");

        // Option 3
        // For custom data vectorize data.

        if (cityEnglish == "Zurich")
        {
            return "Rainy";
        }
        if (cityEnglish == "Sydney")
        {
            return "Sunny";
        }
        if (cityEnglish == "Bern")
        {
            return "Cloudy";
        }

        return null;
    }
}

public class LightModel
{
  [JsonPropertyName("id")]
  public int Id { get; set; }

  [JsonPropertyName("name")]
  public string Name { get; set; }

  [JsonPropertyName("is_on")]
  public bool? IsOn { get; set; }
}
