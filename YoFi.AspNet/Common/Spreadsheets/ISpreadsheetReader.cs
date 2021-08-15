using System;
using System.Collections.Generic;
using System.IO;

namespace YoFi.AspNet.Common
{
    /// <summary>
    /// Interface for spreadsheet reading
    /// </summary>
    /// <remarks>
    /// The goal here is to hide all the complexity of the underlying spreadsheet reading
    /// technology, and instead present the minimal interface that the app needs from
    /// spreadsheets.
    /// </remarks>
    public interface ISpreadsheetReader : IDisposable
    {
        /// <summary>
        /// Open the reader for reading from <paramref name="stream"/>
        /// </summary>
        /// <param name="stream">Where to read from</param>
        void Open(Stream stream);

        /// <summary>
        /// Read the sheet named <paramref name="sheetname"/> into items
        /// </summary>
        /// <remarks>
        /// This can be called multiple times on the same open reader
        /// </remarks>
        /// <typeparam name="T">Type of the items to return</typeparam>
        /// <param name="sheetname">Name of sheet. Will be inferred from name of <typeparamref name="T"/> if not supplied</param>
        /// <param name="exceptproperties">Properties to exclude from the import</param>
        /// <returns>Enumerable of <typeparamref name="T"/> items, OR null if  <paramref name="sheetname"/> is not found</returns>
        IEnumerable<T> Read<T>(string sheetname = null, IEnumerable<string> exceptproperties = null) where T : class, new();

        /// <summary>
        /// The names of all the individual sheets
        /// </summary>
        IEnumerable<string> SheetNames { get; }
    }
}
