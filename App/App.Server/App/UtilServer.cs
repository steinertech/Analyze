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
    public static string VersionServer => "1.0.18";

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
        builder.Services.AddSingleton<TableStorageClient>(); // Contains state
        builder.Services.AddTransient<CosmosDb>(); // Wrapper
        builder.Services.AddTransient<CosmosDbCache>(); // Wrapper
        builder.Services.AddTransient<CosmosDbDynamic>(); // Wrapper
        builder.Services.AddTransient<TableStorage>(); // Wrapper
        builder.Services.AddTransient<TableStorageDynamic>(); // Wrapper
        builder.Services.AddSingleton<GridMemory>(); // Contains state
        builder.Services.AddSingleton<GridExcel>();
        builder.Services.AddTransient<GridArticle>();
        builder.Services.AddSingleton<GridArticle2>(); // Contains state
        builder.Services.AddTransient<GridStorage>(); // Wrapper
        builder.Services.AddScoped<CommandContext>(); // One new instance for every http request
        builder.Services.AddTransient<Storage>(); // Wrapper
        
        // Cache
        builder.Services.AddTransient<Cache>(); // Wrapper
        builder.Services.AddDistributedMemoryCache(); // TODO Redis

        builder.Services.AddControllers().AddJsonOptions(configure =>
        {
            var options = configure.JsonSerializerOptions;
            UtilServer.JsonConfigure(options);
        });

        var jssonOptions = new JsonSerializerOptions();
        UtilServer.JsonConfigure(jssonOptions);

        builder.Services.AddSingleton(jssonOptions);
    }

    private static CookieOptions CookieOptions()
    {
        var result = new CookieOptions
        {
            HttpOnly = true, // JavaScript can not access cookie
            SameSite = SameSiteMode.Strict, // api.example.com and www.example.com are the same site. Not considered to be a third party cookie which can be blocked.
            Secure = true,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        };
        return result;
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
        if (configuration.IsDevelopment == false)
        {
            // Get session from HttpOnly cookie
            context.RequestSessionId = req.Cookies["SessionId"];
            context.CacheId = req.Cookies["CacheId"];
        }
        else
        { 
            // Get session from Dto
            context.RequestSessionId = requestDto.DevelopmentSessionId;
            context.CacheId = requestDto.DevelopmentCacheId;
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
            if (responseDto.Result is GridResponseDto gridResponse)
            {
                gridResponse.ClearResponse();
            }
            responseDto.NavigateUrl = context.ResponseNavigateUrl;
            responseDto.NotificationList = context.NotificationList;
            // Session
            if (context.ResponseSessionId != null)
            {
                if (configuration.IsDevelopment == false)
                {
                    req.HttpContext.Response.Cookies.Append("SessionId", context.ResponseSessionId, CookieOptions());
                }
                else
                {
                    responseDto.DevelopmentSessionId = context.ResponseSessionId;
                }
            }
            // CacheId
            if (context.CacheId != null)
            {
                if (configuration.IsDevelopment == false)
                {
                    req.HttpContext.Response.Cookies.Append("CacheId", context.CacheId, CookieOptions());
                }
                else
                {
                    responseDto.DevelopmentCacheId = context.CacheId;
                }
            }
            responseDto.CacheCount = context.CacheCount;
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

    public static JsonSerializerOptions JsonOptions()
    {
        var result = new JsonSerializerOptions();
        JsonConfigure(result);
        return result;
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

public enum DynamicEnum
{
    None = 0,

    Update = 1,
    
    Insert = 2,

    Delete = 3,
}

/// <summary>
/// Grid data row for processing. It's not a Dto.
/// </summary>
public class Dynamic : Dictionary<string, object?>
{
    public Dynamic()
    {
        
    }

    public Dynamic(IDictionary<string, object?> dictionary) 
        : base(dictionary)
    {
        
    }

    /// <summary>
    /// Returns Dynamic data row with all fields.
    /// </summary>
    public static Dynamic Create(GridConfig config)
    {
        var result = new Dynamic();
        foreach (var column in config.ColumnList)
        {
            result.Add(column.FieldName, null);
        }
        return result;
    }

    /// <summary>
    /// Gets or sets DynamicEnum. Applies if row has been modified.
    /// </summary>
    public DynamicEnum DynamicEnum { get; set; }

    /// <summary>
    /// Gets or sets RowKey. See also property GridConfig.FieldNameRowKey for configuration.
    /// </summary>
    public string? RowKey { get; set; }

    /// <summary>
    /// ((FieldName, IsLeft), GridCellIconDto)
    /// </summary>
    private Dictionary<(string, bool), GridCellIconDto> cellIconList = new();

    /// <summary>
    /// Get cell icon.
    /// </summary>
    public GridCellIconDto? CellIconGet(string fieldName, bool isLeft = false)
    {
        GridCellIconDto? result;
        cellIconList.TryGetValue((fieldName, isLeft), out result);
        return result;
    }

    /// <summary>
    /// Set cell icon.
    /// </summary>
    public void CellIconSet(string fieldName, string? className, string? tooltip, bool isLeft = false)
    {
        if (string.IsNullOrEmpty(className))
        {
            cellIconList.Remove((fieldName, isLeft));
        }
        else
        {
            cellIconList[(fieldName, isLeft)] = new() { ClassName = className, Tooltip = tooltip };
        }
    }
}