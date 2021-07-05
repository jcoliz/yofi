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

namespace OfxWeb.Asp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = BuildWebHost(args);

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var serviceProvider = services.GetRequiredService<IServiceProvider>();
                    var configuration = services.GetRequiredService<IConfiguration>();
                    Data.Seed.CreateRoles(serviceProvider, configuration).Wait();
                }
                catch (Exception exception)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(exception, "An error occurred while creating roles");
                }
            }

            host.Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .ConfigureAppConfiguration((context, config) => 
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"*** BuildWebHost in {context.HostingEnvironment.EnvironmentName}");
                        if (context.HostingEnvironment.EnvironmentName == "Production")
                        {
                            var builtConfig = config.Build();
                            var KeyVaultName = builtConfig["KeyVaultName"];
                            System.Diagnostics.Debug.WriteLine($"*** Using KeyVault {KeyVaultName}");
                            var secretClient = new SecretClient(
                                new Uri($"https://{builtConfig["KeyVaultName"]}.vault.azure.net/"),
                                new DefaultAzureCredential());
                            config.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"*** ERROR with KeyVault: {ex.Message}");
                    }
                })
                .Build();

        // https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/security/key-vault-configuration/samples/3.x/SampleApp/Program.cs
    }
}
