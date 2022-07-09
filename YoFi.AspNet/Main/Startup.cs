using Common.DotNet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using YoFi.AspNet.Boilerplate.Models;
using YoFi.Data;
using YoFi.Data.Identity;
using YoFi.AspNet.Pages;
using YoFi.Core;
using YoFi.Core.Importers;
using YoFi.Core.Models;
using YoFi.Core.Reports;
using YoFi.Core.Repositories;
using YoFi.Core.SampleData;
using YoFi.Services;

#if __DEMO_OPEN_ACCESS__
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
#endif

namespace YoFi.AspNet.Main
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        private readonly Queue<string> logme = new Queue<string>();

        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
            services.AddDatabaseDeveloperPageExceptionFilter();
            
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultUI()
                .AddDefaultTokenProviders();

            // https://andrewlock.net/an-introduction-to-session-storage-in-asp-net-core/
            services.AddDistributedMemoryCache();
            services.AddSession();

            // The following line enables Application Insights telemetry collection.
            services.AddApplicationInsightsTelemetry();

            // Bug 916: Reports endpoint should return content type json, not text.
            // http://www.binaryintellect.net/articles/a1e0e49e-d4d0-4b7c-b758-84234f14047b.aspx
            services.AddControllersWithViews()
                .AddJsonOptions(options => 
                { 
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                }
            );

            services.AddScoped<IBudgetTxRepository, BudgetTxRepository>();
            services.AddScoped<IRepository<BudgetTx>, BudgetTxRepository>();
            services.AddScoped<IPayeeRepository, PayeeRepository>();
            services.AddScoped<IRepository<Payee>, PayeeRepository>();
            services.AddScoped<IRepository<Transaction>, TransactionRepository>();
            services.AddScoped<IRepository<Split>, BaseRepository<Split>>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();
            services.AddScoped<IReceiptRepository, ReceiptRepositoryInDb>();
            services.AddScoped<AllRepositories>();
            services.AddScoped<UniversalImporter>();
            services.AddScoped<TransactionImporter>();
            services.AddScoped<SplitImporter>();
            services.AddScoped<IImporter<Payee>, BaseImporter<Payee>>();
            services.AddScoped<IImporter<BudgetTx>, BaseImporter<BudgetTx>>();
            services.AddScoped<IReportEngine, ReportBuilder>();
            services.AddScoped<IDataProvider, ApplicationDbContext>();
            services.AddScoped<ISampleDataProvider, SampleDataProvider>();
            services.AddScoped<ISampleDataConfiguration, SampleDataConfiguration>();
            services.AddScoped<IDataAdminProvider, DataAdminProvider>();

            if (Configuration.GetSection(SendGridEmailOptions.Section).Exists())
            {
                services.Configure<SendGridEmailOptions>(Configuration.GetSection(SendGridEmailOptions.Section));
                services.AddTransient<IEmailSender, SendGridEmailService>();
            }

            var release = "Unknown";
            if (File.Exists("release.txt"))
            {
                using var sr = File.OpenText("release.txt");
                release = sr.ReadLine();
            }
            Configuration["Codebase:Release"] = release;

            services.Configure<CodebaseConfig>(Configuration.GetSection(CodebaseConfig.Section));
            services.Configure<BrandConfig>(Configuration.GetSection(BrandConfig.Section));
            services.Configure<ApiConfig>(Configuration.GetSection(ApiConfig.Section));
            services.Configure<AdminUserConfig>(Configuration.GetSection(AdminUserConfig.Section));
            services.Configure<AdminModel.PageConfig>(Configuration.GetSection(AdminModel.PageConfig.Section));

            // -----------------------------------------------------------------------------
            //
            // WARNING: Elevation of privelage risk lives here
            //
            // For demo mode, we want a much more open policy than in regular use. Obviously
            // if we have out own real data, we do NOT want this.
            //
            // Define the __DEMO_OPEN_ACCESS__ token during compilation to enable this.
            // The ONLY place this is set is in the build definition file,
            // azure-pipelines-demo.yaml.
            //
            // Furthermore, if you set any of the "Brand" section of configuration
            // variables, demo open access is also disabled. You can set these in your
            // key vault. So this should provide a second level of runtime protection
            // against elevation of privs.
            //
            // -----------------------------------------------------------------------------

            var democonfig = new DemoConfig();
            Configuration.GetSection(DemoConfig.Section).Bind(democonfig);

            // IsOpenAccess cannot be set in configuration. Override it to false for now.
            democonfig.IsOpenAccess = false;

#if __DEMO_OPEN_ACCESS__
            if (!democonfig.IsEnabled)
                ConfigureAuthorizationNormal(services);
            else
                ConfigureAuthorizationDemo(services);

            democonfig.IsOpenAccess = true;
#else
            ConfigureAuthorizationNormal(services);
#endif
            services.AddSingleton<DemoConfig>(democonfig);

            logme.Enqueue($"*** DEMO CONFIG *** {democonfig}");

            var storageconnection = Configuration.GetConnectionString("StorageConnection");
            if (!string.IsNullOrEmpty(storageconnection))
            {
                logme.Enqueue($"*** AZURESTORAGE *** Found Storage Connection String");
                services.AddSingleton<IStorageService>(new Services.AzureStorageService(storageconnection));
            }

            // Setting the system clock is used by functional tests to maintain a controlled environment
            var clock_now = Configuration["Clock:Now"];
            if (clock_now != null)
            {
                logme.Enqueue($"*** CLOCK *** Found clock setting {clock_now}");
                if (System.DateTime.TryParse(clock_now,out var clock_set))
                {
                    logme.Enqueue($"Setting system clock to {clock_set}");
                    services.AddSingleton<IClock>(new TestClock() { Now = clock_set });
                }
                else
                    logme.Enqueue($"Failed to parse as valid time");
            }
            else
                services.AddSingleton<IClock>(new SystemClock());

            if (democonfig.IsHomePageRoot)
            {
                services.AddRazorPages(options =>
                {
                    options.Conventions.AddPageRoute("/Home", "/");
                }).AddRazorRuntimeCompilation();
            }
            else
            {
                services.AddRazorPages().AddRazorRuntimeCompilation();
            }
        }

        private void ConfigureAuthorizationNormal(IServiceCollection services)
        {
            logme.Enqueue($"*** AUTHORIZATION *** Normal ***");

            // Branded mode
            services.AddAuthorization(options =>
            {
                // For regular usage, all access requires that a user is both authenticated 
                // and has bnen assigned the "Verified" role.
                options.AddPolicy("CanWrite", policy => policy.RequireRole("Verified"));
                options.AddPolicy("CanRead", policy => policy.RequireRole("Verified"));
            });
        }

#if __DEMO_OPEN_ACCESS__
        private void ConfigureAuthorizationDemo(IServiceCollection services)
        {
            logme.Enqueue($"*** AUTHORIZATION *** Demo Open Access ***");

            services.AddAuthorization(options =>
            {
                // For demo usage, anonymous users can read any data
                options.AddPolicy("CanRead", policy => policy.AddRequirements(new AnonymousAuth()));

                // For demo usage, anyone who just creats an account can write data
                options.AddPolicy("CanWrite", policy => policy.RequireRole("Verified"));
            });
            services.AddScoped<IAuthorizationHandler, AnonymousAuthHandler>();
        }
#endif

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger, IEnumerable<IStorageService> storages, DemoConfig demo)
        {
            while (logme.Any())
                logger.LogInformation(logme.Dequeue());

            if (env.IsDevelopment())
            {
                logger.LogInformation($"*** CONFIGURE *** Running in Development");

                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                logger.LogInformation($"*** CONFIGURE *** Running in Production");

                app.UseExceptionHandler("/Transactions/Error");
            }

            app.UseStatusCodePagesWithReExecute("/StatusCode","?e={0}");

            // https://stackoverflow.com/questions/41336783/set-culture-and-ui-culture-in-appsettings-json-asp-net-core-localization
            var locale = "en-US"; // Configuration["SiteLocale"];
            RequestLocalizationOptions localizationOptions = new RequestLocalizationOptions
            {
                SupportedCultures = new List<CultureInfo> { new CultureInfo(locale) },
                SupportedUICultures = new List<CultureInfo> { new CultureInfo(locale) },
                DefaultRequestCulture = new RequestCulture(locale)
            };
            app.UseRequestLocalization(localizationOptions);

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseSession();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(x => 
            {
                if (!demo.IsHomePageRoot)
                    x.MapControllerRoute(name: "root", pattern: "/", defaults: new { controller = "Transactions", action = "Index" } );
                
                x.MapControllerRoute(name: "default", pattern: "{controller}/{action=Index}/{id?}");
                x.MapRazorPages();
            });

            // https://newbedev.com/dependency-injection-optional-parameters
            foreach(var storage in storages)
                storage.ContainerName = SetupBlobContainerName(env.IsDevelopment());
        }

        private string SetupBlobContainerName(bool isdevelopment)
        {
            // If blob container name is already set, we're good
            var key = "Storage:BlobContainerName";
            var value = Configuration[key];
            if (string.IsNullOrEmpty(value))
            {
                // It's not set, so we'll need to derive it and set it ourselves

                value = Configuration["Brand:Name"];
                if (string.IsNullOrEmpty(value))
                    value = Configuration["Codebase:Name"];
                if (string.IsNullOrEmpty(value))
                    value = "aspnet";

                if (isdevelopment)
                    value += "-development";
            }

            return value.ToLowerInvariant();
        }
    }

    /// <summary>
    /// Dependency injection container to tell the sample data loader where we
    /// have stored the sample file it needs
    /// </summary>
    public class SampleDataConfiguration : ISampleDataConfiguration
    {
        public SampleDataConfiguration(IWebHostEnvironment e)
        {
            Directory = e.WebRootPath + "/sample";
        }
        public string Directory { get; private set; }
    }

#if __DEMO_OPEN_ACCESS__

    // https://stackoverflow.com/questions/60549828/how-do-i-disable-enable-authentication-at-runtime-in-asp-net-core-2-2
    public class AnonymousAuth: IAuthorizationRequirement
    {
    }

    public class AnonymousAuthHandler : AuthorizationHandler<AnonymousAuth>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AnonymousAuth requirement)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
#endif
}
