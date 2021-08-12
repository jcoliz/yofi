﻿using Azure.Extensions.AspNetCore.Configuration.Secrets;
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

namespace YoFi.AspNet
{
    public class Program
    {
        private static Queue<string> logme = new Queue<string>();

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

                while (logme.Any())
                    logger.LogInformation(logme.Dequeue());

                try
                {
                    var serviceProvider = services.GetRequiredService<IServiceProvider>();
                    var configuration = services.GetRequiredService<IConfiguration>();
                    Data.Seed.CreateRoles(serviceProvider, configuration).Wait();
                }
                catch (Exception exception)
                {
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
