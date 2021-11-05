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
    public class SplitImporter : IImporter<Split>
    {
        public Transaction Target { get; set; }

        public SplitImporter(IRepository<Transaction> repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Split>> ProcessImportAsync()
        {
            if (incoming.Any())
            {
                // Why no has AddRange??
                foreach (var split in incoming)
                {
                    Target.Splits.Add(split);
                }

                await _repository.UpdateAsync(Target);
            }

            return incoming.ToList();
        }

        public void QueueImportFromXlsx(Stream stream)
        {
            using var ssr = new SpreadsheetReader();
            ssr.Open(stream);
            var items = ssr.Deserialize<Split>(exceptproperties: new string[] { "ID" });
            incoming.UnionWith(items);
        }

        HashSet<Split> incoming = new HashSet<Split>();
        private readonly IRepository<Transaction> _repository;
    }
}
