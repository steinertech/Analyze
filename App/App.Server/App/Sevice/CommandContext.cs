/// <summary>
/// Gives access to generic RequestDto and ResponseDto.
/// </summary>
public class CommandContext(UtilCosmosDb utilCosmosDb)
{
    private string? domain;

    /// <summary>
    /// Gets Domain. This is the client domain name. For example localhost or example.com
    /// </summary>
    public string Domain
    {
        get
        {
            if (domain == null)
            {
                throw new Exception(); // Make sure calling service has not an old copy of CommandContext. See also builder.Services.AddScoped
            }
            return domain;
        }
        set
        {
            domain = value;
        }
    }

    public string DomainNameServer { get; set; } = default!;

    /// <summary>
    /// Gets OrganisationName. This is the signed in user selected organisation.
    /// </summary>
    private string? organisationName;

    /// <summary>
    /// Return OrganisationName. This is the signed in users selected organisation.
    /// Throws exception, if user not signed in and not selected an organisation.
    /// </summary>
    public async Task<string> OrganisationNameAsync(string? name = null, bool isGlobal = false)
    {
        if (isGlobal)
        {
            return $"{Domain}/Global" + (name == null ? null : $"/{name}");
        }
        if (organisationName != null)
        {
            return $"{Domain}/Organisation/{organisationName}" + (name == null ? null : $"/{name}");
        }
        var partitionKeySessionDto = await OrganisationNameAsync(typeof(SessionDto).Name, isGlobal: true);
        var session = await utilCosmosDb.Select<SessionDto>(partitionKeySessionDto, RequestSessionId).SingleOrDefaultAsync(); // UtilCosmosDb to prevent circular reference
        if (session == null || session.IsSignIn != true)
        {
            ResponseNavigateUrl = "signin";
            throw new Exception("User not signed in!");
        }
        var partitionKeyOrganisationDto = await OrganisationNameAsync(typeof(OrganisationDto).Name, isGlobal: true);
        var organisation = await utilCosmosDb.Select<OrganisationDto>(partitionKeyOrganisationDto, session.OrganisationName).SingleOrDefaultAsync(); // // UtilCosmosDb to prevent circular reference
        if (organisation == null)
        {
            ResponseNavigateUrl = "signin";
            throw new Exception("User not signed in!");
        }
        organisationName = organisation.Name;
        UtilServer.Assert(!string.IsNullOrEmpty(organisationName));
        return await OrganisationNameAsync(name, isGlobal);
    }

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
