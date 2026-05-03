using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using System.Text;

public class FunctionMcp(Configuration configuration)
{
    [Function("app")]
    public async Task<string> GetVersion(
        [McpToolTrigger(toolName: "Version", description: "Gets current version of this app")] ToolInvocationContext context)
        // [McpToolProperty(propertyName: "location", description: "The city name", isRequired: true)] string location)
    {
        ArgumentException.ThrowIfNullOrEmpty(context.SessionId);
        var aiSessionId = Convert.ToBase64String(Encoding.UTF8.GetBytes(context.SessionId));
        var domain = configuration.TriggerDomainWithPort();
        return $"{UtilServer.VersionServer} Session={context.SessionId};";
        // return $"AUTH_REQUIRED: Please log in at https://{domain}/ai and enter this token {aiSessionId}";
    }

    [Function("app2")]
    public string Description([McpToolTrigger(toolName: "Description", description: "Description of this MCP server")] ToolInvocationContext context)
    {
        return $"{UtilServer.VersionServer} Session={context.SessionId}; Server={configuration.TriggerDomainWithPort()}";
    }
}