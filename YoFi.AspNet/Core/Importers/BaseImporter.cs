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
    /// <summary>
    /// Generic importer for all types which follow the simple import logic
    /// </summary>
    /// <typeparam name="T">Type to import</typeparam>
    public class BaseImporter<T> : IImporter<T> where T : class, IModelItem<T>, new()
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="repository">Where to store items</param>
        public BaseImporter(IRepository<T> repository)
        {
            _repository = repository;
            _importing = new HashSet<T>(new T().ImportDuplicateComparer);
        }

        /// <summary>
        /// Declare that items from the spreadsheet in the given <paramref name="stream"/> should be
        /// imported.
        /// </summary>
        /// <remarks>
        /// Call this as many times as needed, then call ProcessImportAsync when ready to do the import.
        /// Note that the importer first looks for a tab named nameof(T), then if it can't find it,
        /// the importer will process the first tab in the spreadsheet
        /// </remarks>
        /// <param name="stream">Where to find the spreadsheet to import</param>
        public void QueueImportFromXlsx(Stream stream)
        {
            using var ssr = new SpreadsheetReader();
            ssr.Open(stream);
            var items = ssr.Deserialize<T>(exceptproperties: new string[] { "ID" });
            _importing.UnionWith(items);
        }

        /// <summary>
        /// Import previously queued files into their final destination
        /// </summary>
        public async Task<IEnumerable<T>> ProcessImportAsync()
        {
            // Remove duplicate items
            var result = _importing.Except(_repository.All).ToList();

            // Add remaining items
            await _repository.AddRangeAsync(result);

            // Clear import queue for next time
            _importing.Clear();

            // Return those items for display
            return new T().InDefaultOrder(result.AsQueryable());
        }

        /// <summary>
        /// Where we store the resulting items
        /// </summary>
        private readonly IRepository<T> _repository;

        /// <summary>
        /// Current queue of items to be imported
        /// </summary>
        private readonly HashSet<T> _importing;
    }
}