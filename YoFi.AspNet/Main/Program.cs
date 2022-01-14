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
using YoFi.AspNet.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using Common.DotNet;
using Microsoft.Extensions.Options;

namespace YoFi.AspNet.Main
{
    [ExcludeFromCodeCoverage]
    public class Program
    {
        private static readonly Queue<string> logme = new Queue<string>();

        public static void Main(string[] args)
        {
            var host = BuildWebHost(args);

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
                    if (File.Exists("version.txt"))
                    {
                        using var sr = File.OpenText("version.txt");
                        var version = sr.ReadToEnd();
                        logger.LogInformation($"** Version: {version[..8],8}                                        **");
                    }
                    else
                        logger.LogInformation(" Unable to locate version info.");

                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while loading version info");
                }

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
                    var adminuserconfig = services.GetRequiredService<IOptions<AdminUserConfig>>();
                    Data.Seed.CreateRoles(serviceProvider, adminuserconfig).Wait();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while creating roles");
                }

                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>();
                    var democonfig = services.GetRequiredService<DemoConfig>();
                    var clock = services.GetRequiredService<IClock>();
                    Data.Seed.AddSampleData(context, isDemo:democonfig.IsEnabled, clock);
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

        public static IWebHost BuildWebHost(string[] args) =>
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
                })
                .Build();

        // https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/security/key-vault-configuration/samples/3.x/SampleApp/Program.cs
    }
}
