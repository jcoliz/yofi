using ManiaLabs.Portable.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ManiaLabs.NET
{
    public class DotNetAzureStorage : IPlatformAzureStorage
    {
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

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        async Task<string> IPlatformAzureStorage.PostTableEntry(string TableName, IReadOnlyDictionary<string, string> Fields)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
#if true
            // Needs to be rewritten for SDK v12, but I don't use table entires here, so task for later.
            throw new NotImplementedException();

#else
            //var app = Platform.Get<IApp>();
            var table = tableClient.GetTableReference(TableName);
            await table.CreateIfNotExistsAsync();

            var properties = Fields.ToDictionary(x => x.Key, x => new EntityProperty(x.Value));
            var rowkey = DateTime.Now.ToBinary().ToString("X");
            var entry = new DynamicTableEntity("1.0" /*app.Version*/, rowkey, "*", properties);

            TableOperation operation = TableOperation.InsertOrReplace(entry);
            var result = await table.ExecuteAsync(operation);

            if (result.HttpStatusCode >= 300)
                throw new Exception($"Azure operation failed: Status {result.HttpStatusCode}");

            return rowkey;
#endif
        }

        async Task<Uri> IPlatformAzureStorage.UploadToBlob(string ContainerName, string FileName, Stream stream, string ContentType)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(Connection);
            BlobContainerClient container = blobServiceClient.GetBlobContainerClient(ContainerName);
            await container.CreateIfNotExistsAsync();

            BlobClient blobClient = container.GetBlobClient(FileName);

            var options = new BlobUploadOptions();
            options.HttpHeaders = new BlobHttpHeaders() { ContentType = ContentType };

            await blobClient.UploadAsync(stream,options);
            return blobClient.Uri;
        }

        /// <summary>
        /// Check whether the given file exists
        /// </summary>
        /// <param name="ContainerName">Name of the container</param>
        /// <param name="stream">Stream of data</param>
        /// <returns>true if exists</returns>
        async Task<bool> IPlatformAzureStorage.DoesBlobExist(string ContainerName, string FileName)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(Connection);
            BlobContainerClient container = blobServiceClient.GetBlobContainerClient(ContainerName);
            if (!await container.ExistsAsync())
                return false;

            var blob = container.GetBlobClient(FileName);
            if (!await blob.ExistsAsync())
                return false;

            return true;
        }

        /// <summary>
        /// Download from the given ContainerName, the given FileName
        /// </summary>
        /// <param name="ContainerName">Name of the container</param>
        /// <param name="FileName">Name of file</param>
        /// <returns></returns>
        async Task<string> IPlatformAzureStorage.DownloadBlob(string ContainerName, string FileName, Stream stream)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(Connection);
            BlobContainerClient container = blobServiceClient.GetBlobContainerClient(ContainerName);
            if (!await container.ExistsAsync())
                return string.Empty;

            var blob = container.GetBlobClient(FileName);
            if (!await blob.ExistsAsync())
                return string.Empty;

            await blob.DownloadToAsync(stream);

            var props = await blob.GetPropertiesAsync();
            return props.Value.ContentType;
        }

        private string Connection;
       
    }
}
