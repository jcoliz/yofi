using System;
using System.Collections.Generic;
using System.IO;

namespace jcoliz.OfficeOpenXml.Easy
{
    /// <summary>
    /// Interface for spreadsheet writing
    /// </summary>
    /// <remarks>
    /// The goal here is to hide all the complexity of the underlying spreadsheet reading
    /// technology, and instead present the minimal interface that the app needs from
    /// spreadsheets.
    /// </remarks>
    public interface ISpreadsheetWriter : IDisposable
    {
        /// <summary>
        /// Open the reader for writing to <paramref name="stream"/>
        /// </summary>
        /// <param name="stream">Stream where to write</param>
        void Open(Stream stream);

        /// <summary>
        /// Write <paramref name="items"/> to a new sheet named <paramref name="sheetname"/>
        /// </summary>
        /// <remarks>
        /// This can be called multiple times on the same open reader
        /// </remarks>
        /// <param name="items">Which items to write</param>
        /// <param name="sheetname">Name of sheet. Will be inferred from name of T if not supplied</param>

        void Write<T>(IEnumerable<T> items, string sheetname = null) where T : class;

        /// <summary>
        /// The names of all the individual sheets
        /// </summary>
        IEnumerable<string> SheetNames { get; }
    }
}
