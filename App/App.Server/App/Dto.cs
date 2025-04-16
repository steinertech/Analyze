using System.Text.Json;

public class RequestDto
{
    public string CommandName { get; set; } = default!;

    public List<JsonElement>? ParamList { get; set; }
}

public class ResponseDto
{
    public string? CommandName { get; set; }

    public object? Result { get; set; }

    public string? ExceptionText { get; set; }

    public string? NavigateUrl { get; set; }
}

public class Response
{
    public string? NavigateUrl { get; set; }
}

public class DebugDto
{
    public string? VersionServer { get; set; }
    
    public int? Instance { get; set; }

    public int? Counter { get; set; }

    public List<string>? CounterList { get; set; }
}
