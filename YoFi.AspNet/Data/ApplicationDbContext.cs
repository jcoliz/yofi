using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using YoFi.Core.Models;
using YoFi.AspNet.Boilerplate.Models;
using YoFi.Core;
using EFCore.BulkExtensions;
using System.Linq.Expressions;

namespace YoFi.AspNet.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataContext
    {
        private readonly bool inmemory;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            // Bulk operations cannot be completed on an in-memory database
            // TODO: I wish there was a cleaner way to do this.
            inmemory = Database.ProviderName.Contains("InMemory");
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);

            builder.Entity<Split>().ToTable("Split");

            builder.Entity<Transaction>().HasIndex(p => new { p.Timestamp, p.Hidden, p.Category });
        }

        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Payee> Payees { get; set; }
        public DbSet<Split> Splits { get; set; }
        public DbSet<BudgetTx> BudgetTxs { get; set; }

        IQueryable<Payee> IDataContext.Payees => Payees;

        IQueryable<Transaction> IDataContext.Transactions => Transactions;

        IQueryable<Split> IDataContext.Splits => Splits;

        IQueryable<BudgetTx> IDataContext.BudgetTxs => BudgetTxs;

        IQueryable<Transaction> IDataContext.TransactionsWithSplits => Transactions.Include(x => x.Splits);

        IQueryable<Split> IDataContext.SplitsWithTransactions => Splits.Include(x => x.Transaction);

        IQueryable<T> IDataContext.Get<T>() where T : class => Set<T>();

        void IDataContext.Add(object item) => base.Add(item);

        void IDataContext.Update(object item) => base.Update(item);

        void IDataContext.Remove(object item) => base.Remove(item);

        Task IDataContext.SaveChangesAsync() => base.SaveChangesAsync();

        Task<List<T>> IDataContext.ToListNoTrackingAsync<T>(IQueryable<T> query) => query.AsNoTracking().ToListAsync();

        Task<int> IDataContext.CountAsync<T>(IQueryable<T> query) => query.CountAsync();

        Task<bool> IDataContext.AnyAsync<T>(IQueryable<T> query) => query.AnyAsync();

        Task<int> IDataContext.ClearAsync<T>() where T : class => Set<T>().BatchDeleteAsync();

        async Task IDataContext.BulkInsertAsync<T>(IList<T> items)
        {
            if (inmemory)
            {
                await base.Set<T>().AddRangeAsync(items);
                await base.SaveChangesAsync();
            }
            else
            {
                // Note "IncludeGraph" locks to SQL Server.
                // For more complex but portable alternative, see
                // https://github.com/borisdj/EFCore.BulkExtensions
                await this.BulkInsertAsync(items, b => b.IncludeGraph = true);
            }
        }

        Task IDataContext.BulkDeleteAsync<T>(IQueryable<T> items)
        {
            return items.BatchDeleteAsync();
        }

        Task IDataContext.BulkUpdateAsync<T>(IQueryable<T> items, T newvalues, List<string> columns)
        {
            return items.BatchUpdateAsync(newvalues,columns);
        }
    }
}
