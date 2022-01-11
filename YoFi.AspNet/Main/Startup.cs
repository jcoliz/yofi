using Common.DotNet;
using Common.EFCore;
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using YoFi.AspNet.Boilerplate.Models;
using YoFi.AspNet.Data;
using YoFi.Core;
using YoFi.Core.Importers;
using YoFi.Core.Models;
using YoFi.Core.Reports;
using YoFi.Core.Repositories;
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

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultUI()
                .AddDefaultTokenProviders();

            // Add application services.
            // Story #1024: Moving to built-in UI. Not sure if I need this?
            //services.AddTransient<IEmailSender, EmailSender>();

            // For an unbranded site, we want to go to the Home page by default
            if (Configuration.GetSection("Brand").Exists())
            {
                services.AddRazorPages().AddRazorRuntimeCompilation();
            }
            else
            {
                services.AddRazorPages(options =>
                {
                    options.Conventions.AddPageRoute("/Home", "/");
                }).AddRazorRuntimeCompilation();
            }

            // https://andrewlock.net/an-introduction-to-session-storage-in-asp-net-core/
            services.AddDistributedMemoryCache();
            services.AddSession();

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
            services.AddScoped<TransactionImporter>();
            services.AddScoped<SplitImporter>();
            services.AddScoped<IImporter<Payee>, BaseImporter<Payee>>();
            services.AddScoped<IImporter<BudgetTx>, BaseImporter<BudgetTx>>();

            services.AddScoped<IReportEngine, ReportBuilder>();

            services.AddScoped<IDataContext, ApplicationDbContext>();
            services.AddScoped<IAsyncQueryExecution, EFCoreAsyncQueryExecution>();
            services.AddSingleton<IClock>(new SystemClock());

            if (Configuration.GetSection(SendGridEmailOptions.Section).Exists())
            {
                services.Configure<SendGridEmailOptions>(Configuration.GetSection(SendGridEmailOptions.Section));
                services.AddTransient<IEmailSender, SendGridEmailService>();
            }

            services.Configure<BrandConfig>(Configuration.GetSection(BrandConfig.Section));
            services.Configure<ApiConfig>(Configuration.GetSection(ApiConfig.Section));

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
#if __DEMO_OPEN_ACCESS__
            if (Configuration.GetSection("Brand").Exists())
                ConfigureAuthorizationNormal(services);
            else
                ConfigureAuthorizationDemo(services);
#else
            ConfigureAuthorizationNormal(services);
#endif
            var storageconnection = Configuration.GetConnectionString("StorageConnection");
            if (!string.IsNullOrEmpty(storageconnection))
            {
                logme.Enqueue($"*** AZURESTORAGE *** Found Storage Connection String");
                services.AddSingleton<IStorageService>(new Services.AzureStorageService(storageconnection));
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

            // Note that this configuration setting should be used only for warning the user
            // Not for making any access decisions. All access decisions need to use auth
            // policy.
            Configuration["DemoOpenAccess"] = "True";
        }
#endif

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger, IEnumerable<IStorageService> storages)
        {
            while (logme.Any())
                logger.LogInformation(logme.Dequeue());

            if (env.IsDevelopment())
            {
                logger.LogInformation($"*** CONFIGURE *** Running in Development");

                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
                app.UseDatabaseErrorPage();
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
                // In branded mode, "/" goes to transactions 
                if (Configuration.GetSection("Brand").Exists())
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
