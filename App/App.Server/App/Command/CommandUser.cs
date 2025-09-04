public class CommandUser(CosmosDb cosmosDb, CosmosDbCache cosmosDbCache, CommandContext context, Configuration configuration)
{
    /// <summary>
    /// Returns UserDto. This is the currently signed in user.
    /// </summary>
    public async Task<UserDto?> SignStatus()
    {
        UserDto? result = null;
        var sessionId = context.RequestSessionId;
        var session = await cosmosDbCache.SelectByNameAsync<SessionDto>(sessionId, isOrganisation: false);
        if (session != null)
        {
            if (session.IsSignIn == true)
            {
                result = new UserDto
                {
                    Email = session.Email,
                };
            }
        }
        return result;
    }

    /// <summary>
    /// User login.
    /// </summary>
    public async Task SignIn(UserDto user)
    {
        context.NotificationAdd($"IsDevelopment={configuration.IsDevelopment}; IsCache={configuration.IsCache}; IsCacheShared={configuration.IsCacheShared};", NotificationEnum.Info);
        user.Name = user.Email;
        user.Email = user.Email;
        user.Password = user.Password;
        // SignIn
        var userDb = await cosmosDb.SelectByNameAsync<UserDto>(user.Name, isOrganisation: false);
        if (userDb == null || userDb.Password != user.Password)
        {
            context.NotificationAdd("User or password wrong!", NotificationEnum.Error);
        }
        else
        {
            var organisation = await cosmosDb.SelectByNameAsync<OrganisationDto>(user.Email, isOrganisation: false);
            if (organisation == null)
            {
                context.NotificationAdd("User not associated with an organisation!", NotificationEnum.Error);
            }
            else
            {
                var sessionId = Guid.NewGuid().ToString();
                SessionDto session = new SessionDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = sessionId,
                    SessionId = sessionId,
                    Email = user.Email,
                    OrganisationName = organisation.Name,
                    IsSignIn = true
                };
                await cosmosDb.InsertAsync(session, isOrganisation: false);
                context.ResponseSessionId = sessionId;
            }
        }
    }

    /// <summary>
    /// Register new user.
    /// </summary>
    public async Task SignUp(UserDto user)
    {
        var userLocal = new UserDto 
        { 
            Id = Guid.NewGuid().ToString(),
            Name = user.Email,
            Email = user.Email,
            Password = user.Password,
        };
        // SignUp
        await cosmosDb.InsertAsync(user, isOrganisation: false);
        // New (small) organisation for user. Additional users can be invited later on.
        var organisation = new OrganisationDto
        { 
            Id = Guid.NewGuid().ToString(),
            Name = user.Email,
            EmailList = [user.Email],
        };
        await cosmosDb.InsertAsync(organisation, isOrganisation: false);
        // Navigate
        context.ResponseNavigateUrl = "signup-email"; // Email has been sent to activate.
    }

    public async Task SignOut()
    {
        var sessionId = context.RequestSessionId;
        var session = await cosmosDbCache.SelectByNameAsync<SessionDto>(sessionId, isOrganisation: false);
        if (session != null)
        {
            session.IsSignIn = false;
            await cosmosDb.UpdateAsync(session, isOrganisation: false);
            // await cosmosDb.DeleteAsync(session, isOrganisation: false);
            await cosmosDbCache.RemoveByNameAsync<SessionDto>(sessionId, isOrganisation: false);
        }
    }
}

public class UserDto : DocumentDto
{
    public string? Email { get; set; }

    public string? Password { get; set; }
}