using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using YoFi.AspNet.Data;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration
{
    [TestClass]
    public class BasicTests
    {
        protected static IntegrationContext integrationcontext;
        protected static HtmlParser parser => integrationcontext.parser;
        protected static HttpClient client => integrationcontext.client;
        protected static ApplicationDbContext context => integrationcontext.context;

        public TestContext TestContext { get; set; }


        [ClassInitialize]
        public static void InitialSetup(TestContext tcontext)
        {
            integrationcontext = new IntegrationContext(tcontext.FullyQualifiedTestClassName);
        }

        [ClassCleanup]
        public static void FinalCleanup()
        {
            integrationcontext.Dispose();
        }

        /// <summary>
        /// Just testing that the top-level pages load OK on direct load
        /// </summary>
        /// <param name="url"></param>
        [DataRow("/Identity/Account/Register")]
        [DataRow("/Home")]
        [DataRow("/Import")]
        [DataRow("/Reports")]
        [DataRow("/Budget")]
        [DataRow("/Help")]
        [DataTestMethod]
        public async Task GetOK(string url)
        {
            // When: Getting "/"
            var response = await client.GetAsync(url);

            // Then: It's OK
            response.EnsureSuccessStatusCode();
        }
    }
}
