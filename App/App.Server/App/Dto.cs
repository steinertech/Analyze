using System.Text.Json;

/// <summary>
/// Generic request.
/// </summary>
public class RequestDto
{
    public string CommandName { get; set; } = default!;

    public List<JsonElement>? ParamList { get; set; }
}

/// <summary>
/// Generic response.
/// </summary>
public class ResponseDto
{
    public string? CommandName { get; set; }

    public object? Result { get; set; }

    public string? ExceptionText { get; set; }

    public string? NavigateUrl { get; set; }
}

public class CommandContext
{
    public string DomainNameClient { get; set; } = default!;

    public string DomainNameServer { get; set; } = default!;

    public string ResponseNavigateUrl { get; set; } = default!;
}

public class DebugDto
{
    public string? VersionServer { get; set; }
    
    public int? Instance { get; set; }

    public int? Counter { get; set; }

    public List<string>? CounterList { get; set; }
}

public class HeaderLookupDataRowDto
{
    public string? Text { get; set; }
}

public class ColumnLookupDataRowDto
{
    public string? FieldName { get; set; }
}