public class Storage(CommandContext context, Configuration configuration)
{
    public async Task<string> Download(string fileName, bool isOrganisation = true)
    {
        fileName = context.Name(fileName, isOrganisation);
        return await UtilStorage.Download(configuration.ConnectionStringStorage, fileName);
    }

    public async Task Upload(string fileName, string data, bool isOrganisation = true)
    {
        fileName = context.Name(fileName, isOrganisation);
        await UtilStorage.Upload(configuration.ConnectionStringStorage, fileName, data);
    }
}
