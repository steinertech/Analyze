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

    public List<NotificationDto>? NotificationList { get; set; }

    public string? NavigateUrl { get; set; }
}

public enum NotificationEnum
{
    None = 0,

    Info = 1,

    Success = 2,

    Warning = 3,

    Error = 4,
}

public class NotificationDto
{
    public NotificationEnum? NotificationEnum { get; set; }

    public string? Text { get; set; }
}

public class SessionDto : DocumentDto
{
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets or sets Email. This is the signed in user email.
    /// </summary>
    public string? Email { get; set; }

    public bool? IsSignIn { get; set; }

    /// <summary>
    /// Gets or sets OrganisationName. This is the currently selected organisation.
    /// </summary>
    public string? OrganisationName { get; set; }
}

public class OrganisationDto : DocumentDto
{
    /// <summary>
    /// Gets or sets EmailList. This is the list of users which can access this organisation.
    /// </summary>
    public List<string?>? EmailList { get; set; }
}

public class ArticleDto : DocumentDto
{
    public string? Text { get; set; }
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