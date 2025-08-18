public class CommandUser(CosmosDb2 cosmosDb, CommandContext context)
{
    /// <summary>
    /// Returns UserDto. This is the currently signed in user.
    /// </summary>
    public async Task<UserDto?> SignStatus()
    {
        UserDto? result = null;
        var sessionId = context.RequestSessionId;
        if (sessionId != null)
        {
            var session = await (await cosmosDb.SelectAsync<SessionDto>(sessionId, isGlobal: true)).SingleOrDefaultAsync();
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
        }
        return result;
    }

    /// <summary>
    /// User login.
    /// </summary>
    public async Task SignIn(UserDto user)
    {
        user.Name = user.Email;
        user.Email = user.Email;
        user.Password = user.Password;
        // SignIn
        var userDb = await (await cosmosDb.SelectAsync<UserDto>(user.Name, isGlobal: true)).SingleOrDefaultAsync();
        if (userDb == null || userDb.Password != user.Password)
        {
            context.NotificationAdd("User or password wrong!", NotificationEnum.Error);
        }
        else
        {
            var organisation = await (await cosmosDb.SelectAsync<OrganisationDto>(user.Email, isGlobal: true)).SingleOrDefaultAsync();
            if (organisation == null)
            {
                context.NotificationAdd("User not associated with an organisation!", NotificationEnum.Error);
            }
            else
            {
                var sessionId = Guid.NewGuid().ToString();
                SessionDto session = new SessionDto
                {
                    Name = sessionId,
                    SessionId = sessionId,
                    Email = user.Email,
                    OrganisationName = organisation.Name,
                    IsSignIn = true
                };
                await cosmosDb.InsertAsync(session, isGlobal: true);
                context.ResponseSessionId = sessionId;
            }
        }
    }

    /// <summary>
    /// Register new user.
    /// </summary>
    public async Task SignUp(UserDto user)
    {
        user.Name = user.Email;
        user.Email = user.Email;
        user.Password = user.Password;
        // SignUp
        await cosmosDb.InsertAsync(user, isGlobal: true);
        // New (small) organisation for user. Additional users can be invited later on.
        var organisation = new OrganisationDto();
        organisation.Name = user.Email;
        organisation.EmailList = [user.Email];
        await cosmosDb.InsertAsync(organisation, isGlobal: true);
        // Navigate
        context.ResponseNavigateUrl = "signup-email"; // Email has been sent to activate.
    }

    public async Task SignOut()
    {
        var sessionId = context.RequestSessionId;
        var session = await (await cosmosDb.SelectAsync<SessionDto>(sessionId, isGlobal: true)).SingleOrDefaultAsync();
        if (session != null)
        {
            session.IsSignIn = false;
            await cosmosDb.UpdateAsync(session, isGlobal: true);
            // await cosmosDb.DeleteAsync(session, isGlobal: true);
        }
    }
}

public class UserDto : DocumentDto
{
    public string? Email { get; set; }

    public string? Password { get; set; }
}