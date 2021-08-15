using System;
using System.Collections.Generic;
using System.IO;

namespace YoFi.AspNet.Common
{
    public interface ISpreadsheetReader : IDisposable
    {
        /// <summary>
        /// Open the reader for reading from @p stream
        /// </summary>
        /// <param name="stream"></param>
        void Open(Stream stream);

        /// <summary>
        /// Read the sheet named @sheetname into an enumerable of T items.
        /// </summary>
        /// <remarks>
        /// This can be called multiple times on the same open reader
        /// </remarks>
        /// <param name="sheetname"></param>
        /// <returns>Enumerable of T items, OR null if @sheetname is not found</returns>
        IEnumerable<T> Read<T>(string sheetname = null, IEnumerable<string> exceptproperties = null) where T : class, new();

        /// <summary>
        /// The names of all the individuals sheets
        /// </summary>
        IEnumerable<string> SheetNames { get; }
    }

}
