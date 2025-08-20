public class CommandStorage(Storage storage, CommandContext context)
{
    public async Task<string> Download(string fileName)
    {
        await context.UserSignInOrganisation();
        return await storage.Download(fileName);
    }

    public async Task Upload(string fileName, string data)
    {
        await context.UserSignInOrganisation();
        await storage.Upload(fileName, data);
    }
}

