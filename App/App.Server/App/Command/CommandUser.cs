public class CommandUser(CosmosDb cosmosDb, CommandContext context)
{
    /// <summary>
    /// Returns UserDto. This is the currently signed in user.
    /// </summary>
    public async Task<UserDto?> SignStatus()
    {
        UserDto? result = null;
        var sessionId = context.RequestSessionId;
        var session = await cosmosDb.Select<SessionDto>(name: sessionId).SingleOrDefaultAsync();
        if (session != null)
        {
            if (session.DomainNameClient == context.DomainNameClient)
            {
                if (session.IsLogin == true)
                {
                    result = new UserDto
                    {
                        DomainNameClient = context.DomainNameClient,
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
        user.Name = $"({context.DomainNameClient}; {user.Email})";
        user.DomainNameClient = context.DomainNameClient;
        user.Email = user.Email;
        user.Password = user.Password;
        // SignIn
        var userDb = await cosmosDb.Select<UserDto>(name: user.Name).SingleOrDefaultAsync();
        if (userDb == null || userDb.Password != user.Password)
        {
            context.NotificationAdd("User or password wrong!", NotificationEnum.Error);
        }
        else
        {
            var sessionId = Guid.NewGuid().ToString();
            SessionDto session = new SessionDto
            {
                Name = sessionId,
                SessionId = sessionId,
                DomainNameClient = context.DomainNameClient,
                Email = user.Email,
                IsLogin = true
            };
            await cosmosDb.InsertAsync(session);
            context.ResponseSessionId = sessionId;
        }
    }

    /// <summary>
    /// Register new user.
    /// </summary>
    public async Task SignUp(UserDto user)
    {
        user.Name = $"({context.DomainNameClient}; {user.Email})";
        user.DomainNameClient = context.DomainNameClient;
        user.Email = user.Email;
        user.Password = user.Password;
        // SignUp
        await cosmosDb.InsertAsync(user);
        context.ResponseNavigateUrl = "signup-email"; // Email has been sent to activate.
    }

    public async Task SignOut()
    {
        var sessionId = context.RequestSessionId;
        var session = await cosmosDb.Select<SessionDto>(name: sessionId).SingleOrDefaultAsync();
        if (session != null)
        {
            session.IsLogin = false;
            await cosmosDb.UpdateAsync(session);
        }
    }
}

public class UserDto : DocumentDto
{
    public string? DomainNameClient { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }
}