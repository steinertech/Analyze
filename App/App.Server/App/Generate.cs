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
                responseDto.Result = await new CommandUser(serviceProvider.GetService<CosmosDb>()!, serviceProvider.GetService<CommandContext>()!).SignStatus();
                break;
            case nameof(CommandUser) + nameof(CommandUser.SignIn):
                await new CommandUser(serviceProvider.GetService<CosmosDb>()!, serviceProvider.GetService<CommandContext>()!).SignIn(UtilServer.JsonElementTo<UserDto>(requestDto.ParamList![0], jsonOptions)!);
                break;
            case nameof(CommandUser) + nameof(CommandUser.SignUp):
                await new CommandUser(serviceProvider.GetService<CosmosDb>()!, serviceProvider.GetService<CommandContext>()!).SignUp(UtilServer.JsonElementTo<UserDto>(requestDto.ParamList![0], jsonOptions)!);
                break;
            case nameof(CommandUser) + nameof(CommandUser.SignOut):
                await new CommandUser(serviceProvider.GetService<CosmosDb>()!, serviceProvider.GetService<CommandContext>()!).SignOut();
                break;
            // Grid
            case nameof(CommandGrid) + nameof(CommandGrid.Load):
                responseDto.Result = await new CommandGrid(serviceProvider.GetService<MemoryGrid>()!, serviceProvider.GetService<ExcelGrid>()!, serviceProvider.GetService<StorageGrid>()!, serviceProvider.GetService<ArticleGrid>()!, serviceProvider.GetService<GridArticle>()!).Load(UtilServer.JsonElementTo<GridDto>(requestDto.ParamList![0], jsonOptions)!, UtilServer.JsonElementTo<GridCellDto>(requestDto.ParamList![1], jsonOptions), UtilServer.JsonElementTo<GridControlDto>(requestDto.ParamList![2], jsonOptions), UtilServer.JsonElementTo<GridDto>(requestDto.ParamList![3], jsonOptions));
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