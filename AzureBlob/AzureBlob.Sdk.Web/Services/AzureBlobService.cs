using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AzureBlob.Sdk.Web.Services
{
    public class AzureBlobService
    {
        private readonly BlobContainerClient _containerClient;
        public string AuthMode { get; }

        public AzureBlobService(IConfiguration config)
        {
            var settings = config.GetSection("AzureBlob");
            string accountName = settings["AccountName"];
            string containerName = settings["ContainerName"];
            string blobUrl = settings["BlobUrl"];
            string sasUrl = settings["SasUrl"];
            string connectionString = settings["ConnectionString"];
            string mode = settings["UseAuthMode"];

            AuthMode = mode.ToUpperInvariant();

            _containerClient = mode switch
            {
                "SAS" => new BlobContainerClient(new Uri(sasUrl)),
                "KEY" => new BlobContainerClient(connectionString, containerName),
                "MSI" => new BlobContainerClient(
                            new Uri($"{blobUrl}/{containerName}"),
                            new DefaultAzureCredential()),
                _ => throw new InvalidOperationException("Invalid auth mode")
            };
        }

        public async Task<string> UploadAsync(Stream fileStream, string fileName)
        {
            var blobClient = _containerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(fileStream, overwrite: true);
            return blobClient.Uri.ToString();
        }

        public async Task<Stream> DownloadAsync(string fileName)
        {
            var blobClient = _containerClient.GetBlobClient(fileName);
            var response = await blobClient.DownloadAsync();
            return response.Value.Content;
        }

        public async Task DeleteAsync(string fileName)
        {
            var blobClient = _containerClient.GetBlobClient(fileName);
            await blobClient.DeleteIfExistsAsync();
        }

        public async Task<List<string>> ListAsync()
        {
            var blobs = new List<string>();
            await foreach (BlobItem blob in _containerClient.GetBlobsAsync())
            {
                blobs.Add(blob.Name);
            }
            return blobs;
        }
    }
}

