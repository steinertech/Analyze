using Azure.Identity; // Used for AddAzureKeyVault
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

public static class UtilServer
{
    public static string Version
    {
        get
        {
            return "1.0.12";
        }
    }

    /// <summary>
    /// App start config.
    /// </summary>
    public static void AppConfigure(FunctionsApplicationBuilder builder)
    {
        builder.Configuration.AddUserSecrets(typeof(Function).Assembly); // secrets.json // Package Microsoft.Extensions.Configuration.UserSecrets
        // builder.Configuration.AddAzureKeyVault(new Uri("https://stc001keyvault.vault.azure.net/"), new DefaultAzureCredential()); // KeyVault // Package Azure.Extensions.AspNetCore.Configuration.Secrets // Package Azure.Identity

        builder.Services.AddSingleton<Configuration>();
        builder.Services.AddSingleton<DataService>();
        builder.Services.AddSingleton<CosmosDb>();
        builder.Services.AddSingleton<MemoryGrid>();
        builder.Services.AddSingleton<ExcelGrid>();
        builder.Services.AddSingleton<StorageGrid>();
        builder.Services.AddScoped<Response>();

        builder.Services.AddControllers().AddJsonOptions(configure =>
        {
            var options = configure.JsonSerializerOptions;
            UtilServer.JsonConfigure(options);
        });

        var jssonOptions = new JsonSerializerOptions();
        UtilServer.JsonConfigure(jssonOptions);

        builder.Services.AddSingleton(jssonOptions);
    }

    /// <summary>
    /// Process one request.
    /// </summary>
    public static async Task<IActionResult> Run(HttpRequest req, IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetService<ILogger<Function>>()!;
        logger.LogInformation("UrilServer.Run();");
        var jsonOptions = serviceProvider.GetService<JsonSerializerOptions>()!;
        // GET
        if (req.Method == "GET")
        {
            return new OkObjectResult($"App.Server ({UtilServer.Version})");
        }
        // POST
        using var reader = new StreamReader(req.Body);
        var requestBody = await reader.ReadToEndAsync();
        var requestDto = JsonSerializer.Deserialize<RequestDto>(requestBody, jsonOptions)!;
        ResponseDto responseDto;
        try
        {
            responseDto = await ServerApi.Run(requestDto, jsonOptions, serviceProvider);
            if (responseDto.Result is GridDto grid)
            {
                grid.ClearResponse();
            }
            var response = serviceProvider.GetService<Response>()!;
            responseDto.NavigateUrl = response.NavigateUrl;
        }
        catch (Exception exception)
        {
            responseDto = new ResponseDto { ExceptionText = exception.Message };
        }
        responseDto.CommandName = requestDto.CommandName;
        if (responseDto.ExceptionText != null)
        {
            return new BadRequestObjectResult(responseDto);
        }
        return new OkObjectResult(responseDto);
    }

    /// <summary>
    /// Configure json serialization, deserialization.
    /// </summary>
    private static void JsonConfigure(JsonSerializerOptions jsonOptions)
    {
        jsonOptions.WriteIndented = true;
        jsonOptions.PropertyNameCaseInsensitive = true;
        jsonOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        jsonOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        {
            Modifiers = { UtilServer.JsonConfigure }
        };
    }

    /// <summary>
    /// Configure json inheritance for ComponentDto.
    /// </summary>
    private static void JsonConfigure(JsonTypeInfo jsonTypeInfo)
    {
        var typeList = AppServerComponent.ComponentTypeList();
        var list = new List<JsonDerivedType>();
        foreach (var type in typeList)
        {
            var typeName = type.Name.Substring("Component".Length);
            typeName = typeName.Substring(0, typeName.Length - "Dto".Length);
            list.Add(new JsonDerivedType(type, typeName));
        }
        if (jsonTypeInfo.Type == typeof(ComponentDto))
        {
            jsonTypeInfo.PolymorphismOptions = new JsonPolymorphismOptions
            {
                TypeDiscriminatorPropertyName = "type",
            };
            foreach (var item in list)
            {
                jsonTypeInfo.PolymorphismOptions.DerivedTypes.Add(item);
            }
        }
    }

    public static JsonElement? JsonElementFrom(object? value, JsonSerializerOptions jsonOptions)
    {
        return JsonDocument.Parse(JsonSerializer.Serialize(value, jsonOptions)).RootElement;
    }

    public static object? JsonElementTo(JsonElement? value, Type type, JsonSerializerOptions jsonOptions)
    {
        return value?.Deserialize(type, jsonOptions);
    }

    public static T? JsonElementTo<T>(JsonElement? value, JsonSerializerOptions jsonOptions)
    {
        return (T?)JsonElementTo(value, typeof(T), jsonOptions);
    }

    public static string FolderNameAppServer()
    {
        var result = new Uri(new Uri(typeof(UtilServer).Assembly.Location), ".").LocalPath.Replace(@"\", "/");
        if (File.Exists(result + "App.Server.dll"))
        {
            // Running on server
            return result;
        }
        if (File.Exists(result + "App.Server.csproj"))
        {
            // Running locally
            return result;
        }
        throw new Exception("Folder not found!");
    }

    public static void Assert(bool value, string? message = null)
    {
        if (!value)
        {
            throw new Exception(message ?? "Assert!");
        }
    }
}
