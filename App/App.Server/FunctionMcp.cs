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
            headers?.TryGetValue("my-api-key", out apiKey); // Add in VS Code or Claude Cowork
        }
        // return $"AUTH_REQUIRED: Please log in at http://example.com";
        return $"Version={UtilServer.VersionServer}; Session={toolContext.SessionId}; McpUrl={configuration.McpUrl()}; ApiKey={apiKey}";
    }
}