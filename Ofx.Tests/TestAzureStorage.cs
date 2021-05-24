using ManiaLabs.Portable.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ManiaLabs.Portable.Tests
{
    public class TestAzureStorage : IPlatformAzureStorage
    {
        public Dictionary<string, Table> TableStorage = new Dictionary<string, Table>();

        public Task<bool> DoesBlobExist(string ContainerName, string FileName)
        {
            throw new NotImplementedException();
        }

        public Task DownloadBlob(string ContainerName, string FileName, Stream stream)
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
        }

        // It's only async beause the interface needs it that way
#pragma warning disable 1998
        public async Task<string> PostTableEntry(string TableName, IReadOnlyDictionary<string, string> Fields)
        {
            if (!TableStorage.ContainsKey(TableName))
                TableStorage[TableName] = new Table();

            var table = TableStorage[TableName];

            table.Add(Fields);

            return (table.Count - 1).ToString();
        }
#pragma warning restore

        public Task UploadToBlob(string ContainerName, string FileName, Stream stream)
        {
            throw new NotImplementedException();
        }

        public Task<Uri> UploadToBlob(string ContainerName, string FileName, Stream stream, string ContentType)
        {
            // Note that in real-life this is always an async method. Here in test code, we aren't
            // doing anything async. But the signature remains for the interface.

            // For this test, we are just going to full ignore it.

            return Task.FromResult<Uri>(new Uri("http://www.nytimes.com/"));
        }

        Task<string> IPlatformAzureStorage.DownloadBlob(string ContainerName, string FileName, Stream stream)
        {
            throw new NotImplementedException();
        }

        public class Table: List<IReadOnlyDictionary<string, string>>
        {
        }
    }
}
