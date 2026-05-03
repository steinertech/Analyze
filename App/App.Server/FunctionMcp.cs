using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

public class FunctionMcp(Configuration configuration)
{
    [Function("version")]
    public async Task<string> Version(
        [McpToolTrigger(toolName: "Version", description: "Gets current version of this app")]
        ToolInvocationContext toolContext)
    {
        // See also npx @modelcontextprotocol/inspector
        string? apiKey = null;
        if (toolContext.TryGetHttpTransport(out var transport))
        {
            var headers = transport?.Headers;
            headers?.TryGetValue("my-api-key", out apiKey);
        }
        // return $"AUTH_REQUIRED: Please log in at http://{configuration.TriggerDomainWithPort()}";
        return $"{UtilServer.VersionServer} Session={toolContext.SessionId}; Domain={configuration.TriggerDomainWithPort()}; ApiKey={apiKey}";
    }
}
