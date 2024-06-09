using jcoliz.OfficeOpenXml.Serializer;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.Core.Importers;

/// <summary>
/// Imports splits into the database, adding them to a specified
/// target transaction
/// </summary>
public class SplitImporter
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="repository">Where should the target transaction be saved?</param>
    public SplitImporter(IRepository<Transaction> repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Import previously queued files into their final destination
    /// </summary>
    /// <param name="target">Transaction to add queued splits</param>
    public async Task<IEnumerable<Split>> ProcessImportAsync(Transaction target)
    {
        if (incoming.Count > 0)
        {
            // Why no has AddRange??
            foreach (var split in incoming)
            {
                target.Splits.Add(split);
            }

            await _repository.UpdateAsync(target);
        }

        return incoming.ToList();
    }

    /// <summary>
    /// Declare that items from the spreadsheet in the given <paramref name="stream"/> should be
    /// imported.
    /// </summary>
    /// <remarks>
    /// Call this as many times as needed, then call ProcessImportAsync when ready to do the import.
    /// </remarks>
    /// <param name="stream">Where to find the spreadsheet to import</param>
    public void QueueImportFromXlsx(Stream stream)
    {
        using var reader = new SpreadsheetReader();
        reader.Open(stream);
        QueueImportFromXlsx(reader);
    }

    public void QueueImportFromXlsx(ISpreadsheetReader reader)
    {
        var items = reader.Deserialize<Split>(exceptproperties: new string[] { "ID" });
        incoming.UnionWith(items);
    }

    /// <summary>
    /// Queue of items waiting to be imported
    /// </summary>
    private readonly HashSet<Split> incoming = [];

    /// <summary>
    /// Where should the target transaction be saved?
    /// </summary>
    private readonly IRepository<Transaction> _repository;
}
