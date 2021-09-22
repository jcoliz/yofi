using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using YoFi.AspNet.Data;

namespace YoFi.Tests
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
    }
}
