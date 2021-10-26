using jcoliz.OfficeOpenXml.Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Core.Repositories;
using YoFi.AspNet.Models;

namespace YoFi.Core.Importers
{
    public class BudgetTxImporter
    {
        private readonly HashSet<BudgetTx> incoming = new HashSet<BudgetTx>(new BudgetTxComparer());
        private readonly BudgetTxRepository _repository;

        public BudgetTxImporter(BudgetTxRepository repository)
        {
            _repository = repository;
        }

        public void LoadFromXlsx(Stream stream)
        {
            using var ssr = new SpreadsheetReader();
            ssr.Open(stream);
            var items = ssr.Deserialize<BudgetTx>(exceptproperties: new string[] { "ID" });
            incoming.UnionWith(items);
        }

        public async Task<IEnumerable<BudgetTx>> ProcessAsync()
        {
            // Remove duplicate items.
            var result = incoming.Except(_repository.All).ToList();

            // Add remaining items
            await _repository.AddRangeAsync(result);

            return result.OrderBy(x => x.Timestamp.Year).ThenBy(x => x.Timestamp.Month).ThenBy(x => x.Category);
        }
    }

    class BudgetTxComparer : IEqualityComparer<BudgetTx>
    {
        public bool Equals(BudgetTx x, BudgetTx y) => x.Timestamp.Year == y.Timestamp.Year && x.Timestamp.Month == y.Timestamp.Month && x.Category == y.Category;
        public int GetHashCode(BudgetTx obj) => (obj.Timestamp.Year * 12 + obj.Timestamp.Month) ^ obj.Category.GetHashCode();
    }
}

