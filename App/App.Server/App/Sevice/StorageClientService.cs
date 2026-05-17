using Azure.Storage.Files.DataLake;

/// <summary>
/// Keeps TableStorage client connection.
/// </summary>
public class StorageClientService
{
    public StorageClientService(Configuration configuration)
    {
        var connectionString = configuration.ConnectionStringStorage;
        this.Client = new DataLakeDirectoryClient(connectionString, ContainerName, ContainerFolderName).GetSubDirectoryClient(null);

        // Debug
        // var list = result.GetPaths(recursive: true).ToArray();
        // foreach (var item in list)
        // {
        //     var folderOrFileName = PathItemToFolderOrFileName(item);
        //     var localPath = result.GetFileClient(folderOrFileName).Uri.LocalPath;
        //     UtilServer.Assert(localPath == prefix + folderOrFileName);
        // }
    }

    public static string ContainerName = "app";

    public static string ContainerFolderName = "data/"; // "data/Organisation/"

    public static string Prefix = "/" + ContainerName + "/" + ContainerFolderName;


    public DataLakeDirectoryClient Client { get; }
}
