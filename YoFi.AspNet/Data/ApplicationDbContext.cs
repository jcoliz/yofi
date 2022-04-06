using EFCore.BulkExtensions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using YoFi.AspNet.Boilerplate.Models;
using YoFi.Core;
using YoFi.Core.Models;

namespace YoFi.AspNet.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataProvider
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

        #region Entity Sets

        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Payee> Payees { get; set; }
        public DbSet<Split> Splits { get; set; }
        public DbSet<BudgetTx> BudgetTxs { get; set; }
        public DbSet<Receipt> Receipts { get; set; }

        #endregion

        #region CRUD Entity Accessors

        IQueryable<T> IDataProvider.Get<T>() where T : class
        {
            return Set<T>();
        }

        IQueryable<TEntity> IDataProvider.GetIncluding<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> navigationPropertyPath) where TEntity : class
            => base.Set<TEntity>().Include(navigationPropertyPath);

        void IDataProvider.Add(object item)
        {
            base.Add(item);
        }

        void IDataProvider.Update(object item)
        {
            base.Update(item);
        }

        void IDataProvider.Remove(object item) 
        {
            base.Remove(item); 
        }

        Task IDataProvider.SaveChangesAsync()
        {
            return base.SaveChangesAsync();
        }

        #endregion

        #region Async Queries

        Task<List<T>> IDataProvider.ToListNoTrackingAsync<T>(IQueryable<T> query) 
        {
            return query.AsNoTracking().ToListAsync(); 
        }

        Task<int> IDataProvider.CountAsync<T>(IQueryable<T> query)
        {
            return query.CountAsync();
        }

        Task<bool> IDataProvider.AnyAsync<T>(IQueryable<T> query) 
        {
            return query.AnyAsync(); 
        }

        #endregion

        #region Bulk Operations

        Task<int> IDataProvider.ClearAsync<T>() where T : class => Set<T>().BatchDeleteAsync();

        async Task IDataProvider.BulkInsertAsync<T>(IList<T> items)
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

        async Task IDataProvider.BulkDeleteAsync<T>(IQueryable<T> items)
        {
            if (inmemory)
            {
                base.Set<T>().RemoveRange(items);
                await base.SaveChangesAsync();
            }
            else
                await items.BatchDeleteAsync();
        }

        async Task IDataProvider.BulkUpdateAsync<T>(IQueryable<T> items, T newvalues, List<string> columns)
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

        #endregion
    }
}
