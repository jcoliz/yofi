using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using YoFi.Data;

namespace ListsWebApp.Data.MigrationsMain;

public class CatalogContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(string.Empty);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
