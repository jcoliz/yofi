using Common.NET;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YoFi.AspNet.Data;
using YoFi.AspNet.Models;
using System.Collections.Generic;
using System.Linq;
using YoFi.AspNet.Boilerplate.Models;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace YoFi.AspNet.Root
{
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

            services.AddRazorPages().AddRazorRuntimeCompilation();

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

            var storageconnection = Configuration.GetConnectionString("StorageConnection");
            if (!string.IsNullOrEmpty(storageconnection))
            {
                logme.Enqueue($"*** AZURESTORAGE *** Found Storage Connection String");
                services.AddSingleton<IPlatformAzureStorage>(new DotNetAzureStorage(storageconnection));
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
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
                //x.MapControllers();
                x.MapControllerRoute(name: "default", pattern: "{controller=Transactions}/{action=Index}/{id?}");
                //x.MapDefaultControllerRoute();
                x.MapRazorPages();
            });

            SetupBlobContainerName(env.IsDevelopment());
        }

        private void SetupBlobContainerName(bool isdevelopment)
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

                Configuration[key] = value.ToLowerInvariant();
            }
        }
    }
}
