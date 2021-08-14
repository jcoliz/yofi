using System;
using System.Collections.Generic;
using System.IO;

namespace YoFi.AspNet.Common
{
    public interface ISpreadsheetWriter : IDisposable
    {
        /// <summary>
        /// Open the reader for writing to @p stream
        /// </summary>
        /// <param name="stream"></param>
        void Open(Stream stream);

        /// <summary>
        /// Write an enumerable of items to a new  sheet named @sheetname
        /// </summary>
        /// <remarks>
        /// This can be called multiple times on the same open reader
        /// </remarks>
        /// <param name="sheetname">Name of sheet. Will be inferred from name of T if not supplied</param>
        void Write<T>(IEnumerable<T> items, string sheetname = null) where T : class;

        /// <summary>
        /// The names of all the individuals sheets
        /// </summary>
        IEnumerable<string> SheetNames { get; }
    }
}
