using Azure.AI.ContentUnderstanding;
using Azure.AI.OpenAI;
using ModelContextProtocol.Client;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using System.ClientModel;
using System.Text;
using System.Text.Json;

public class AiService
{
    public AiService(ConfigurationService configuration)
    {
        Configuration = configuration;
        var openAiIsEnabled = configuration.OpenAiIsEnabled == true;
        if (openAiIsEnabled == false)
        {
            // Azure OpenAI
            client = new AzureOpenAIClient(new Uri(Configuration.AzureOpenAiEndpoint!), new ApiKeyCredential(Configuration.AzureOpenAiApiKey!));
            embeddingClient = client.GetEmbeddingClient(Configuration.AzureOpenAiEmbeddingModel!);
            chatClient = client.GetChatClient(Configuration.AzureOpenAiChatModel!);
        }
        else
        {
            // OpenAI
            client = new OpenAIClient(new ApiKeyCredential(Configuration.OpenAiApiKey!));
            embeddingClient = client.GetEmbeddingClient(Configuration.OpenAiEmbeddingModel!);
            chatClient = client.GetChatClient(Configuration.OpenAiChatModel!);
        }
        mcpUrl = Configuration.McpUrl();
        contentUnderstandingClient = new ContentUnderstandingClient(new Uri(Configuration.AzureContentUnderstandingEndpoint!), new Azure.AzureKeyCredential(Configuration.AzureContentUnderstandingApiKey!));
    }

    public readonly ConfigurationService Configuration;

    private readonly OpenAIClient client;

    private readonly EmbeddingClient embeddingClient;

    private readonly ChatClient chatClient;

    private readonly string mcpUrl;

    private readonly ContentUnderstandingClient contentUnderstandingClient;

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var embedding = await embeddingClient.GenerateEmbeddingAsync(text);
        var result = embedding.Value.ToFloats().ToArray();
        return result;
    }

    public async Task<string> CompleteChatAsync(string text)
    {
        // Connect Mcp server
        var transport = new HttpClientTransport(new HttpClientTransportOptions() { Endpoint = new Uri(mcpUrl) });
        var mcpClient = await McpClient.CreateAsync(transport);
        var mcpTools = await mcpClient.ListToolsAsync();
        var options = new ChatCompletionOptions();
        foreach (var tool in mcpTools)
        {
            options.Tools.Add(ChatTool.CreateFunctionTool(tool.Name, tool.Description, BinaryData.FromObjectAsJson(tool.ProtocolTool.InputSchema)));
        }

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("You are a helpful assistant that greets users with 'Hello World'."),
            new UserChatMessage(text),
        };

        // Send message
        var response = await chatClient.CompleteChatAsync(messages, options);
        // Process tool calls
        while (response.Value.FinishReason == ChatFinishReason.ToolCalls)
        {
            // Add the model's tool call request to the history
            messages.Add(new AssistantChatMessage(response));
            foreach (ChatToolCall tool in response.Value.ToolCalls)
            {
                // 3. Extract arguments and call the remote MCP server
                string toolName = tool.FunctionName;
                string toolArgsJson = tool.FunctionArguments.ToString();
                var arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(toolArgsJson);
                // Execute via your IMcpClient
                var responseTool = await mcpClient.CallToolAsync(toolName, arguments);
                // 4. Add the tool's output back to the conversation history
                messages.Add(new ToolChatMessage(tool.Id, responseTool.Content[0].ToString()));
            }
            // 5. Send history back to the model to get the final (or next) response
            response = await chatClient.CompleteChatAsync(messages, options);
        }

        var result = response.Value.Content[0].Text;
        return result;
    }

    public async Task<string> AnalyzeDocumentAsync(string fileName, StorageService storage)
    {
        var result = new StringBuilder();
        var downloadUrl = storage.DownloadUrl(fileName, isOrganisation: false);
        var response = await contentUnderstandingClient.AnalyzeAsync(Azure.WaitUntil.Completed, "prebuilt-read", [new AnalysisInput { Uri = new Uri(downloadUrl) }]);
        foreach (var item in response.Value.Contents)
        {
            if (item is DocumentContent documentContent)
            {
                foreach (var page in documentContent.Pages)
                {
                    foreach (var line in page.Lines)
                    {
                        result.AppendLine(line.Content);
                    }
                }
            }
        }
        return result.ToString();
    }
}
