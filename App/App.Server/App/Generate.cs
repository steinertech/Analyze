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
                responseDto.Result = new CommandDebug(serviceProvider.GetService<CommandContext>()!, serviceProvider.GetService<DataService>()!).Run();
                break;
            // Storage
            case nameof(CommandStorage) + nameof(CommandStorage.Download):
                responseDto.Result = await new CommandStorage(serviceProvider.GetService<Storage>()!, serviceProvider.GetService<CommandContext>()!).Download(UtilServer.JsonElementTo<string>(requestDto.ParamList![0], jsonOptions)!);
                break;
            case nameof(CommandStorage) + nameof(CommandStorage.Upload):
                await new CommandStorage(serviceProvider.GetService<Storage>()!, serviceProvider.GetService<CommandContext>()!).Upload(UtilServer.JsonElementTo<string>(requestDto.ParamList![0], jsonOptions)!, UtilServer.JsonElementTo<string>(requestDto.ParamList![1], jsonOptions)!);
                break;
            // User
            case nameof(CommandUser) + nameof(CommandUser.SignStatus):
                responseDto.Result = await new CommandUser(serviceProvider.GetService<CosmosDb>()!, serviceProvider.GetService<CosmosDbCache>()!, serviceProvider.GetService<CommandContext>()!, serviceProvider.GetService<Configuration>()!).SignStatus();
                break;
            case nameof(CommandUser) + nameof(CommandUser.SignIn):
                await new CommandUser(serviceProvider.GetService<CosmosDb>()!, serviceProvider.GetService<CosmosDbCache>()!, serviceProvider.GetService<CommandContext>()!, serviceProvider.GetService<Configuration>()!).SignIn(UtilServer.JsonElementTo<UserDto>(requestDto.ParamList![0], jsonOptions)!);
                break;
            case nameof(CommandUser) + nameof(CommandUser.SignUp):
                await new CommandUser(serviceProvider.GetService<CosmosDb>()!, serviceProvider.GetService<CosmosDbCache>()!, serviceProvider.GetService<CommandContext>()!, serviceProvider.GetService<Configuration>()!).SignUp(UtilServer.JsonElementTo<UserDto>(requestDto.ParamList![0], jsonOptions)!);
                break;
            case nameof(CommandUser) + nameof(CommandUser.SignOut):
                await new CommandUser(serviceProvider.GetService<CosmosDb>()!, serviceProvider.GetService<CosmosDbCache>()!, serviceProvider.GetService<CommandContext>()!, serviceProvider.GetService<Configuration>()!).SignOut();
                break;
            // Grid
            case nameof(CommandGrid) + nameof(CommandGrid.Load):
                responseDto.Result = await new CommandGrid(serviceProvider.GetService<GridMemory>()!, serviceProvider.GetService<GridExcel>()!, serviceProvider.GetService<GridStorage>()!, serviceProvider.GetService<GridArticle>()!, serviceProvider.GetService<GridArticle2>()!).Load(UtilServer.JsonElementTo<GridRequestDto>(requestDto.ParamList![0], jsonOptions)!);
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