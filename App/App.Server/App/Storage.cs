public class Storage(CommandContext context, Configuration configuration)
{
    public async Task<string> Download(string fileName, bool isGlobal = false)
    {
        fileName = await context.OrganisationNameAsync(fileName, isGlobal);
        return await UtilStorage.Download(configuration.ConnectionStringStorage, fileName);
    }

    public async Task Upload(string fileName, string data, bool isGlobal = false)
    {
        fileName = await context.OrganisationNameAsync(fileName, isGlobal);
        await UtilStorage.Upload(configuration.ConnectionStringStorage, fileName, data);
    }
}
