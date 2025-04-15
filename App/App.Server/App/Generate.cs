public static class AppServerCommand
{
    public static ResponseDto Run(RequestDto requestDto)
    {
        ResponseDto responseDto;
        switch (requestDto.CommandName)
        {
            case nameof(CommandVersion):
                responseDto = new ResponseVersionDto { Result = new CommandVersion().Run() };
                break;
            default:
                throw new Exception($"Command not found! ({requestDto.CommandName})");
        }
        return responseDto;
    }
}

public class ResponseVersionDto : ResponseDto
{
    public string? Result { get; set; }
}

public static class AppServerComponent
{
    public static List<Type> ComponentTypeList()
    {
        var result = new List<Type>
            {
                typeof(ComponentDto),
                typeof(ComponentLabelDto),
                typeof(ComponentButtonDto),
                typeof(ComponentGridDto),
            };
        return result;
    }
}