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

        // Mock receipts data for integration tests
        private List<Receipt> Receipts { get; set; } = new List<Receipt>();
        private int nextrid = 1;

        IQueryable<Payee> IDataContext.Payees => Payees;

        IQueryable<Transaction> IDataContext.Transactions => Transactions;

        IQueryable<Split> IDataContext.Splits => Splits;

        IQueryable<BudgetTx> IDataContext.BudgetTxs => BudgetTxs;

        IQueryable<Transaction> IDataContext.TransactionsWithSplits => Transactions.Include(x => x.Splits);

        IQueryable<Split> IDataContext.SplitsWithTransactions => Splits.Include(x => x.Transaction);

        IQueryable<T> IDataContext.Get<T>() where T : class 
        {
            if (typeof(T) == typeof(Receipt))
            {
                return Receipts.AsQueryable() as IQueryable<T>;
            }
            else
                return Set<T>(); 
        }

        void IDataContext.Add(object item)
        {
            if (item is Receipt r)
            {
                r.ID = nextrid++;
                Receipts.Add(r);
            }
            else
                base.Add(item);
        }

        void IDataContext.Update(object item) => base.Update(item);

        void IDataContext.Remove(object item) 
        {
            if (item is Receipt r)
            {
                Receipts.Remove(r);
            }
            base.Remove(item); 
        }

        Task IDataContext.SaveChangesAsync() => base.SaveChangesAsync();

        Task<List<T>> IDataContext.ToListNoTrackingAsync<T>(IQueryable<T> query) 
        {
            if (query is IQueryable<Receipt> qr)
                return Task.FromResult(qr.ToList() as List<T>);
            else
                return query.AsNoTracking().ToListAsync(); 
        }

        Task<int> IDataContext.CountAsync<T>(IQueryable<T> query) => query.CountAsync();

        Task<bool> IDataContext.AnyAsync<T>(IQueryable<T> query) 
        {
            if (query is IQueryable<Receipt> qr)
                return Task.FromResult(qr.Any());
            else
                return query.AnyAsync(); 
        }

        Task<int> IDataContext.ClearAsync<T>() where T : class => Set<T>().BatchDeleteAsync();

        async Task IDataContext.BulkInsertAsync<T>(IList<T> items)
        {
            if (inmemory)
            {
                base.Set<T>().AddRange(items);
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

        async Task IDataContext.BulkDeleteAsync<T>(IQueryable<T> items)
        {
            if (inmemory)
            {
                base.Set<T>().RemoveRange(items);
                await base.SaveChangesAsync();
            }
            else
                await items.BatchDeleteAsync();
        }

        async Task IDataContext.BulkUpdateAsync<T>(IQueryable<T> items, T newvalues, List<string> columns)
        {
            if (inmemory)
            {
                // We support ONLY a very limited range of possibilities, which is where this
                // method is actually called.
                if (typeof(T) != typeof(Transaction))
                    throw new NotImplementedException("Bulk Update on in-memory DB is only implemented for transactions");

                var txvalues = newvalues as Transaction;
                var txitems = items as IQueryable<Transaction>;
                var txlist = await txitems.ToListAsync();
                foreach (var item in txlist)
                {
                    if (columns.Contains("Imported"))
                        item.Imported = txvalues.Imported;
                    if (columns.Contains("Hidden"))
                        item.Hidden = txvalues.Hidden;
                    if (columns.Contains("Selected"))
                        item.Selected = txvalues.Selected;
                }
                UpdateRange(txlist);

                await SaveChangesAsync();
            }
            else
                await items.BatchUpdateAsync(newvalues,columns);
        }

        public DbSet<YoFi.Core.Models.Receipt> Receipt { get; set; }
    }
}
