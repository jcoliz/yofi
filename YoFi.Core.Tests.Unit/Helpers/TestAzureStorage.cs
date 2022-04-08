using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core;

namespace Common.DotNet.Test
{
    public class TestAzureStorage : IStorageService
    {
        public Dictionary<string, Table> TableStorage = new Dictionary<string, Table>();

        public List<BlobItem> BlobItems = new List<BlobItem>();

        string IStorageService.ContainerName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Task<bool> DoesBlobExist(string ContainerName, string FileName)
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
        }

        public Task<string> PostTableEntry(string TableName, IReadOnlyDictionary<string, string> Fields)
        {
            if (!TableStorage.ContainsKey(TableName))
                TableStorage[TableName] = new Table();

            var table = TableStorage[TableName];

            table.Add(Fields);

            return Task.FromResult<string>((table.Count - 1).ToString());
        }

        public Task UploadToBlob(string ContainerName, string FileName, Stream stream)
        {
            throw new NotImplementedException();
        }

        public Task<Uri> UploadToBlob(string ContainerName, string FileName, Stream stream, string ContentType)
        {
            // Note that in real-life this is always an async method. Here in test code, we aren't
            // doing anything async. But the signature remains for the interface.

            // For this test, we are just going to full ignore it.

            // Save off the stream, in case we want to retrieve it later
            var path = "TestAzureStorage/" + FileName;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.Delete(path);
            using (var writestream = File.OpenWrite(path))
            {
                stream.CopyTo(writestream);
            }

            BlobItems.Add(new BlobItem() { ContainerName = ContainerName, FileName = FileName, ContentType = ContentType, UploadedFile = path });

            return Task.FromResult<Uri>(new Uri("http://www.nytimes.com/"));
        }

        public Task<string> DownloadBlob(string _, string FileName, Stream stream)
        {
            var blobitem = BlobItems.Where(x => x.FileName == FileName).SingleOrDefault();

            if (null == blobitem)
                throw new ApplicationException("Blob not found");

            if (string.IsNullOrEmpty(blobitem.InternalFile))
            {
                if (string.IsNullOrEmpty(blobitem.UploadedFile))
                {
                    throw new ApplicationException("No uploaded or internal file for this blob");
                }

                var filestream = File.OpenRead(blobitem.UploadedFile);
                filestream.CopyTo(stream);
            }
            else
            {
                var filestream = SampleData.Open(blobitem.InternalFile);
                filestream.CopyTo(stream);
            }

            return Task.FromResult<string>(blobitem.ContentType);
        }

        Task<Uri> IStorageService.UploadBlobAsync(string filename, Stream stream, string contenttype) => UploadToBlob("Default", filename, stream, contenttype);

        Task<string> IStorageService.DownloadBlobAsync(string filename, Stream stream) => DownloadBlob("Default", filename, stream);

        public Task<IEnumerable<string>> GetBlobNamesAsync(string prefix = null)
        {
            if (prefix is null)
                return Task.FromResult(BlobItems.Select(x => x.FileName));
            else
                return Task.FromResult(BlobItems.Select(x => x.FileName).Where(x=>x.StartsWith(prefix)));
        }

        public Task RemoveBlobAsync(string filename)
        {
            BlobItems.Remove(BlobItems.FirstOrDefault(x => x.FileName == filename));
            return Task.CompletedTask;
        }

        public class Table: List<IReadOnlyDictionary<string, string>>
        {
        }
        public class BlobItem
        {
            public string ContainerName;
            public string FileName;
            public string ContentType;
            public string InternalFile;
            public string UploadedFile;
        };
    }
}
