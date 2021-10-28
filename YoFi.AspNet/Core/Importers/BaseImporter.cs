using jcoliz.OfficeOpenXml.Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.Core.Importers
{
    public class BaseImporter<T> where T: class, IModelItem, new()
    {
        private readonly HashSet<T> _incoming;
        private readonly IRepository<T> _repository;

        public BaseImporter(IRepository<T> repository)
        {
            _repository = repository;
            _incoming = new HashSet<T>(new T().ImportDuplicateComparer);
        }

        public void LoadFromXlsx(Stream stream)
        {
            using var ssr = new SpreadsheetReader();
            ssr.Open(stream);
            var items = ssr.Deserialize<T>(exceptproperties: new string[] { "ID" });
            _incoming.UnionWith(items);
        }

        public async Task<IEnumerable<T>> ProcessAsync()
        {
            // Remove duplicate items
            var result = _incoming.Except(_repository.All).ToList();

            // Add remaining items
            await _repository.AddRangeAsync(result);

            // Return those items for display
            return _repository.InDefaultOrder(result.AsQueryable());
        }
    }
}
