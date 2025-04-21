using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

public static class ServerApi
{
    public static async Task<ResponseDto> Run(RequestDto requestDto, JsonSerializerOptions jsonOptions, IServiceProvider serviceProvider)
    {
        var responseDto = new ResponseDto();
        switch (requestDto.CommandName)
        {
            case nameof(CommandVersion):
                responseDto.Result = new CommandVersion().Run();
                break;
            case nameof(CommandTree):
                responseDto.Result = new CommandTree().Run(UtilServer.JsonElementTo<ComponentDto?>(requestDto.ParamList![0], jsonOptions));
                break;
            case nameof(CommandDebug):
                responseDto.Result = new CommandDebug(serviceProvider.GetService<DataService>()!).Run();
                break;
            case nameof(CommandStorageDownload):
                responseDto.Result = await new CommandStorageDownload(serviceProvider.GetService<DataService>()!).Run(UtilServer.JsonElementTo<string>(requestDto.ParamList![0], jsonOptions)!);
                break;
            case nameof(CommandStorageUpload):
                await new CommandStorageUpload(serviceProvider.GetService<DataService>()!).Run(UtilServer.JsonElementTo<string>(requestDto.ParamList![0], jsonOptions)!, UtilServer.JsonElementTo<string>(requestDto.ParamList![1], jsonOptions)!);
                break;
            case nameof(CommandUserSignUp):
                await new CommandUserSignUp(serviceProvider.GetService<CosmosDb>()!, serviceProvider.GetService<Response>()!).Run(UtilServer.JsonElementTo<UserDto>(requestDto.ParamList![0], jsonOptions)!);
                break;
            case nameof(CommandGrid) + nameof(CommandGrid.Select):
                responseDto.Result = new CommandGrid(serviceProvider.GetService<MemoryDb>()!).Select(UtilServer.JsonElementTo<string>(requestDto.ParamList![0], jsonOptions)!);
                break;
            case nameof(CommandGrid) + nameof(CommandGrid.SelectConfig):
                responseDto.Result = new CommandGrid(serviceProvider.GetService<MemoryDb>()!).SelectConfig(UtilServer.JsonElementTo<string>(requestDto.ParamList![0], jsonOptions)!);
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