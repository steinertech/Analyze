public class CommandStorage(Storage storage)
{
    public async Task<string> Download(string fileName)
    {
        return await storage.Download(fileName);
    }

    public async Task Upload(string fileName, string data)
    {
        await storage.Upload(fileName, data);
    }
}

