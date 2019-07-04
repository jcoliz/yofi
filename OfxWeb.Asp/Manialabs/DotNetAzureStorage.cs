using ManiaLabs.Portable.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;

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
            if (tableClient != null && blobClient != null)
                return;

            // Retrieve storage account from connection string
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Connection);

            // Create the table if it doesn't exist
            if (tableClient == null)
                tableClient = storageAccount.CreateCloudTableClient();

            if (blobClient == null)
                blobClient = storageAccount.CreateCloudBlobClient();
        }

        async Task<string> IPlatformAzureStorage.PostTableEntry(string TableName, IReadOnlyDictionary<string, string> Fields)
        {
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
        }

        async Task<Uri> IPlatformAzureStorage.UploadToBlob(string ContainerName, string FileName, Stream stream)
        {
            CloudBlobContainer container = blobClient.GetContainerReference(ContainerName);
            await container.CreateIfNotExistsAsync();

            CloudBlockBlob blob = container.GetBlockBlobReference(FileName);
            await blob.UploadFromStreamAsync(stream);

            return blob.Uri;
        }

        /// <summary>
        /// Check whether the given file exists
        /// </summary>
        /// <param name="ContainerName">Name of the container</param>
        /// <param name="stream">Stream of data</param>
        /// <returns>true if exists</returns>
        async Task<bool> IPlatformAzureStorage.DoesBlobExist(string ContainerName, string FileName)
        {
            CloudBlobContainer container = blobClient.GetContainerReference(ContainerName);
            if (!await container.ExistsAsync())
                return false;

            CloudBlockBlob blob = container.GetBlockBlobReference(FileName);
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
        async Task IPlatformAzureStorage.DownloadBlob(string ContainerName, string FileName, Stream stream)
        {
            CloudBlobContainer container = blobClient.GetContainerReference(ContainerName);
            if (!await container.ExistsAsync())
                return;

            CloudBlockBlob blob = container.GetBlockBlobReference(FileName);
            if (!await blob.ExistsAsync())
                return;

            await blob.DownloadToStreamAsync(stream);
        }

        private string Connection;
        private CloudTableClient tableClient { set; get; }
        private CloudBlobClient blobClient { set; get; }
    }
}
