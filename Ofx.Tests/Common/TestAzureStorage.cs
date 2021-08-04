using ManiaLabs.Portable.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Common.Test.Helper;

namespace Common.Test.Mock
{
    public class TestAzureStorage : IPlatformAzureStorage
    {
        public Dictionary<string, Table> TableStorage = new Dictionary<string, Table>();

        public List<BlobItem> BlobItems = new List<BlobItem>();

        public Task<bool> DoesBlobExist(string ContainerName, string FileName)
        {
            throw new NotImplementedException();
        }

#if false
        public async Task<string> DownloadBlob(string ContainerName, string FileName, Stream stream)
        {
        }
#endif

        public void Initialize()
        {
        }

        // It's only async beause the interface needs it that way
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

            BlobItems.Add(new BlobItem() { ContainerName = ContainerName, FileName = FileName, ContentType = ContentType });

            return Task.FromResult<Uri>(new Uri("http://www.nytimes.com/"));
        }

        Task<string> IPlatformAzureStorage.DownloadBlob(string ContainerName, string FileName, Stream stream)
        {
            var blobitem = BlobItems.Where(x => x.FileName == FileName).SingleOrDefault();

            if (null == blobitem)
                throw new ApplicationException("Blob not found");

            if (string.IsNullOrEmpty(blobitem.InternalFile))
                throw new ApplicationException("No intenal file for this blob");

            var filestream = SampleData.Open(blobitem.InternalFile);

            filestream.CopyTo(stream);

            return Task.FromResult<string>(blobitem.ContentType);
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
        };
    }
}
