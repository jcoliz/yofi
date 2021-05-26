﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeOpenXml;
using OfxWeb.Asp.Controllers;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.AspNetCore.Test;

//
// This is my first controller unit test. I am going to start with an easy controller, the category map controller.
//

namespace Ofx.Tests
{


    [TestClass]
    public class CategoryMapControllerTest
    {
        private ControllerTestHelper<CategoryMap, CategoryMapsController> helper = null;

        [TestInitialize]
        public void SetUp()
        {
            helper = new ControllerTestHelper<CategoryMap, CategoryMapsController>();
            helper.SetUp();
            helper.controller = new CategoryMapsController(helper.context);

            helper.Items.Add(new CategoryMap() { Category = "B", SubCategory = "A", Key1 = "1", Key2 = "2", Key3 = "3" });
            helper.Items.Add(new CategoryMap() { Category = "A", SubCategory = "A", Key1 = "1", Key2 = "2", Key3 = "2" });
            helper.Items.Add(new CategoryMap() { Category = "C", SubCategory = "A", Key1 = "1", Key2 = "2", Key3 = "5" });
            helper.Items.Add(new CategoryMap() { Category = "A", SubCategory = "A", Key1 = "1", Key2 = "1", Key3 = "1" });
            helper.Items.Add(new CategoryMap() { Category = "B", SubCategory = "B", Key1 = "1", Key2 = "2", Key3 = "4" });

            helper.dbset = helper.context.CategoryMaps;

            // Sample data items will use 'key3' as a unique sort idenfitier
            helper.KeyFor = (x => x.Key3);
        }

        [TestCleanup]
        public void Cleanup() => helper.Cleanup();
        [TestMethod]
        public void Empty() => helper.Empty();
        [TestMethod]
        public async Task IndexEmpty() => await helper.IndexEmpty();
        [TestMethod]
        public async Task IndexSingle() => await helper.IndexSingle();
        [TestMethod]
        public async Task IndexMany() => await helper.IndexMany();
        [TestMethod]
        public async Task DetailsFound() => await helper.DetailsFound();
        [TestMethod]
        public async Task DetailsNotFound() => await helper.DetailsNotFound();
        [TestMethod]
        public async Task EditFound() => await helper.EditFound();
        [TestMethod]
        public async Task EditNotFound() => await helper.EditNotFound();
        [TestMethod]
        public async Task Create() => await helper.Create();
        [TestMethod]
        public async Task EditObjectValues() => await helper.EditObjectValues();
        [TestMethod]
        public async Task DeleteFound() => await helper.DeleteFound();
        [TestMethod]
        public async Task DeleteConfirmed() => await helper.DeleteConfirmed();
        [TestMethod]
        public async Task Download() => await helper.Download();
        [TestMethod]
        public async Task Upload() => await helper.Upload();
        [TestMethod]
        public async Task UploadWithID() => await helper.UploadWithID();
        [TestMethod]
        public async Task UploadDuplicate() => await helper.UploadDuplicate();

        // TODO Add a test to upload a new category map for an overlapping category/subcat pair with different
        // Keys. This should be rejected, because having duplicate rules with same cat/subcat is wrong.
        // I don't think the code rejects this, so I'll have to work on that.
    }
}