using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;

namespace App.Function
{
    public class Function(ILogger<Function> logger, Kernel kernel)
    {

        /// <summary>
        /// Semantic kernel with weather plugin.
        /// </summary>
        [Function("Function")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            logger.LogInformation("C# HTTP trigger function processed a request.");

            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            // Create a history store the conversation
            var history = new ChatHistory();

            // Serialize and deserialze chat history. // TODO
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(history, options);
            history = JsonSerializer.Deserialize<ChatHistory>(json, options)!;

            // Add user input
            var userInput = "What is the weather like in Bern?";
            history.AddUserMessage(userInput);

            // Enable planning
            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            // Get the response from the AI
            var result = await chatCompletionService.GetChatMessageContentAsync(
                history,
                executionSettings: openAIPromptExecutionSettings,
                kernel: kernel);

            return new OkObjectResult($"Welcome to Azure Functions! {result}");
        }

        /// <summary>
        /// MCP Server providing a tool.
        /// </summary>
        [Function("MyTool")]
        public string MyTool([McpToolTrigger("MyTool", "Say hello")] ToolInvocationContext context)
        {
            return "Hello MyWorld";
        }
    }
}
