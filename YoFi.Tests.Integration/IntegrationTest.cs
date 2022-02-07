using AngleSharp.Html.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using YoFi.AspNet.Data;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration
{
    public class IntegrationTest
    {
        protected static IntegrationContext integrationcontext;
        protected static HtmlParser parser => integrationcontext.parser;
        protected static HttpClient client => integrationcontext.client;
        protected static ApplicationDbContext context => integrationcontext.context;

        public TestContext TestContext { get; set; }
    }
}
