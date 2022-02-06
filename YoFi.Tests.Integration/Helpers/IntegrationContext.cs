using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using YoFi.AspNet.Data;
using YoFi.AspNet.Main;

namespace YoFi.Tests.Integration.Helpers
{
    public class IntegrationContext: IDisposable
    {
        private CustomWebApplicationFactory<Startup> factory;
        private TestServer server;
        private IServiceScope scope;

        public HtmlParser parser { get; private set; }
        public HttpClient client { get; private set; }
        public ApplicationDbContext context { get; private set; }

        public IntegrationContext(string name)
        {
            factory = new CustomWebApplicationFactory<Startup>() { DatabaseName = name };
            server = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, Helpers.TestAuthHandler>(
                            "Test", options => { });
                });
            }).Server;
            parser = new HtmlParser();
            client = server.CreateClient();
            scope = server.Services.CreateScope();
            var scopedServices = scope.ServiceProvider;
            context = scopedServices.GetRequiredService<ApplicationDbContext>();
        }

        public void Dispose()
        {
            scope.Dispose();
        }
    }
}
