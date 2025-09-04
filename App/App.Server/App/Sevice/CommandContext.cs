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
    public async Task UserAuthenticateAsync()
    {
        if (organisationName != null)
        {
            return;
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
        organisationName = organisation.Name;
        UtilServer.Assert(!string.IsNullOrEmpty(organisationName));
    }

    /// <summary>
    /// Gets OrganisationName. This is the signed in user selected organisation.
    /// </summary>
    private string? organisationName;

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
        if (organisationName != null)
        {
            return $"Domain{separator}{Domain}{separator}Organisation{separator}{organisationName}" + (name == null ? null : $"{separator}{name}");
        }
        throw new Exception("Request not authenticated!"); // Call method CommandContext.UserSignInOrganisation(); to make sure user is signed in and has an organisation selected.
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
}
