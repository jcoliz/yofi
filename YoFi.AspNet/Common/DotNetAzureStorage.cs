using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Common.NET
{
    /// <summary>
    /// Manage connection to Azure Storage
    /// </summary>
    public class DotNetAzureStorage : IPlatformAzureStorage
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connection">Connection string to use for this connection</param>
        public DotNetAzureStorage(string connection)
        {
            Connection = connection;
        }

        /// <summary>
        /// Initializes connection to the server
        /// </summary>
        /// <exception cref="Exception">
        /// Will bubble back up exceptions from the azure storage process. Be ready!
        /// </exception>
        void IPlatformAzureStorage.Initialize()
        {
        }

        /// <summary>
        /// Post these <paramref name="fields"/> as a line in the <paramref name="table"/>
        /// </summary>
        /// <param name="table">Which table to post into</param>
        /// <param name="fields">Fields which constitute a table line</param>
        /// <returns>Row key for this item, or null if failed</returns>
        Task<string> IPlatformAzureStorage.PostTableEntry(string table, IReadOnlyDictionary<string, string> fields)
        {
            // Needs to be rewritten for SDK v12, but I don't use table entires here, so task for later.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Upload file to blob
        /// </summary>
        /// <param name="container">Which container to upload into</param>
        /// <param name="name">Filename for the blob within the container</param>
        /// <param name="stream">Source for the data</param>
        /// <param name="contenttype">MIME Content Type of the data</param>
        /// <returns>Uri where the data is stored</returns>
        async Task<Uri> IPlatformAzureStorage.UploadToBlob(string container, string name, Stream stream, string contenttype)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(Connection);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(container);
            await containerClient.CreateIfNotExistsAsync();

            BlobClient blobClient = containerClient.GetBlobClient(name);

            var options = new BlobUploadOptions();
            options.HttpHeaders = new BlobHttpHeaders() { ContentType = contenttype };

            await blobClient.UploadAsync(stream,options);
            return blobClient.Uri;
        }

        /// <summary>
        /// Check whether the given file <paramref name="name"/> exists in the <paramref name="container"/>
        /// </summary>
        /// <param name="container">Name of the container</param>
        /// <param name="name">Filename for the blob within the container</param>
        /// <returns>true if exists</returns>
        async Task<bool> IPlatformAzureStorage.DoesBlobExist(string container, string name)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(Connection);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(container);
            if (!await containerClient.ExistsAsync())
                return false;

            var blob = containerClient.GetBlobClient(name);
            if (!await blob.ExistsAsync())
                return false;

            return true;
        }

        /// <summary>
        /// Download the file <paramref name="name"/> from the given <paramref name="container"/> into the given
        /// <paramref name="stream"/>
        /// </summary>
        /// <param name="container">Name of the container</param>
        /// <param name="name">Filename for the blob within the container</param>
        /// <param name="stream">Where to place the data</param>
        /// <returns>Content type of the blob or empty string if failed</returns>
        async Task<string> IPlatformAzureStorage.DownloadBlob(string container, string name, Stream stream)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(Connection);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(container);
            if (!await containerClient.ExistsAsync())
                return string.Empty;

            var blob = containerClient.GetBlobClient(name);
            if (!await blob.ExistsAsync())
                return string.Empty;

            await blob.DownloadToAsync(stream);

            var props = await blob.GetPropertiesAsync();
            return props.Value.ContentType;
        }

        /// <summary>
        /// Connection sttring for this connection
        /// </summary>
        private string Connection;       
    }
}
