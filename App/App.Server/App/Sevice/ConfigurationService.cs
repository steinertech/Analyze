using Microsoft.Extensions.Configuration;

public class ConfigurationService
{
    public ConfigurationService(IConfiguration configuration)
    {
        // var sources = ((ConfigurationManager)configuration).Sources;
        // See also AddUserSecrets, Function App > Environment variables > Connection strings, AddAzureKeyVault
        this.ConnectionStringStorage = configuration.GetConnectionString("Storage")!; 
        this.ConnectionStringCosmosDb = configuration.GetConnectionString("CosmosDb")!;
        this.IsDevelopment = configuration.GetValue<bool>("IsDevelopment", false);
        this.IsCache = configuration.GetValue<bool>("IsCache", false);
        this.IsCacheShared = configuration.GetValue<bool>("IsCacheShared", false);
        this.TriggerUrl = configuration.GetValue<string?>("TriggerUrl", null);
        this.AzureOpenAiEndpoint = configuration.GetValue<string?>("AzureOpenAiEndpoint", null);
        this.AzureOpenAiApiKey = configuration.GetValue<string?>("AzureOpenAiApiKey", null);
        this.AzureOpenAiEmbeddingModel = configuration.GetValue<string?>("AzureOpenAiEmbeddingModel", null);
        this.AzureOpenAiChatModel = configuration.GetValue<string?>("AzureOpenAiChatModel", null);
        this.OpenAiIsActive = configuration.GetValue<bool?>("OpenAiIsActive", null);
        this.OpenAiApiKey = configuration.GetValue<string?>("OpenAiApiKey", null);
        this.OpenAiEmbeddingModel = configuration.GetValue<string?>("OpenAiEmbeddingModel", null);
        this.OpenAiChatModel = configuration.GetValue<string?>("OpenAiChatModel", null);
        this.AzureContentUnderstandingEndpoint = configuration.GetValue<string?>("AzureContentUnderstandingEndpoint", null);
        this.AzureContentUnderstandingApiKey = configuration.GetValue<string?>("AzureContentUnderstandingApiKey", null);
    }

    public string ConnectionStringStorage { get; }
    
    public string ConnectionStringCosmosDb { get; }

    /// <summary>
    /// Gets IsDevelopment. If true, running for example on GitHub Codespaces. 
    /// See also files secrets.json and local.settings.json and generate.ts method configuration()
    /// IsDevelopment has to be identically on client and server BEFORE first request.
    /// </summary>
    public bool IsDevelopment { get; }

    /// <summary>
    /// Gets IsCache. If false, all caching is disabled.
    /// </summary>
    public bool IsCache { get; }

    /// <summary>
    /// Gets IsCacheShared. If true, cache (like Redis) is shared between server instances.
    /// If false, each server instance has it's own cache.
    /// </summary>
    public bool IsCacheShared { get; }

    /// <summary>
    /// Gets TriggerUrl. Called every minute by trigger.
    /// </summary>
    public string? TriggerUrl { get; }

    /// <summary>
    /// Returns server domain. Used for example for Mcp server for login redirect. Returns for example http://localhost:7138
    /// </summary>
    public string McpUrl()
    {
        ArgumentNullException.ThrowIfNull(TriggerUrl);
        var result = new Uri(TriggerUrl).GetLeftPart(UriPartial.Authority) + "/runtime/webhooks/mcp";
        return result;
    }

    /// <summary>
    /// Gets AzureOpenAiEndpoint. This is the Azure OpenAI for example to vectorize. See also https://ai.azure.com/resource/overview
    /// </summary>
    public string? AzureOpenAiEndpoint { get; }

    /// <summary>
    /// Gets OpenAiApiKey. Key for Azure OpenAI. See also https://ai.azure.com/resource/overview
    /// </summary>
    public string? AzureOpenAiApiKey { get; }

    /// <summary>
    /// Gets AzureOpenAiEmbeddingModel. Used to create vectors. See also https://ai.azure.com/resource/deployments
    /// </summary>
    public string? AzureOpenAiEmbeddingModel { get; }

    /// <summary>
    /// Gets AzureOpenAiModel. Used for chat. See also https://ai.azure.com/resource/deployments
    /// </summary>
    public string? AzureOpenAiChatModel { get; }

    /// <summary>
    /// Gets OpenAiIsActive. If true, use OpenAI. If false use Azure OpenAI.
    /// </summary>
    public bool? OpenAiIsActive { get; }

    /// <summary>
    /// Gets OpenAiApiKey. Key for OpenAI. See also  https://platform.openai.com/account/api-keys
    /// </summary>
    public string? OpenAiApiKey { get; }

    /// <summary>
    /// Gets OpenAiEmbeddingModel. See also https://platform.openai.com/settings/organization/limits
    /// </summary>
    public string? OpenAiEmbeddingModel { get; }

    /// <summary>
    /// Gets OpenAiModel. See also https://platform.openai.com/settings/organization/limits
    /// </summary>
    public string? OpenAiChatModel { get; }

    /// <summary>
    /// Gets AzureContentUnderstandingEndpoint. Go to Azure Portal Foundry AI Services. See also https://contentunderstanding.ai.azure.com/settings
    /// </summary>
    public string? AzureContentUnderstandingEndpoint { get; }

    /// <summary>
    /// Gets AzureContentUnderstandingApiKey. Key for Azure Foundry.
    /// </summary>
    public string? AzureContentUnderstandingApiKey { get; } 
}

