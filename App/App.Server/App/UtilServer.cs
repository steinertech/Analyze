using Azure.Identity; // Used for AddAzureKeyVault
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

internal static class UtilServer
{
    public static string VersionServer => "1.0.15";

    /// <summary>
    /// App start config.
    /// </summary>
    public static void AppConfigure(FunctionsApplicationBuilder builder)
    {
        builder.Configuration.AddUserSecrets(typeof(Function).Assembly); // secrets.json // Package Microsoft.Extensions.Configuration.UserSecrets
        // builder.Configuration.AddAzureKeyVault(new Uri("https://stc001keyvault.vault.azure.net/"), new DefaultAzureCredential()); // KeyVault // Package Azure.Extensions.AspNetCore.Configuration.Secrets // Package Azure.Identity

        builder.Services.AddSingleton<Configuration>(); // Contains state
        builder.Services.AddSingleton<DataService>(); // Contains state
        builder.Services.AddSingleton<CosmosDbContainer>(); // Contains state
        // builder.Services.AddSingleton<CosmosDb>();
        builder.Services.AddTransient<CosmosDb>(); // Wrapper
        builder.Services.AddTransient<CosmosDbDynamic>(); // Wrapper
        builder.Services.AddSingleton<MemoryGrid>(); // Contains state
        builder.Services.AddSingleton<ExcelGrid>();
        builder.Services.AddTransient<ArticleGrid>();
        builder.Services.AddTransient<StorageGrid>(); // Wrapper
        builder.Services.AddScoped<CommandContext>(); // One new instance for every http request
        builder.Services.AddTransient<Storage>(); // Wrapper

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
            return new OkObjectResult($"App.Server ({UtilServer.VersionServer})");
        }
        // POST
        using var reader = new StreamReader(req.Body);
        var requestBody = await reader.ReadToEndAsync();
        var requestDto = JsonSerializer.Deserialize<RequestDto>(requestBody, jsonOptions)!;
        var context = serviceProvider.GetService<CommandContext>()!;
        var configuration = serviceProvider.GetService<Configuration>()!;
        context.Domain = new Uri(req.Headers.Origin!).Host;
        // context.DomainNameServer = req.Host.Host; // Not used
        context.RequestSessionId = req.Cookies["SessionId"];
        if (configuration.IsDevelopment)
        {
            context.RequestSessionId = requestDto.DevelopmentSessionId;
        }
        ResponseDto responseDto;
        var isReload = requestDto.VersionClient != null && requestDto.VersionClient != UtilServer.VersionServer;
        try
        {
            // IsReload
            if (isReload)
            {
                throw new Exception("Reload page.");
            }
            // Run
            responseDto = await ServerApi.Run(requestDto, jsonOptions, serviceProvider);
            if (responseDto.Result is GridDto grid)
            {
                grid.ClearResponse();
            }
            responseDto.NavigateUrl = context.ResponseNavigateUrl;
            responseDto.NotificationList = context.NotificationList;
            // Session
            if (context.ResponseSessionId != null)
            {
                var options = new CookieOptions
                {
                    HttpOnly = true, // JavaScript can not access cookie
                    SameSite = SameSiteMode.Strict, // api.example.com and www.example.com are the same site. Not considered to be a third party cookie which can be blocked.
                    Secure = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                };
                req.HttpContext.Response.Cookies.Append("SessionId", context.ResponseSessionId, options);
                if (configuration.IsDevelopment)
                {
                    responseDto.DevelopmentSessionId = context.ResponseSessionId;
                }
            }
        }
        catch (Exception exception)
        {
            responseDto = new ResponseDto
            {
                ExceptionText = exception.Message,
                NavigateUrl = context.ResponseNavigateUrl, // For example navigate to signin
                IsReload = isReload ? true : null
            };
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
        jsonOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull; // Response value null ends up as unassigned (not null) in JavaScript!
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
