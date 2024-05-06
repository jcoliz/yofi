using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YoFi.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using Common.DotNet;
using Microsoft.Extensions.Options;
using YoFi.Core.SampleData;
using YoFi.Core;

namespace YoFi.AspNet.Main
{
    [ExcludeFromCodeCoverage]
    public class Program
    {
        private static readonly Queue<string> logme = new Queue<string>();

        public static async Task Main(string[] args)
        {
            var host = CreateWebHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();

                logger.LogInformation("**************************************************************");
                logger.LogInformation("**                                                          **");
                logger.LogInformation("** APPLICATION STARTUP                                      **");
                logger.LogInformation("**                                                          **");
                logger.LogInformation("**************************************************************");

                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>();
                    context.Database.Migrate();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while migrating database");
                }

                try
                {
                    var serviceProvider = services.GetRequiredService<IServiceProvider>();
                    YoFi.Main.Seeders.IdentitySeeder.SeedIdentity(serviceProvider).Wait();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while creating roles");
                }

                try
                {
                    var democonfig = services.GetRequiredService<DemoConfig>();
                    if (democonfig.IsEnabled)
                    {
                        var loader = services.GetRequiredService<ISampleDataProvider>();
                        var dbadmin = services.GetRequiredService<IDataAdminProvider>();
                        await dbadmin.SeedDemoSampleData(true,loader);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while seeding the database");
                }

                while (logme.Any())
                    logger.LogInformation(logme.Dequeue());
            }

            if (!args.Contains("--norun"))
                host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .ConfigureAppConfiguration((context, config) => 
                {
                    try
                    {
                        logme.Enqueue($"*** BuildWebHost in {context.HostingEnvironment.EnvironmentName}");
                        if (context.HostingEnvironment.EnvironmentName == "Production")
                        {
                            var builtConfig = config.Build();
                            var KeyVaultUrl = builtConfig["KEY_VAULT_URL"];
                            if (!string.IsNullOrEmpty(KeyVaultUrl))
                            {
                                logme.Enqueue($"*** Using KeyVault {KeyVaultUrl}");
                                var secretClient = new SecretClient(
                                    new Uri(KeyVaultUrl),
                                    new DefaultAzureCredential());
                                config.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logme.Enqueue($"*** ERROR with KeyVault: {ex.Message}");
                    }
                });

        // https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/security/key-vault-configuration/samples/3.x/SampleApp/Program.cs
    }
}
