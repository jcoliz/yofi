using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(YoFi.AspNet.Areas.Identity.IdentityHostingStartup))]
namespace YoFi.AspNet.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
            });
        }
    }
}