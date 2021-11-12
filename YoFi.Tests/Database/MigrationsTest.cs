using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using YoFi.AspNet.Data;

namespace YoFi.Tests.Database
{
    [TestClass]
    public class MigrationsTest
    {



        ApplicationDbContext context;

        List<string> migrations;

        [TestInitialize]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(CreateInMemoryDatabase())
                .Options;

            context = new ApplicationDbContext(options);

        }

        // https://docs.microsoft.com/en-us/ef/core/testing/sqlite
        private static DbConnection CreateInMemoryDatabase()
        {
            var connection = new SqliteConnection("Filename=:memory:");

            connection.Open();

            return connection;
        }

        [TestMethod]
        public void GetMigrations()
        {
            migrations = new List<string>(context.Database.GetMigrations());
        }

        [TestMethod]
        public void FirstMigration()
        {
            migrations = new List<string>(context.Database.GetMigrations());

            IMigrator m = context.GetInfrastructure().GetService(typeof(IMigrator)) as IMigrator;

            m.Migrate(migrations.First());
        }

        //[TestMethod]
        public void AllMigrations()
        {
            migrations = new List<string>(context.Database.GetMigrations());

            IMigrator m = context.GetInfrastructure().GetService(typeof(IMigrator)) as IMigrator;

            foreach(var which in migrations)
            {
                Console.WriteLine(which);
                m.Migrate(which);
            }
        }
    }
}
