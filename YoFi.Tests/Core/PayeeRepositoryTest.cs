using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using YoFi.AspNet.Controllers;
using YoFi.Core.Models;
using YoFi.Core.Importers;
using YoFi.Core.Repositories;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Core
{
    [TestClass]
    public class PayeeRepositoryTest : BaseRepositoryTest<Payee>
    {
        protected override List<Payee> Items => new List<Payee>()
        {
                    new Payee() { ID = 1, Category = "B", Name = "3" },
                    new Payee() { ID = 2, Category = "A", Name = "2" },
                    new Payee() { ID = 3, Category = "C", Name = "5" },
                    new Payee() { ID = 4, Category = "A", Name = "1" },
                    new Payee() { ID = 5, Category = "B", Name = "4" },

                    new Payee() { Category = "ABCD", Name = "5" },
                    new Payee() { Category = "X", Name = "6" }
        };

        protected override int CompareKeys(Payee x, Payee y) => x.Name.CompareTo(y.Name);

        protected override BaseImporter<Payee> MakeImporter() => new BaseImporter<Payee>(repository);

        [TestInitialize]
        public void SetUp()
        {
            context = new MockDataContext();
            repository = new PayeeRepository(context);
        }
    }
}
