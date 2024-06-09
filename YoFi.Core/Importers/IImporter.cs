using jcoliz.OfficeOpenXml.Serializer;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace YoFi.Core.Importers;

/// <summary>
/// Imports objects of <typeparamref name="T"/> into the database in a standard way
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IImporter<T>
{
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
    void QueueImportFromXlsx(Stream stream);

    /// <summary>
    /// Declare that items from the spreadsheet in the given <paramref name="reader"/> should be
    /// imported.
    /// </summary>
    /// <remarks>
    /// Same behaviour as the stream version, just doesn't need to open the spreadsheet, because
    /// the caller already has it open.
    /// </remarks>
    void QueueImportFromXlsx(ISpreadsheetReader reader);

    /// <summary>
    /// Import previously queued files into their final destination
    /// </summary>
    Task<IEnumerable<T>> ProcessImportAsync();
}
