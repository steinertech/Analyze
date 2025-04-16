public class CommandStorageDownload(DataService dataService)
{
    public Task<string> Run(string fileName)
    {
        return UtilServer.StorageDownload(fileName, dataService.ConnectionStringStorage);
    }
}

public class CommandStorageUpload(DataService dataService)
{
    public Task Run(string fileName, string data)
    {
        return UtilServer.StorageUpload(fileName, data, dataService.ConnectionStringStorage);
    }
}

