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

/// <summary>
/// Gives access to generic RequestDto and ResponseDto.
/// </summary>
public class CommandContext
{
    /// <summary>
    /// Gets Domain. This is the client domain name. For example localhost or example.com
    /// </summary>
    public string Domain { get; internal set; } = default!;
    
    /// <summary>
    /// Gets Tenant. This is the users selected tenant.
    /// </summary>
    public string TenantId { get; internal set; } = default!;

    // public string DomainNameServer { get; set; } = default!;

    /// <summary>
    /// Gets or sets ResponseNavigateUrl. For example "about"
    /// </summary>
    public string? ResponseNavigateUrl { get; set; }

    internal List<NotificationDto>? NotificationList { get; set; }

    /// <summary>
    /// Gets RequestSessionId. This is the SessionId sent by the client.
    /// </summary>
    public string? RequestSessionId { get; internal set; }

    /// <summary>
    /// Gets or sets ResponseSessionId.
    /// </summary>
    public string? ResponseSessionId { get; set; }

    public void NotificationAdd(string text, NotificationEnum notificationEnum = NotificationEnum.None)
    {
        if (NotificationList == null)
        {
            NotificationList = new();
        }
        NotificationList.Add(new NotificationDto { NotificationEnum = notificationEnum, Text = text });
    }
}

public class SessionDto : DocumentDto
{
    public string? SessionId { get; set; }

    public string? Email { get; set; }

    public bool? IsLogin { get; set; }
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