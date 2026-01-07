using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Gives access to generic RequestDto and ResponseDto.
/// </summary>
public class CommandContext(IServiceProvider serviceProvider)
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
                // Make sure calling service has not an old copy of CommandContext. See also builder.Services.AddScoped.
                // For example AddSingleton can not hold a AddTransient service
                throw new Exception(); 
            }
            return domain;
        }
        internal set
        {
            domain = value;
        }
    }

    // public string DomainNameServer { get; set; } = default!;

    /// <summary>
    /// For this request check user is signed in and has an organisation selected. Throws exception if not.
    /// </summary>
    public async Task<CommandContextAuthResult> UserAuthAsync()
    {
        if (this.organisation != null)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(email);
            return new() { Email = email, Organisation = this.organisation };
        }
        var cosmosDbCache = serviceProvider.GetService<CosmosDbCache>()!; // Prevent circular reference.
        var session = RequestSessionId == null ? null : await cosmosDbCache.SelectByNameAsync<SessionDto>(RequestSessionId, isOrganisation: false);
        if (session == null || session.IsSignIn != true)
        {
            // No session or session expired
            ResponseNavigateUrl = "signin";
            throw new Exception("User not signed in!");
        }
        var partitionKeyOrganisationDto = Name(typeof(OrganisationDto).Name, isOrganisation: false);
        var organisation = await cosmosDbCache.SelectByNameAsync<OrganisationDto>(session.OrganisationName, isOrganisation: false);
        if (organisation == null)
        {
            // No organisation selected
            ResponseNavigateUrl = "signin";
            throw new Exception("User not signed in!");
        }
        this.organisation = organisation.Name;
        email = session.Email;
        ArgumentNullException.ThrowIfNullOrEmpty(email);
        ArgumentNullException.ThrowIfNullOrEmpty(this.organisation);

        return new() { Email = email, Organisation = this.organisation };
    }

    public async Task OrganisationSwitch(string organisationName)
    {
        var userAuth = await UserAuthAsync();
        var cosmosDb = serviceProvider.GetService<CosmosDb>()!;
        var organisation = await cosmosDb.SelectByNameAsync<OrganisationDto>(organisationName, isOrganisation: false);
        if (organisation?.EmailList?.Contains(userAuth.Email) == true)
        {
            var sessionId = Guid.NewGuid().ToString();
            SessionDto session = new SessionDto
            {
                Id = Guid.NewGuid().ToString(),
                Name = sessionId,
                SessionId = sessionId,
                Email = email,
                OrganisationName = organisation.Name,
                OrganisationText = organisation.Text,
                IsSignIn = true
            };
            await cosmosDb.InsertAsync(session, isOrganisation: false);
            NotificationAdd("Switching Organisation. Please wait ...", NotificationEnum.Info);
            ResponseSessionId = sessionId;
            ResponseIsReload = true;
        }
    }

    /// <summary>
    /// Gets or sets Organisation. This is the signed in user selected organisation.
    /// </summary>
    private string? organisation;

    /// <summary>
    /// Gets or sets email. This is the signed in user.
    /// </summary>
    private string? email;

    /// <summary>
    /// Returns name in global scope or in organisation scope. 
    /// If organisation scope, user has to be signed in and has to have selected an organisation. Otherwise method throws exception.
    /// </summary>
    /// <param name="name">Name in global or in organisation scope.</param>
    /// <param name="isOrganisation">Organisation or global scope.</param>
    internal string Name(string? name = null, bool isOrganisation = true, string separator = "/")
    {
        if (isOrganisation == false)
        {
            return $"Domain{separator}{Domain}{separator}Global" + (name == null ? null : $"{separator}{name}");
        }
        if (organisation != null)
        {
            return $"Domain{separator}{Domain}{separator}Organisation{separator}{organisation}" + (name == null ? null : $"{separator}{name}");
        }
        throw new Exception("Request not authenticated!"); // Call method CommandContext.UserAuthAsync(); to make sure user is signed in and has an organisation selected.
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
    public string? ResponseSessionId { get; internal set; }

    public bool? ResponseIsReload { get; internal set; }

    public void NotificationAdd(string text, NotificationEnum notificationEnum = NotificationEnum.None)
    {
        if (NotificationList == null)
        {
            NotificationList = new();
        }
        NotificationList.Add(new NotificationDto { NotificationEnum = notificationEnum, Text = text });
    }

    /// <summary>
    /// Gets or sets CacheId. Used for not shared cache.
    /// </summary>
    public string? CacheId { get; internal set; }

    /// <summary>
    /// Gets or sets CacheCount. Number of times cached data was used for this request.
    /// </summary>
    public int? CacheCount { get; internal set; }
}

public class CommandContextAuthResult
{
    public string Email { get; set; } = default!;

    public string Organisation { get; set; } = default!;
}