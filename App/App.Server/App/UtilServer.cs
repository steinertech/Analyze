using System.Text.Json.Serialization.Metadata;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Builder;

public static class UtilServer
{
    public static string Version
    {
        get
        {
            return "1.0.1";
        }
    }

    public static async Task<IActionResult> Run(HttpRequest req, IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetService<ILogger<Function>>()!;
        logger.LogInformation("UrilServer.Run();");
        // GET
        if (req.Method == "GET")
        {
            return new OkObjectResult($"App.Server ({UtilServer.Version})");
        }
        // POST
        using var reader = new StreamReader(req.Body);
        var requestBody = await reader.ReadToEndAsync();
        var options = new JsonSerializerOptions();
        UtilServer.Configure(options);
        var requestDto = JsonSerializer.Deserialize<RequestDto>(requestBody, options)!;
        ResponseDto responseDto;
        try
        {
            responseDto = AppServerCommand.Run(requestDto);
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

    public static void Configure(FunctionsApplicationBuilder builder)
    {
        builder.Services.AddSingleton<DataService>();

        builder.Services.AddControllers().AddJsonOptions(configure =>
        {
            var options = configure.JsonSerializerOptions;
            UtilServer.Configure(options);
        });

        var options = new JsonSerializerOptions();
        UtilServer.Configure(options);

        builder.Services.AddSingleton(options);
    }

    /// <summary>
    /// Configure json.
    /// </summary>
    private static void Configure(JsonSerializerOptions options)
    {
        options.WriteIndented = true;
        options.PropertyNameCaseInsensitive = true;
        options.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        {
            Modifiers = { UtilServer.Configure }
        };
    }

    /// <summary>
    /// Configure json inheritance for ComponentDto.
    /// </summary>
   private static void Configure(JsonTypeInfo jsonTypeInfo)
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
}
