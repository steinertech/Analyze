public class CommandStorageDownload(Configuration configuration)
{
    public Task<string> Run(string fileName)
    {
        return UtilStorage.Download(configuration.ConnectionStringStorage, fileName);
    }
}

public class CommandStorageUpload(Configuration configuration)
{
    public Task Run(string fileName, string data)
    {
        return UtilStorage.Upload(configuration.ConnectionStringStorage, fileName, data);
    }
}

