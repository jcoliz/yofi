using jcoliz.OfficeOpenXml.Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Repositories;
using YoFi.AspNet.Models;

namespace YoFi.Core.Importers
{
    public class BudgetTxImporter: BaseImporter<BudgetTx>
    {
        public BudgetTxImporter(IRepository<BudgetTx> repository): base(repository, new BudgetTxImportDuplicateComparer())
        {
        }
    }

    /// <summary>
    /// Tells us whether two items are duplicates for the purposes of importing
    /// </summary>
    /// <remarks>
    /// Generally, we don't import duplicates, although some importers override this behavior
    /// </remarks>
    class BudgetTxImportDuplicateComparer : IEqualityComparer<BudgetTx>
    {
        public bool Equals(BudgetTx x, BudgetTx y) => x.Timestamp.Year == y.Timestamp.Year && x.Timestamp.Month == y.Timestamp.Month && x.Category == y.Category;
        public int GetHashCode(BudgetTx obj) => (obj.Timestamp.Year * 12 + obj.Timestamp.Month) ^ obj.Category.GetHashCode();
    }
}

