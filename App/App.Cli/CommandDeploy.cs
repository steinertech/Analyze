using Microsoft.Extensions.Configuration;
using System.Diagnostics;

public static class CommandDeploy
{
    public static void Run(string functionName, string resourceGroup)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("local.settings.json", optional: true)
            .AddUserSecrets(System.Reflection.Assembly.GetExecutingAssembly(), optional: true)
            .Build();

        var settings = new List<string>();
        var connectionStrings = new List<string>();

        // Azure Functions local.settings.json stores app settings under "Values" (flat).
        // Fall back to top-level keys for standard appsettings.json.
        var valuesSection = configuration.GetSection("Values");
        if (valuesSection.GetChildren().Any())
        {
            foreach (var kv in valuesSection.AsEnumerable(makePathsRelative: true))
            {
                if (kv.Value is null) continue;
                settings.Add($"{kv.Key.Replace(":", "__")}={kv.Value}");
            }
        }
        else
        {
            foreach (var kv in configuration.AsEnumerable())
            {
                if (kv.Value is null) continue;
                if (kv.Key.StartsWith("ConnectionStrings:", StringComparison.OrdinalIgnoreCase)) continue;
                settings.Add($"{kv.Key.Replace(":", "__")}={kv.Value}");
            }
        }

        // ConnectionStrings go via `connection-string set` (not appsettings) because Azure
        // rejects reserved prefixes like CUSTOMCONNSTR_ as plain app settings.
        // Handles both simple ("Name": "connstr") and complex ("Name": { "ConnectionString": "..." }).
        foreach (var kv in configuration.GetSection("ConnectionStrings").AsEnumerable(makePathsRelative: true))
        {
            if (kv.Value is null) continue;

            if (!kv.Key.Contains(':'))
            {
                connectionStrings.Add($"{kv.Key}={kv.Value}");
            }
            else if (kv.Key.EndsWith(":ConnectionString", StringComparison.OrdinalIgnoreCase))
            {
                var name = kv.Key[..kv.Key.LastIndexOf(':')];
                connectionStrings.Add($"{name}={kv.Value}");
            }
        }

        if (settings.Count == 0 && connectionStrings.Count == 0)
        {
            Console.WriteLine("No settings found to deploy.");
            return;
        }

        if (settings.Count > 0)
        {
            Console.WriteLine($"Deploying {settings.Count} app settings to '{functionName}' in '{resourceGroup}'...");
            var azArgs = new List<string>
            {
                "functionapp", "config", "appsettings", "set",
                "--name", functionName,
                "--resource-group", resourceGroup,
                "--settings"
            };
            azArgs.AddRange(settings);
            int exitCode = RunAz([.. azArgs]);
            if (exitCode != 0)
                Console.Error.WriteLine($"az appsettings exited with code {exitCode}");
        }

        if (connectionStrings.Count > 0)
        {
            Console.WriteLine($"Deploying {connectionStrings.Count} connection strings to '{functionName}' in '{resourceGroup}'...");
            var azArgs = new List<string>
            {
                "webapp", "config", "connection-string", "set",
                "--name", functionName,
                "--resource-group", resourceGroup,
                "--connection-string-type", "Custom",
                "--settings"
            };
            azArgs.AddRange(connectionStrings);
            int exitCode = RunAz([.. azArgs]);
            if (exitCode != 0)
                Console.Error.WriteLine($"az connection-string exited with code {exitCode}");
        }
    }

    private static int RunAz(params string[] azArgs)
    {
        var psi = new ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        if (OperatingSystem.IsWindows())
        {
            psi.FileName = "cmd";
            psi.ArgumentList.Add("/c");
            psi.ArgumentList.Add("az");
        }
        else
        {
            psi.FileName = "az";
        }

        foreach (var arg in azArgs)
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi)!;
        process.OutputDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) Console.Error.WriteLine(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        return process.ExitCode;
    }
}
