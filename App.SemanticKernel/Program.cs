// Import packages
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
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

// Enable planning
OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
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
    public async Task<string?> GetWeather(string city)
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

public class LightModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("is_on")]
    public bool? IsOn { get; set; }
}

