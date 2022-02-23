﻿using Common.DotNet;
using Common.DotNet.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Repositories;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Core
{
    [TestClass]
    public class ReceiptRepositoryTest
    {
        ReceiptRepository repository;
        MockTransactionRepository txrepo;
        TestAzureStorage storage;
        TestClock clock;

        [TestInitialize]
        public void SetUp()
        {
            txrepo = new MockTransactionRepository();
            clock = new TestClock() { Now = new DateTime(2022, 6, 1) };
            storage = new TestAzureStorage();
            repository = new ReceiptRepository(txrepo,storage,clock);
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(repository);
        }

        [TestMethod]
        public async Task Upload()
        {
            // Given: A receipt file
            var contenttype = "image/png";
            var length = 25;
            var stream = new MemoryStream(Enumerable.Repeat<byte>(0x60, length).ToArray());

            // When: Uploading it to the repository
            var filename = "Uptown Espresso $5.11 1-2.png";
            await repository.UploadReceiptAsync(filename, stream, contenttype);

            // Then: The receipt is contained in storage
            Assert.AreEqual(1, storage.BlobItems.Count());
            Assert.AreEqual(contenttype, storage.BlobItems.Single().ContentType);
            Assert.AreEqual(filename, storage.BlobItems.Single().FileName);
        }

        [TestMethod]
        public async Task GetNone()
        {
            // Given: Empty storage

            // When: Getting All
            var items = await repository.GetAllAsync();

            // Then: Nothing returned
            Assert.IsFalse(items.Any());
        }

        [TestMethod]
        public async Task GetOne()
        {
            // Given: One receipt in storage
            var filename = "Uptown Espresso $5.11 1-2.png";
            var contenttype = "image/png";
            storage.BlobItems.Add(new TestAzureStorage.BlobItem() { FileName = filename, InternalFile = "budget-white-60x.png", ContentType = contenttype });

            // When: Getting All
            var items = await repository.GetAllAsync();

            // Then: One item returned
            Assert.AreEqual(1, items.Count());

            // And: It was matched correctly
            var actual = items.Single();
            Assert.AreEqual("Uptown Espresso", actual.Name);
            Assert.AreEqual(new DateTime(2022,1,2), actual.Timestamp);
        }

        [TestMethod]
        public async Task GetMany()
        {
            // Given: Many receipts in storage
            var contenttype = "image/png";
            for(int i=1; i<10; i++ )
            {
                var filename = $"Uptown Espresso $5.11 1-{i}.png";
                storage.BlobItems.Add(new TestAzureStorage.BlobItem() { FileName = filename, InternalFile = "budget-white-60x.png", ContentType = contenttype });
            }

            // When: Getting All
            var items = await repository.GetAllAsync();

            // Then: Nine items returned
            Assert.AreEqual(9, items.Count());

            // And: Names matched correctly
            Assert.IsTrue(items.All(x=>x.Name =="Uptown Espresso"));
        }
    }
}
