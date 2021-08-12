using Common.AspNetCore;
using Common.NET;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;
using OfxWeb.Asp.Services;
using System.Collections.Generic;
using System.Linq;

namespace OfxWeb.Asp
{
    public class Startup
    {
        private Queue<string> logme = new Queue<string>();

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
                .AddDefaultTokenProviders();

            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();

            services.AddRazorPages().AddRazorRuntimeCompilation();

            // https://andrewlock.net/an-introduction-to-session-storage-in-asp-net-core/
            services.AddDistributedMemoryCache();
            services.AddSession();

            // Bug 916: Reports endpoint should return content type json, not text.
            // http://www.binaryintellect.net/articles/a1e0e49e-d4d0-4b7c-b758-84234f14047b.aspx
            services.AddControllers()
                .AddJsonOptions(options => 
                { 
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                }
                );

            logme.Enqueue($"*** AZURESTORAGE *** Looking...");

            // Build connection string out of component key parts
            var storagesection = Configuration.GetSection("AzureStorage");

            if (null != storagesection)
            {
                var AccountKey = storagesection.GetValue<string>("AccountKey");
                var AccountName = storagesection.GetValue<string>("AccountName");
                if (null != AccountKey && null != AccountName)
                {
                    logme.Enqueue($"*** AZURESTORAGE *** Found Account {AccountName}");

                    var storageconnection = string.Join(';', storagesection.GetChildren().Select(x => $"{x.Key}={x.Value}"));
                    services.AddSingleton<IPlatformAzureStorage>(new DotNetAzureStorage(storageconnection));
                }
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

            app.UseStaticFiles();

            app.UseSession();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(x => x.MapControllerRoute(name: "default", pattern: "{controller=Transactions}/{action=Index}/{id?}"));
        }
    }
}
