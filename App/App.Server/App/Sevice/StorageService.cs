public class StorageService(CommandContextService context, ConfigurationService configuration, StorageClientService storageClient)
{
    public async Task<string> Download(string fileName, bool isOrganisation = true)
    {
        fileName = context.Name(fileName, isOrganisation);
        return await UtilStorage.Download(storageClient.Client, fileName);
    }

    public async Task Upload(string fileName, string? data, bool isOrganisation = true)
    {
        fileName = context.Name(fileName, isOrganisation);
        await UtilStorage.Upload(storageClient.Client, fileName, data);
    }

    /// <summary>
    /// Returns fileNameLocal after download from storage.
    /// </summary>
    public async Task<string> DownloadLocal(string fileNameStorage, bool isOrganisation = true)
    {
        fileNameStorage = context.Name(fileNameStorage, isOrganisation);
        var fileNameLocal = UtilServer.FolderNameTemp() + "App/Data/Storage/" + fileNameStorage;
        await UtilStorage.DownloadLocal(storageClient.Client, fileNameStorage, fileNameLocal);
        return fileNameLocal;
    }

    public async Task<List<UtilStorageEntry>> List(string? folderName = null, bool isRecursive = false, bool isOrganisation = true)
    {
        folderName = context.Name(folderName, isOrganisation);
        var result = await UtilStorage.List(storageClient.Client, folderName, isRecursive);
        var folderNamePrefix = context.Name(null, isOrganisation) + "/";
        foreach (var item in result)
        {
            UtilServer.Assert(item.FolderOrFileName.StartsWith(folderNamePrefix));
            item.FolderOrFileName = item.FolderOrFileName.Substring(folderNamePrefix.Length);
        }
        return result;
    }

    public List<string> DownloadUrl(List<string> fileNameList, bool isOrganisation = true)
    {
        for (int i = 0; i < fileNameList.Count; i++)
        {
            fileNameList[i] = context.Name(fileNameList[i], isOrganisation);
        }
        return UtilStorage.DownloadUrl(storageClient.Client, fileNameList);
    }

    public string DownloadUrl(string fileName, bool isOrganisation = true)
    {
        return DownloadUrl([fileName], isOrganisation).Single();
    }

    public List<string> UploadUrl(List<string> fileNameList, bool isOrganisation = true)
    {
        for (int i = 0; i < fileNameList.Count; i++)
        {
            fileNameList[i] = context.Name(fileNameList[i], isOrganisation);
        }
        return UtilStorage.UploadUrl(storageClient.Client, fileNameList);
    }

    public async Task Rename(string folderOrFileName, string folderOrFileNameOnlyNew, bool isOrganisation = true)
    {
        folderOrFileName = context.Name(folderOrFileName, isOrganisation);
        await UtilStorage.Rename(storageClient.Client, folderOrFileName, folderOrFileNameOnlyNew);
    }

    public async Task Delete(string folderOrFileName, bool isOrganisation = true)
    {
        folderOrFileName = context.Name(folderOrFileName, isOrganisation);
        await UtilStorage.Delete(storageClient.Client, folderOrFileName);
    }

    public async Task<long> Copy(string folderOrFileNameSource, string folderNameDest, bool isOrganisation = true)
    {
        folderOrFileNameSource = context.Name(folderOrFileNameSource, isOrganisation);
        folderNameDest = context.Name(folderNameDest, isOrganisation);
        return await UtilStorage.Copy(storageClient.Client, folderOrFileNameSource, folderNameDest, configuration.ConnectionStringStorage);
    }

    public async Task Create(string folderName, bool isOrganisation = true)
    {
        folderName = context.Name(folderName, isOrganisation);
        await UtilStorage.Create(storageClient.Client, folderName);
    }
}
