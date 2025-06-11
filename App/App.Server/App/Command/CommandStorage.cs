public class CommandStorageDownload(Configuration configuration)
{
    public Task<string> Run(string fileName)
    {
        return UtilStorage.Download(fileName, configuration.ConnectionStringStorage);
    }
}

public class CommandStorageUpload(Configuration configuration)
{
    public Task Run(string fileName, string data)
    {
        return UtilStorage.Upload(fileName, data, configuration.ConnectionStringStorage);
    }
}

