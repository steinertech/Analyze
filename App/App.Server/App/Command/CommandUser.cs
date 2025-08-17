public class CommandUser(CosmosDb cosmosDb, CommandContext context)
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
            var session = await cosmosDb.SelectSingleOrDefaultAsync<SessionDto>(context, name: sessionId);
            if (session != null)
            {
                if (session.IsLogin == true)
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
        var userDb = await cosmosDb.SelectSingleOrDefaultAsync<UserDto>(context, name: user.Name);
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
                Email = user.Email,
                IsLogin = true
            };
            await cosmosDb.InsertAsync(context, session);
            context.ResponseSessionId = sessionId;
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
        await cosmosDb.InsertAsync(context, user);
        context.ResponseNavigateUrl = "signup-email"; // Email has been sent to activate.
    }

    public async Task SignOut()
    {
        var sessionId = context.RequestSessionId;
        var session = await cosmosDb.SelectSingleOrDefaultAsync<SessionDto>(context, name: sessionId);
        if (session != null)
        {
            session.IsLogin = false;
            await cosmosDb.UpdateAsync(context, session);
            // await cosmosDb.DeleteAsync(session);
        }
    }
}

public class UserDto : DocumentDto
{
    public string? Email { get; set; }

    public string? Password { get; set; }
}