using Azure.Storage.Blobs; // Used for StartCopyFromUriAsync
using Azure.Storage.Files.DataLake;
using Azure.Storage.Files.DataLake.Models;
using System.Text;

public static class UtilStorage
{
    private static string PathItemToFolderOrFileName(PathItem value)
    {
        UtilServer.Assert(value.Name.StartsWith(StorageClientService.ContainerFolderName));
        var result = value.Name.Substring(StorageClientService.ContainerFolderName.Length);
        return result;
    }

    /// <summary>
    /// Ensure folder or file name starts with /app/data/ path also if it uses relative path characters.
    /// </summary>
    private static string Sanatize(DataLakeDirectoryClient client, string? folderOrFileName)
    {
        folderOrFileName = folderOrFileName?.TrimEnd('/');
        var result = client.GetSubDirectoryClient(string.IsNullOrEmpty(folderOrFileName) ? "." : folderOrFileName).Uri.LocalPath;
        if (result == StorageClientService.Prefix + folderOrFileName)
        {
            result = result.Substring(StorageClientService.Prefix.Length);
            return result;
        }
        throw new Exception($"Folder or file name invalid! ({folderOrFileName})");
    }

    private static bool IsFolder(string folderOrFileName)
    {
        return folderOrFileName.Length == 0 || folderOrFileName.EndsWith("/");
    }

    private static bool IsFile(string folderOrFileName)
    {
        return folderOrFileName.Length > 0 && !IsFolder(folderOrFileName);
    }

    private static string FolderName(string folderOrFileName)
    {
        var result = (Path.GetDirectoryName(folderOrFileName) ?? "").Replace("\"", "/");
        return result;
    }

    public static string FolderOrFileNameOnly(string folderOrFileName)
    {
        var result = Path.GetFileName(folderOrFileName);
        return result;
    }

    public static async Task<long> Copy(DataLakeDirectoryClient client, string folderOrFileNameSource, string folderNameDest, string connectionString)
    {
        UtilServer.Assert(IsFolder(folderNameDest));
        if (IsFile(folderOrFileNameSource))
        {
            var fileNameOnlySource = FolderOrFileNameOnly(folderOrFileNameSource);
            var folderNameSource = FolderName(folderOrFileNameSource);
            await Create(client, folderNameDest);
            var folderSource = client.GetSubDirectoryClient(folderNameSource);
            var fileSource = folderSource.GetFileClient(fileNameOnlySource);
            var fileSourceUri = fileSource.GenerateSasUri(Azure.Storage.Sas.DataLakeSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(3));
            // var folderDest = client.GetSubDirectoryClient(folderNameDest);
            // var fileDest = folderDest.GetFileClient(folderNameDest); // DataLakeFileClient does not support server‑side copy
            var fileDest2 = new BlobClient(connectionString, StorageClientService.ContainerName, StorageClientService.ContainerFolderName + folderNameDest + fileNameOnlySource);
            var result = await fileDest2.StartCopyFromUriAsync(fileSourceUri);
            await result.UpdateStatusAsync();
            return result.Value;
        }
        throw new Exception();
    }

    public static async Task Create(DataLakeDirectoryClient client, string folderName)
    {
        folderName = Sanatize(client, folderName);

        await client.GetFileClient(folderName).CreateAsync(PathResourceType.Directory);
    }

    public static async Task Delete(DataLakeDirectoryClient client, string folderOrFileName)
    {
        folderOrFileName = Sanatize(client, folderOrFileName);

        UtilServer.Assert(folderOrFileName.Length > 0); // Do not delete root

        await client.GetFileClient(folderOrFileName).DeleteAsync();
    }

    public static async Task Rename(DataLakeDirectoryClient client, string folderOrFileName, string folderOrFileNameOnlyNew)
    {
        var folderName = FolderName(folderOrFileName);
        folderOrFileNameOnlyNew = FolderOrFileNameOnly(folderOrFileNameOnlyNew);
        var folderOrFileNameNew2 = folderName + "/" + folderOrFileNameOnlyNew; // Rename only. No move to other folder.

        folderOrFileName = Sanatize(client, folderOrFileName);
        folderOrFileNameNew2 = Sanatize(client, folderOrFileNameNew2);

        await client.GetFileClient(folderOrFileName).RenameAsync(StorageClientService.ContainerFolderName + folderOrFileNameNew2);
    }

    public static async Task<List<UtilStorageEntry>> List(DataLakeDirectoryClient client, string? folderName = null, bool isRecursive = false)
    {
        folderName = Sanatize(client, folderName);

        var result = new List<UtilStorageEntry>();
        await foreach (var pathItem in client.GetSubDirectoryClient(folderName).GetPathsAsync(new DataLakeGetPathsOptions { Recursive = true }))
        {
            var folderOrFileName = PathItemToFolderOrFileName(pathItem) + (pathItem.IsDirectory == true ? "/" : null);
            var isFolder = pathItem.IsDirectory ?? false;
            var text = folderOrFileName.TrimEnd('/').Substring(folderOrFileName.TrimEnd('/').LastIndexOf("/") + 1);
            var resultItem = new UtilStorageEntry 
            { 
                FolderOrFileName = folderOrFileName, 
                IsFolder = isFolder, 
                Text = text, 
                DateModified = pathItem.LastModified.UtcDateTime, 
                Size = !isFolder ? 
                pathItem.ContentLength : null 
            };
            result.Add(resultItem);
        }
        return result;
    }

    public static async Task<string> Download(DataLakeDirectoryClient client, string fileName)
    {
        fileName = Sanatize(client, fileName);

        string result;
        var content = await client.GetFileClient(fileName).ReadContentAsync();
        var fileNameExtension = Path.GetExtension(fileName).ToLower();
        switch (fileNameExtension)
        {
            case ".txt":
                result = Encoding.UTF8.GetString(content.Value.Content);
                break;
            case ".png":
                result = $"data:text/plain;base64,{Convert.ToBase64String(content.Value.Content)}";
                break;
            default:
                result = Convert.ToBase64String(content.Value.Content);
                break;
        }
        return result;
    }

    public static async Task DownloadLocal(DataLakeDirectoryClient client, string fileNameStorage, string fileNameLocal)
    {
        fileNameStorage = Sanatize(client, fileNameStorage);

        using var file = DownloadStream(client, fileNameStorage);
        using var streamStorage = file.Content;

        var folderNameLocal = Path.GetDirectoryName(fileNameLocal)!;
        if (!Path.Exists(folderNameLocal))
        {
            Directory.CreateDirectory(folderNameLocal);
        }
        using var streamLocal = new FileStream(fileNameLocal, FileMode.Create);
        await streamStorage.CopyToAsync(streamLocal);
        streamLocal.Close();
        streamStorage.Close();
    }

    public static DataLakeFileReadStreamingResult DownloadStream(DataLakeDirectoryClient client, string fileName)
    {
        fileName = Sanatize(client, fileName);

        var result = client.GetFileClient(fileName).ReadStreaming().Value;
        return result; // result.Content is the Stream. Beware of DataLakeFileReadStreamingResult.IDisposable
    }

    public static async Task Upload(DataLakeDirectoryClient client, string fileName, string? data)
    {
        fileName = Sanatize(client, fileName);

        byte[] result;
        var fileNameExtension = Path.GetExtension(fileName).ToLower();
        switch (fileNameExtension)
        {
            case ".txt":
                result = Encoding.UTF8.GetBytes(data ?? "");
                break;
            default:
                result = Convert.FromBase64String(data ?? "");
                break;
        }
        using var stream = new MemoryStream(result);
        var fileClient = client.GetFileClient(fileName);
        if (stream.Length == 0)
        {
            await fileClient.CreateAsync(); // Prevent exception The value for one of the HTTP headers is not in the correct format.
        }
        else
        {
            await fileClient.UploadAsync(stream, overwrite: true);
        }
        stream.Close();
    }

    /// <summary>
    /// Returns list of SasUri to upload to.
    /// </summary>
    public static List<string> UploadUrl(DataLakeDirectoryClient client, List<string> fileNameList)
    {
        var result = new List<string>();
        foreach (var fileName in fileNameList)
        {
            var file = client.GetFileClient(fileName);
            var fileUrl = file.GenerateSasUri(Azure.Storage.Sas.DataLakeSasPermissions.Write, DateTimeOffset.UtcNow.AddMinutes(3)).ToString();
            result.Add(fileUrl);
        }
        return result;
    }

    /// <summary>
    /// Returns list of SasUri to download from.
    /// </summary>
    public static List<string> DownloadUrl(DataLakeDirectoryClient client, List<string> fileNameList)
    {
        var result = new List<string>();
        foreach (var fileName in fileNameList)
        {
            var file = client.GetFileClient(fileName);
            var fileUrl = file.GenerateSasUri(Azure.Storage.Sas.DataLakeSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(3)).ToString();
            result.Add(fileUrl);
        }
        return result;
    }
}

public class UtilStorageEntry
{
    /// <summary>
    /// Gets or sets FolderOrFileName. This includes full path.
    /// </summary>
    public string FolderOrFileName { get; set; } = default!;

    public bool IsFolder {get; set;}

    /// <summary>
    /// Gets or sets Text. This is Folder or FileName only.
    /// </summary>
    public string Text { get; set; } = default!;

    public DateTime? DateModified { get; set; }

    public long? Size { get; set; }
}