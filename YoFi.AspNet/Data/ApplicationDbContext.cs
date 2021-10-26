using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using YoFi.AspNet.Models;
using YoFi.AspNet.Boilerplate.Models;
using YoFi.Core;

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
    }
}
