﻿using Microsoft.Extensions.DependencyInjection;
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
                responseDto.Result = await new CommandStorageDownload(serviceProvider.GetService<Configuration>()!).Run(UtilServer.JsonElementTo<string>(requestDto.ParamList![0], jsonOptions)!);
                break;
            case nameof(CommandStorageUpload):
                await new CommandStorageUpload(serviceProvider.GetService<Configuration>()!).Run(UtilServer.JsonElementTo<string>(requestDto.ParamList![0], jsonOptions)!, UtilServer.JsonElementTo<string>(requestDto.ParamList![1], jsonOptions)!);
                break;
            case nameof(CommandUserSignUp):
                await new CommandUserSignUp(serviceProvider.GetService<CosmosDb>()!, serviceProvider.GetService<Response>()!).Run(UtilServer.JsonElementTo<UserDto>(requestDto.ParamList![0], jsonOptions)!);
                break;
            case nameof(CommandGrid) + nameof(CommandGrid.Load):
                responseDto.Result = await new CommandGrid(serviceProvider.GetService<MemoryGrid>()!, serviceProvider.GetService<ExcelGrid>()!, serviceProvider.GetService<StorageGrid>()!).Load(UtilServer.JsonElementTo<GridDto>(requestDto.ParamList![0], jsonOptions)!, UtilServer.JsonElementTo<GridCellDto>(requestDto.ParamList![1], jsonOptions), UtilServer.JsonElementTo<GridControlDto>(requestDto.ParamList![2], jsonOptions), UtilServer.JsonElementTo<GridDto>(requestDto.ParamList![3], jsonOptions));
                break;
            case nameof(CommandGrid) + nameof(CommandGrid.Save):
                responseDto.Result = await new CommandGrid(serviceProvider.GetService<MemoryGrid>()!, serviceProvider.GetService<ExcelGrid>()!, serviceProvider.GetService<StorageGrid>()!).Save(UtilServer.JsonElementTo<GridDto>(requestDto.ParamList![0], jsonOptions)!, UtilServer.JsonElementTo<GridCellDto>(requestDto.ParamList![1], jsonOptions), UtilServer.JsonElementTo<GridControlDto>(requestDto.ParamList![2], jsonOptions), UtilServer.JsonElementTo<GridDto>(requestDto.ParamList![3], jsonOptions));
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