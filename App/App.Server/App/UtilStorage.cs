using Azure.Storage.Files.DataLake;
using Azure.Storage.Files.DataLake.Models;
using System.Text;

public static class UtilStorage
{
    private static string containerName = "app";

    private static string containerFolderName = "data/"; // "data/Organisation/"

    private static string prefix = "/" + containerName + "/" + containerFolderName;

    private static string PathItemToFolderOrFileName(PathItem value)
    {
        UtilServer.Assert(value.Name.StartsWith(containerFolderName));
        var result = value.Name.Substring(containerFolderName.Length);
        return result;
    }

    private static DataLakeDirectoryClient Client(string connectionString)
    {
        var result = new DataLakeDirectoryClient(connectionString, containerName, containerFolderName).GetSubDirectoryClient(null);
        
        // Debug
        // var list = result.GetPaths(recursive: true).ToArray();
        // foreach (var item in list)
        // {
        //     var folderOrFileName = PathItemToFolderOrFileName(item);
        //     var localPath = result.GetFileClient(folderOrFileName).Uri.LocalPath;
        //     UtilServer.Assert(localPath == prefix + folderOrFileName);
        // }
        
        return result;
    }

    /// <summary>
    /// Ensure folder or file name starts with /app/data/ path also if it uses relative path characters.
    /// </summary>
    private static string Sanatize(string connectionString, string? folderOrFileName)
    {
        folderOrFileName = folderOrFileName?.TrimEnd('/');
        var client = Client(connectionString);
        var result = client.GetSubDirectoryClient(string.IsNullOrEmpty(folderOrFileName) ? "." : folderOrFileName).Uri.LocalPath;
        if (result == prefix + folderOrFileName)
        {
            result = result.Substring(prefix.Length);
            return result;
        }
        throw new Exception($"Folder or file name invalid! ({folderOrFileName})");
    }

    public static async Task Create(string connectionString, string folderName)
    {
        folderName = Sanatize(connectionString, folderName);

        var client = Client(connectionString);
        await client.GetFileClient(folderName).CreateAsync(PathResourceType.Directory);
    }

    public static async Task Delete(string connectionString, string folderOrFileName)
    {
        folderOrFileName = Sanatize(connectionString, folderOrFileName);

        var client = Client(connectionString);
        await client.GetFileClient(folderOrFileName).DeleteAsync();
    }

    public static async Task Rename(string connectionString, string folderOrFileName, string folderOrFileNameNew)
    {
        folderOrFileName = Sanatize(connectionString, folderOrFileName);
        folderOrFileNameNew = Sanatize(connectionString, folderOrFileNameNew);

        var client = Client(connectionString);
        await client.GetFileClient(folderOrFileName).RenameAsync(containerFolderName + folderOrFileNameNew);
    }

    public static async Task<List<UtilStorageEntry>> List(string connectionString, string? folderName = null, bool isRecursive = false)
    {
        folderName = Sanatize(connectionString, folderName);

        var result = new List<UtilStorageEntry>();
        var client = Client(connectionString);
        await foreach (var pathItem in client.GetSubDirectoryClient(folderName).GetPathsAsync(recursive: isRecursive))
        {
            var folderOrFileName = PathItemToFolderOrFileName(pathItem) + (pathItem.IsDirectory == true ? "/" : null);
            var isFolder = pathItem.IsDirectory ?? false;
            var text = folderOrFileName.TrimEnd('/').Substring(folderOrFileName.TrimEnd('/').LastIndexOf("/") + 1);
            var resultItem = new UtilStorageEntry { FolderOrFileName = folderOrFileName, IsFolder = isFolder, Text = text };
            result.Add(resultItem);
        }
        return result;
    }

    public static async Task<string> Download(string connectionString, string fileName)
    {
        fileName = Sanatize(connectionString, fileName);

        string result;
        var client = Client(connectionString);
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

    public static async Task Download(string connectionString, string fileNameStorage, string fileNameLocal)
    {
        fileNameStorage = Sanatize(connectionString, fileNameStorage);

        using var file = DownloadStream(connectionString, fileNameStorage);
        using var streamStorage = file.Content;
        using var streamLocal = new FileStream(fileNameLocal, FileMode.Create);
        await streamStorage.CopyToAsync(streamLocal);
        streamLocal.Close();
        streamStorage.Close();
    }

    public static DataLakeFileReadStreamingResult DownloadStream(string connectionString, string fileName)
    {
        fileName = Sanatize(connectionString, fileName);

        var client = Client(connectionString);
        var result = client.GetFileClient(fileName).ReadStreaming().Value;
        return result; // result.Content is the Stream. Beware of DataLakeFileReadStreamingResult.IDisposable
    }

    public static async Task Upload(string connectionString, string fileName, string? data)
    {
        fileName = Sanatize(connectionString, fileName);

        byte[] result;
        var fileNameExtension = Path.GetExtension(fileName).ToLower();
        var client = Client(connectionString);
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
}