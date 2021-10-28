﻿using jcoliz.OfficeOpenXml.Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Repositories;
using YoFi.Core.Models;

namespace YoFi.Tests.Helpers
{
    public interface IMockRepository<T>: IRepository<T> where T: class, IModelItem
    {
        public void AddItems(int numitems);

        public T MakeItem(int x);

        public IEnumerable<T> MakeItems(int numitems);

        public bool Ok { get; set; }
    }

    public class MockBudgetTxRepository : BaseMockRepository<BudgetTx>
    {
        static readonly DateTime defaulttimestamp = new DateTime(2020, 1, 1);

        public override BudgetTx MakeItem(int x) => new BudgetTx() { ID = x, Amount = x, Category = x.ToString(), Timestamp = defaulttimestamp };

        public override IQueryable<BudgetTx> ForQuery(string q) => string.IsNullOrEmpty(q) ? All : All.Where(x => x.Category.Contains(q));
    }
}