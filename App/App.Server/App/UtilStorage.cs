using Azure.Storage.Files.DataLake;
using Azure.Storage.Files.DataLake.Models;
using System.Diagnostics;
using System.Text;

public class UtilStorage
{
    private static string folderNameRoot = "data/"; // TODO "app/data/"

    private static DataLakeDirectoryClient Client(string connectionString)
    {
        return new DataLakeDirectoryClient(connectionString, "app", folderNameRoot.Substring(0, folderNameRoot.Length - 1));
    }

    /// <summary>
    /// Ensure folder or file name starts with /app/data/ path also if it uses relative path characters.
    /// </summary>
    private static string Sanatize(string connectionString, string? folderOrFileName)
    {
        folderOrFileName = !string.IsNullOrEmpty(folderOrFileName) ? folderOrFileName : ".";
        var client = Client(connectionString);
        var result = client.GetFileClient(folderOrFileName).Uri.LocalPath;
        var prefix = "/app/" + folderNameRoot;
        if (result.StartsWith(prefix))
        {
            return result.Substring(prefix.Length);
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
        await client.GetFileClient(folderOrFileName).RenameAsync(folderNameRoot + folderOrFileNameNew);
    }

    public static async Task<List<(string FolderOrFileName, bool IsFolder)>> List(string connectionString, string? folderName = null, bool isRecursive = false)
    {
        folderName = Sanatize(connectionString, folderName);

        var result = new List<(string, bool)>();
        var client = Client(connectionString);
        await foreach (var pathItem in client.GetSubDirectoryClient(folderName).GetPathsAsync(recursive: isRecursive))
        {
            Debug.Assert(pathItem.Name.StartsWith(folderNameRoot));
            var folderOrFileName = pathItem.Name.Substring(folderNameRoot.Length)!;
            var isFolder = pathItem.IsDirectory ?? false;
            var resultItem = (folderOrFileName, isFolder);
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
        fileNameLocal = Sanatize(connectionString, fileNameLocal);

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

    public static async Task Upload(string connectionString, string fileName, string data)
    {
        fileName = Sanatize(connectionString, fileName);

        byte[] result;
        var fileNameExtension = Path.GetExtension(fileName).ToLower();
        var client = Client(connectionString);
        switch (fileNameExtension)
        {
            case ".txt":
                result = Encoding.UTF8.GetBytes(data);
                break;
            default:
                result = Convert.FromBase64String(data);
                break;
        }
        using var stream = new MemoryStream(result);
        var fileClient = client.GetFileClient(fileName);
        await fileClient.UploadAsync(stream, overwrite: true);
        stream.Close();
    }
}