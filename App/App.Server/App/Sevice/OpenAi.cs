using Azure.AI.OpenAI;
using OpenAI;
using OpenAI.Embeddings;

public class OpenAi
{
    public OpenAi(Configuration configuration)
    {
        // OpenAI
        // client = new OpenAIClient(configuration.OpenAiApiKey);
        // Azure OpenAI
        client = new AzureOpenAIClient(new Uri(configuration.AzureOpenAiEndpoint!), new System.ClientModel.ApiKeyCredential(configuration.AzureOpenAiApiKey!));
        embeddingClient = client.GetEmbeddingClient("text-embedding-3-small");
    }

    private readonly OpenAIClient client;

    private readonly EmbeddingClient embeddingClient;

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var embedding = await embeddingClient.GenerateEmbeddingAsync(text);
        var result = embedding.Value.ToFloats().ToArray();
        return result;
    }
}
