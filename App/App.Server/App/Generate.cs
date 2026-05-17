using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

internal static class ServerApi
{
    public static async Task<ResponseDto> Run(RequestDto requestDto, JsonSerializerOptions jsonOptions, IServiceProvider serviceProvider)
    {
        var responseDto = new ResponseDto();
        switch (requestDto.CommandName)
        {
            // Version
            case nameof(CommandVersion):
                responseDto.Result = new CommandVersion().Run();
                break;
            // Tree
            case nameof(CommandTree):
                responseDto.Result = new CommandTree().Run(UtilServer.JsonElementTo<ComponentDto?>(requestDto.ParamList![0], jsonOptions));
                break;
            // Debug
            case nameof(CommandDebug):
                responseDto.Result = await new CommandDebug(serviceProvider.GetRequiredService<CommandContextService>(), serviceProvider.GetRequiredService<DataService>(), serviceProvider.GetRequiredService<AiService>(), serviceProvider.GetRequiredService<ConfigurationService>(), serviceProvider.GetRequiredService<StorageService>()).Run();
                break;
            // Storage
            case nameof(CommandStorage) + nameof(CommandStorage.Download):
                responseDto.Result = await new CommandStorage(serviceProvider.GetRequiredService<StorageService>(), serviceProvider.GetRequiredService<CommandContextService>()).Download(UtilServer.JsonElementTo<string>(requestDto.ParamList![0], jsonOptions)!);
                break;
            case nameof(CommandStorage) + nameof(CommandStorage.Upload):
                await new CommandStorage(serviceProvider.GetRequiredService<StorageService>(), serviceProvider.GetRequiredService<CommandContextService>()).Upload(UtilServer.JsonElementTo<string>(requestDto.ParamList![0], jsonOptions)!, UtilServer.JsonElementTo<string>(requestDto.ParamList![1], jsonOptions)!);
                break;
            // User
            case nameof(CommandUser) + nameof(CommandUser.SignStatus):
                responseDto.Result = await new CommandUser(serviceProvider.GetRequiredService<CosmosDbService>(), serviceProvider.GetRequiredService<CosmosDbCacheService>(), serviceProvider.GetRequiredService<CommandContextService>(), serviceProvider.GetRequiredService<ConfigurationService>()).SignStatus();
                break;
            case nameof(CommandUser) + nameof(CommandUser.SignIn):
                await new CommandUser(serviceProvider.GetRequiredService<CosmosDbService>(), serviceProvider.GetRequiredService<CosmosDbCacheService>(), serviceProvider.GetRequiredService<CommandContextService>(), serviceProvider.GetRequiredService<ConfigurationService>()).SignIn(UtilServer.JsonElementTo<UserDto>(requestDto.ParamList![0], jsonOptions)!);
                break;
            case nameof(CommandUser) + nameof(CommandUser.SignUp):
                await new CommandUser(serviceProvider.GetRequiredService<CosmosDbService>(), serviceProvider.GetRequiredService<CosmosDbCacheService>(), serviceProvider.GetRequiredService<CommandContextService>(), serviceProvider.GetRequiredService<ConfigurationService>()).SignUp(UtilServer.JsonElementTo<UserDto>(requestDto.ParamList![0], jsonOptions)!);
                break;
            case nameof(CommandUser) + nameof(CommandUser.SignOut):
                await new CommandUser(serviceProvider.GetRequiredService<CosmosDbService>(), serviceProvider.GetRequiredService<CosmosDbCacheService>(), serviceProvider.GetRequiredService<CommandContextService>(), serviceProvider.GetRequiredService<ConfigurationService>()).SignOut();
                break;
            // Grid
            case nameof(CommandGrid) + nameof(CommandGrid.Load2):
                responseDto.Result = await new CommandGrid(serviceProvider.GetRequiredService<GridMemoryService>(), serviceProvider.GetRequiredService<GridExcelService>(), serviceProvider.GetRequiredService<GridStorageService>(), serviceProvider.GetRequiredService<GridArticleService>(), serviceProvider.GetRequiredService<GridArticle2Service>(), serviceProvider.GetRequiredService<GridOrganisationService>(), serviceProvider.GetRequiredService<GridOrganisationEmailService>(), serviceProvider).Load2(UtilServer.JsonElementTo<GridRequest2Dto>(requestDto.ParamList![0], jsonOptions)!);
                break;
            // Assistant
            case nameof(CommandAssistant):
                responseDto.Result = await new CommandAssistant(serviceProvider.GetRequiredService<AiService>()).Run(UtilServer.JsonElementTo<string>(requestDto.ParamList![0], jsonOptions)!);
                break;
            default:
                throw new Exception($"Command not found! ({requestDto.CommandName})");
        }
        return responseDto;
    }
}

public static class AppServerComponent
{
    public static List<Type> ComponentTypeList()
    {
        var result = new List<Type>
            {
                typeof(ComponentDto),
                typeof(ComponentTextDto),
                typeof(ComponentButtonDto),
            };
        return result;
    }
}