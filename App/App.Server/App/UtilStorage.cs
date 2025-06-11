using Azure.Storage.Files.DataLake;
using Azure.Storage.Files.DataLake.Models;
using System.Diagnostics;
using System.Text;

public class UtilStorage
{
    private static string folderNameRoot = "data/";

    private static DataLakeDirectoryClient Client(string connectionString)
    {
        return new DataLakeDirectoryClient(connectionString, "app", folderNameRoot.Substring(0, folderNameRoot.Length - 1));
    }

    public static async Task Rename(string connectionString, string folderOrFileName, string folderOrFileNameNew)
    {
        var client = Client(connectionString);
        var isFolderName = (await client.GetSubDirectoryClient(folderOrFileName).ExistsAsync()).Value;
        if (isFolderName)
        {
            await client.GetSubDirectoryClient(folderOrFileName).RenameAsync(folderNameRoot + folderOrFileNameNew);
        }
        else
        {
            await client.GetFileClient(folderOrFileName).RenameAsync(folderNameRoot + folderOrFileNameNew);
        }
    }

    public static async Task<List<string>> FileOrFolderNameList(string connectionString, string? folderName = null, bool isRecursive = false)
    {
        var result = new List<string>();
        var client = Client(connectionString);
        await foreach (var pathItem in client.GetSubDirectoryClient(folderName).GetPathsAsync(recursive: isRecursive))
        {
            Debug.Assert(pathItem.Name.StartsWith(folderNameRoot));
            result.Add(pathItem.Name.Substring(folderNameRoot.Length));
        }
        return result;
    }

    public static async Task<string> Download(string fileName, string connectionString)
    {
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

    public static async Task Download(string fileNameStorage, string fileNameLocal, string connectionString)
    {
        using var file = DownloadStream(fileNameStorage, connectionString);
        using var streamStorage = file.Content;
        using var streamLocal = new FileStream(fileNameLocal, FileMode.Create);
        await streamStorage.CopyToAsync(streamLocal);
        streamLocal.Close();
        streamStorage.Close();
    }

    public static DataLakeFileReadStreamingResult DownloadStream(string fileName, string connectionString)
    {
        var client = Client(connectionString);
        var result = client.GetFileClient(fileName).ReadStreaming().Value;
        return result; // result.Content is the Stream. Beware of DataLakeFileReadStreamingResult.IDisposable
    }

    public static async Task Upload(string fileName, string data, string connectionString)
    {
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