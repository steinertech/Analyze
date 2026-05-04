using Azure.AI.OpenAI;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;

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
    }

    private readonly OpenAIClient client;

    private readonly EmbeddingClient embeddingClient;

    private readonly ChatClient chatClient;

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var embedding = await embeddingClient.GenerateEmbeddingAsync(text);
        var result = embedding.Value.ToFloats().ToArray();
        return result;
    }

    public async Task<string> CompleteChatAsync()
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("You are a helpful assistant that greets users with 'Hello World'."),
            new UserChatMessage("Say hello and tell me a joke.")
        };

        var response = await chatClient.CompleteChatAsync(messages);
        var result = response.Value.Content.Last().Text;

        return result;
    }
}
