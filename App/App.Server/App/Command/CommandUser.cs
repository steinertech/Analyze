public class CommandUserSignUp(CosmosDb cosmosDb, CommandContext context)
{
    public async Task Run(UserDto user)
    {
        user.Name = $"({context.DomainNameClient}; {user.Email})";
        user.DomainNameClient = context.DomainNameClient;
        user.Email = user.Email;
        await cosmosDb.InsertAsync(user);
        context.ResponseNavigateUrl = "signup-email"; // Email has been sent to activate.
    }
}

public class UserDto : DocumentDto
{
    public string? DomainNameClient { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }
}