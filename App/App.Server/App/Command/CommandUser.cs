public class CommandUserSignUp(CosmosDb cosmosDb, Response response)
{
    public async Task Run(UserDto user)
    {
        user.Name = user.Email;
        await cosmosDb.InsertAsync(user);
        response.NavigateUrl = "signup-email"; // Email has been sent to activate.
    }
}

public class UserDto : DocumentDto
{
    public string? Email { get; set; }

    public string? Password { get; set; }
}