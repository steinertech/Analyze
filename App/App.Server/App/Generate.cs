using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

public static class ServerApi
{
    public static async Task<ResponseDto> Run(RequestDto requestDto, JsonSerializerOptions jsonOptions, IServiceProvider serviceProvider)
    {
        ResponseDto responseDto;
        switch (requestDto.CommandName)
        {
            case nameof(CommandVersion):
                responseDto = new ResponseDto { Result = new CommandVersion().Run() };
                break;
            case nameof(CommandTree):
                responseDto = new ResponseDto { Result = new CommandTree().Run(UtilServer.JsonElementTo<ComponentDto?>(requestDto.ParamList![0], jsonOptions)) };
                break;
            case nameof(CommandDebug):
                responseDto = new ResponseDto { Result = new CommandDebug(serviceProvider.GetService<DataService>()!).Run() };
                break;
            case nameof(CommandStorageDownload):
                responseDto = new ResponseDto { Result = await new CommandStorageDownload(serviceProvider.GetService<DataService>()!).Run(UtilServer.JsonElementTo<string>(requestDto.ParamList![0], jsonOptions)!) };
                break;
            case nameof(CommandStorageUpload):
                responseDto = new ResponseDto { Result = new CommandStorageUpload(serviceProvider.GetService<DataService>()!).Run(UtilServer.JsonElementTo<string>(requestDto.ParamList![0], jsonOptions)!, UtilServer.JsonElementTo<string>(requestDto.ParamList![1], jsonOptions)!) };
                break;
            case nameof(CommandUserSignUp):
                responseDto = new ResponseDto { Result = new CommandUserSignUp(serviceProvider.GetService<CosmosDb>()!, serviceProvider.GetService<Response>()!).Run(UtilServer.JsonElementTo<UserDto>(requestDto.ParamList![0], jsonOptions)!) };
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