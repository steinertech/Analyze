public class CommandStorageDownload(DataService dataService)
{
    public Task<string> Run(string fileName)
    {
        return UtilServer.StorageDownload(dataService.ConnectionStringStorage, fileName);
    }
}

