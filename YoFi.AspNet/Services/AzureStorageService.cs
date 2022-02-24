using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using YoFi.Core;

namespace YoFi.Services
{
    [ExcludeFromCodeCoverage]
    public class AzureStorageService : IStorageService
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connection">Connection string</param>
        /// <param name="container">Name of blob container we store in</param>
        public AzureStorageService(string connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Name of the blob storage container we store in
        /// </summary>
        public string ContainerName { get; set; }

        /// <summary>
        /// Download this file from blob storage
        /// </summary>
        /// <exception cref="Exception">
        /// Will throw exceptions if encounters problems
        /// </exception>
        /// <param name="filename">Name of the file in storage</param>
        /// <param name="stream">Target for the bits</param>
        /// <returns>type of the content</returns>
        public async Task<string> DownloadBlobAsync(string filename, Stream stream)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(_connection);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
            if (!await containerClient.ExistsAsync())
                throw new DirectoryNotFoundException($"No such container {ContainerName}");

            var blob = containerClient.GetBlobClient(filename);
            if (!await blob.ExistsAsync())
                throw new FileNotFoundException($"No such file {filename}");

            await blob.DownloadToAsync(stream);

            var props = await blob.GetPropertiesAsync();
            return props.Value.ContentType;
        }

        /// <summary>
        /// Upload this file to blob storage
        /// </summary>
        /// <exception cref="Exception">
        /// Will throw exceptions if encounters problems
        /// </exception>
        /// <param name="filename">Name of the file in storage</param>
        /// <param name="stream">Source of the bits</param>
        /// <param name="contenttype">Type of the content</param>
        /// <returns>Uri where the data is stored</returns>
        public async Task<Uri> UploadBlobAsync(string filename, Stream stream, string contenttype)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(_connection);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync();

            BlobClient blobClient = containerClient.GetBlobClient(filename);

            var options = new BlobUploadOptions()
            {
                HttpHeaders = new BlobHttpHeaders() { ContentType = contenttype }
            };

            await blobClient.UploadAsync(stream, options);
            return blobClient.Uri;
        }

        public async Task<IEnumerable<string>> GetBlobNamesAsync(string prefix = null)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(_connection);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobs = containerClient.GetBlobsAsync(prefix:prefix);

            var result = new List<string>();
            await foreach(var blob in blobs)
                result.Add(blob.Name);

            return result;
        }
        public async Task RemoveBlobAsync(string filename)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(_connection);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync();

            await containerClient.DeleteBlobIfExistsAsync(filename);
        }

        /// <summary>
        /// Connection string
        /// </summary>
        private readonly string _connection;
    }
}
