using Azure.AI.OpenAI;
using ModelContextProtocol.Client;
using OpenAI.Chat;
using OpenAI.Embeddings;
using System.Text.Json;

public class OpenAi
{
    public OpenAi(Configuration configuration)
    {
        // OpenAI
        // client = new OpenAIClient(configuration.OpenAiApiKey);
        // Azure OpenAI
        client = new AzureOpenAIClient(new Uri(configuration.AzureOpenAiEndpoint!), new System.ClientModel.ApiKeyCredential(configuration.AzureOpenAiApiKey!));
        embeddingClient = client.GetEmbeddingClient(configuration.AzureOpenAiEmbeddingModel!);
        chatClient = client.GetChatClient(configuration.AzureOpenAiChatModel!);
        mcpUrl = configuration.McpUrl();
    }

    private readonly AzureOpenAIClient client;

    private readonly EmbeddingClient embeddingClient;

    private readonly ChatClient chatClient;

    private readonly string mcpUrl;

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var embedding = await embeddingClient.GenerateEmbeddingAsync(text);
        var result = embedding.Value.ToFloats().ToArray();
        return result;
    }

    public async Task<string> CompleteChatAsync()
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
            new UserChatMessage("Say hello and tell me a joke."),
            new UserChatMessage("Get also current version of this app.")
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
}
