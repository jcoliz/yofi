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

namespace YoFi.AspNet.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
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

        IQueryable<T> IDataContext.Get<T>()
        {
            if (typeof(T) == typeof(Transaction))
            {
                return Transactions as IQueryable<T>;
            }
            if (typeof(T) == typeof(Payee))
            {
                return Payees as IQueryable<T>;
            }
            if (typeof(T) == typeof(Split))
            {
                return Splits as IQueryable<T>;
            }
            if (typeof(T) == typeof(BudgetTx))
            {
                return BudgetTxs as IQueryable<T>;
            }
            throw new NotImplementedException();
        }
       
        void IDataContext.Add(object item) => base.Add(item);

        void IDataContext.Update(object item) => base.Update(item);

        void IDataContext.Remove(object item) => base.Remove(item);

        Task IDataContext.SaveChangesAsync() => base.SaveChangesAsync();

        Task<List<T>> IDataContext.ToListNoTrackingAsync<T>(IQueryable<T> query) => query.AsNoTracking().ToListAsync();

        Task<int> IDataContext.CountAsync<T>(IQueryable<T> query) => query.CountAsync();

        Task<bool> IDataContext.AnyAsync<T>(IQueryable<T> query) => query.AnyAsync();

        async Task<int> IDataContext.ClearAsync<T>()
        {
            int result = default;
            if (typeof(T) == typeof(Transaction))
            {
                result = await Transactions.BatchDeleteAsync();
            }
            else if (typeof(T) == typeof(Payee))
            {
                result = await Payees.BatchDeleteAsync();
            }
            else if (typeof(T) == typeof(Split))
            {
                result = await Splits.BatchDeleteAsync();
            }
            else if (typeof(T) == typeof(BudgetTx))
            {
                result = await BudgetTxs.BatchDeleteAsync();
            }
            else
                throw new NotImplementedException();

            return result;
        }

        Task IDataContext.BulkInsertAsync<T>(IList<T> items) =>
            this.BulkInsertAsync(items);

    }
}
