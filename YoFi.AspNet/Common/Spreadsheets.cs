using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.AspNet.Common
{
    /*
     * Spreadsheets helpers
     * 
     * My goal is to isolate all spreadsheet reading/writing into ONE class
     * or namespace, so this is the ONLY place that the underlying
     * dependant library is touched.
     * 
     * Scenarios:
     * 
     * 1. Given an open stream to a Spreadsheet, find a single named sheet, read the rows into an IEnumerable<T>
     * 2. Same as #1, plus there can optionally be a second named sheet, whos rows are read into a different IEnumerable<T>
     * 2. Given an open result stream, and an IEnumerable<T>, create a new spreadsheet, then a new named sheet, write the objects to it as rows.
     * 4. Same as #3, plus there can be a second IEnumerable<T>, which is written into a second sheet.
     * 
     * I'm separating these into an interface to ensure that when I refactor
     * spreadsheet handling, I present the same interface externally.
     */

    public interface ISpreadsheetReader: IDisposable
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
        IEnumerable<T> Read<T>(string sheetname = null, bool? includeids = false) where T : class, new();
    }

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
    }

    public class SpreadsheetReader : ISpreadsheetReader
    {
        public void Open(Stream stream)
        {
            _package = new ExcelPackage(stream);
        }

        public IEnumerable<T> Read<T>(string sheetname = null, bool? includeids = false) where T : class, new()
        {
            List<T> result = null;

            var name = sheetname;
            if (string.IsNullOrEmpty(name))
                name = typeof(T).Name + "s";

            var found = _package.Workbook.Worksheets.Where(x => x.Name == name);
            if (found.Any())
            {
                result = new List<T>();
                var worksheet = found.First();
                worksheet.ExtractInto(result,includeids);
            }

            return result;
        }

        ExcelPackage _package;

        #region IDispose
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~SpreadsheetReader()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    public class SpreadsheetWriter : ISpreadsheetWriter
    {
        public void Open(Stream stream)
        {
            _stream = stream;
            _package = new ExcelPackage();
        }

        public void Write<T>(IEnumerable<T> items, string sheetname = null) where T : class
        {
            var name = sheetname;
            if (string.IsNullOrEmpty(name))
                name = typeof(T).Name + "s";

            var worksheet = _package.Workbook.Worksheets.Add(name);
            worksheet.PopulateFrom(items, out _, out _);
        }

        private void Flush()
        {
            _package.SaveAs(_stream);
        }

        Stream _stream;
        ExcelPackage _package;

        #region IDisposable
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // This is where we write the package
                    Flush();

                    // TODO: dispose managed state (managed objects)
                    if (_package != null)
                    {
                        _package.Dispose();
                        _package = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Spreadsheets()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
