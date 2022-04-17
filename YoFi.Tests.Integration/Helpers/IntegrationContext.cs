using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Common.DotNet;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using YoFi.Data;
using YoFi.AspNet.Main;
using YoFi.Core;

namespace YoFi.Tests.Integration.Helpers
{
    public class IntegrationContext: IDisposable
    {
        private readonly CustomWebApplicationFactory<Startup> factory;
        private readonly TestServer server;
        private readonly IServiceScope scope;

        public HtmlParser parser { get; private set; }
        public HttpClient client { get; private set; }
        public ApplicationDbContext context { get; private set; }
        public TestAzureStorage storage { get; private set; }
        public AnonymousAuth canwrite { get; private set; }
        public AnonymousAuth canread { get; private set; }
        public ApiConfig apiconfig { get; private set; }
        public TestClock clock { get; private set; }

        public IntegrationContext(string name)
        {
            factory = new CustomWebApplicationFactory<Startup>() { DatabaseName = name };
            canread = new AnonymousAuth();
            canwrite = new AnonymousAuth();
            server = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, Helpers.TestAuthHandler>(
                            "Test", options => { });

                    services.AddAuthorization(options =>
                    {
                        options.AddPolicy("CanRead", policy => policy.AddRequirements(canread));
                        options.AddPolicy("CanWrite", policy => policy.AddRequirements(canwrite));
                    });
                    services.AddScoped<IAuthorizationHandler, AnonymousAuthHandler>();
                    services.AddSingleton<IStorageService>(storage = new TestAzureStorage());
                    services.AddSingleton<IClock>(clock = new TestClock() { Now = new DateTime( 2022, 12, 31 ) });
                });
            }).Server;
            parser = new HtmlParser();
            client = server.CreateClient();
            scope = server.Services.CreateScope();
            var scopedServices = scope.ServiceProvider;
            context = scopedServices.GetRequiredService<ApplicationDbContext>();
            var apioptions = scopedServices.GetService(typeof(IOptions<ApiConfig>)) as IOptions<ApiConfig>;
            apiconfig = apioptions.Value;
        }

        public void Dispose()
        {
            scope.Dispose();
        }
    }

    public class AnonymousAuth : IAuthorizationRequirement
    {
        public bool Ok { get; set; } = true;
    }

    public class AnonymousAuthHandler : AuthorizationHandler<AnonymousAuth>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AnonymousAuth requirement)
        {
            if (requirement.Ok)
               context.Succeed(requirement);
            else
                context.Fail();
            return Task.CompletedTask;
        }
    }
}
