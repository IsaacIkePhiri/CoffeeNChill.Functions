using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace CoffeeNChill.Functions.Services;

public class DocumentFileService
{
    private readonly ShareClient _shareClient;

    public DocumentFileService()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("AzureFilesConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "AzureFilesConnection is not configured.");
        }

        _shareClient = new ShareClient(
            connectionString,
            "staff-docs");

        _shareClient.CreateIfNotExists();
    }

    public async Task UploadAsync(
        string fileName,
        Stream fileStream)
    {
        var rootDirectory =
            _shareClient.GetRootDirectoryClient();

        var fileClient =
            rootDirectory.GetFileClient(fileName);

        using var memoryStream = new MemoryStream();

        await fileStream.CopyToAsync(memoryStream);

        memoryStream.Position = 0;

        await fileClient.CreateAsync(memoryStream.Length);

        await fileClient.UploadAsync(memoryStream);
    }

    public async Task<List<ShareFileItem>> ListAsync()
    {
        var rootDirectory =
            _shareClient.GetRootDirectoryClient();

        var files = new List<ShareFileItem>();

        await foreach (var item in rootDirectory.GetFilesAndDirectoriesAsync())
        {
            if (item.IsDirectory != true)
            {
                files.Add(item);
            }
        }

        return files;
    }

    public async Task<Stream> DownloadAsync(string fileName)
    {
        var rootDirectory =
            _shareClient.GetRootDirectoryClient();

        var fileClient =
            rootDirectory.GetFileClient(fileName);

        var download =
            await fileClient.DownloadAsync();

        var memoryStream = new MemoryStream();

        await download.Value.Content.CopyToAsync(memoryStream);

        memoryStream.Position = 0;

        return memoryStream;
    }
}