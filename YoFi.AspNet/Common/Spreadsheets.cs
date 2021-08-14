using OfficeOpenXml;
using OfficeOpenXml.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
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

        /// <summary>
        /// The names of all the individuals sheets
        /// </summary>
        IEnumerable<string> SheetNames { get; }
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

        /// <summary>
        /// The names of all the individuals sheets
        /// </summary>
        IEnumerable<string> SheetNames { get; }
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
                ExtractInto(worksheet,result, includeids);
            }

            return result;
        }

        public IEnumerable<string> SheetNames => _package.Workbook.Worksheets.Select(x => x.Name);

        ExcelPackage _package;

        private static void ExtractInto<T>(ExcelWorksheet worksheet, ICollection<T> result, bool? includeids = false) where T : new()
        {
            var cols = new List<String>();

            // Read headers
            for (int col = 1; col <= worksheet.Dimension.Columns; col++)
            {
                cols.Add(worksheet.Cells[1, col].Text);
            }

            var columns = new Dictionary<string, int>();
            foreach (var property in typeof(T).GetProperties())
            {
                if (cols.Contains(property.Name))
                    columns[property.Name] = 1 + cols.IndexOf(property.Name);
            }

            // Read rows
            for (int row = 2; row <= worksheet.Dimension.Rows; row++)
            {
                var item = new T();

                try
                {
                    foreach (var property in typeof(T).GetProperties())
                    {
                        if (cols.Contains(property.Name))
                        {
                            var col = columns[property.Name];

                            // Note this excludes typeof(int), which excludes importing
                            // the ID. So if you re-import items you already have, the IDs
                            // will be stripped and ignored. Generally I think that's
                            // good behaviour, but in other implementations, I have done
                            // it the other way where duplicating the ID is a method of
                            // bulk editing.
                            //
                            // ... And this runs up against Pbi #870 :) I now want to
                            // export TransactionID for splits. This means I also need to
                            // export ID for Transactions, so the TransactionID has meaning!

                            if (property.PropertyType == typeof(DateTime))
                            {
                                // Fix #767: Sometimes datetimes are datetimes, othertimes they're doubles
                                DateTime value = DateTime.MinValue;
                                var xlsvalue = worksheet.Cells[row, col].Value;
                                var type = xlsvalue.GetType();
                                if (type == typeof(DateTime))
                                    value = (DateTime)xlsvalue;
                                else if (type == typeof(Double))
                                    value = new DateTime(1900, 1, 1) + TimeSpan.FromDays((Double)xlsvalue - 2.0);
                                property.SetValue(item, value);
                            }
                            else if (property.PropertyType == typeof(Int32) && (includeids ?? false))
                            {
                                var value = Convert.ToInt32((double)worksheet.Cells[row, col].Value);
                                property.SetValue(item, value);
                            }
                            else if (property.PropertyType == typeof(decimal))
                            {
                                var value = Convert.ToDecimal((double)worksheet.Cells[row, col].Value);
                                property.SetValue(item, value);
                            }
                            else if (property.PropertyType == typeof(string))
                            {
                                var value = worksheet.Cells[row, col].Text?.Trim();
                                if (!string.IsNullOrEmpty(value))
                                {
                                    property.SetValue(item, value);
                                }
                            }
                        }
                    }
                    result.Add(item);

                }
                catch (Exception)
                {
                    // Generally if there is an exception creating an item, we will just move onto the next row and ignore it
                }
            }
        }

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

            int rows, cols;
            PopulateFrom(worksheet,items, out rows, out cols);

            var tbl = worksheet.Tables.Add(new ExcelAddressBase(fromRow: 1, fromCol: 1, toRow: rows, toColumn: cols), name);
            tbl.ShowHeader = true;
            tbl.TableStyle = TableStyles.Dark9;
        }
        public IEnumerable<string> SheetNames => _package.Workbook.Worksheets.Select(x => x.Name);

        private void Flush()
        {
            _package.SaveAs(_stream);
        }

        Stream _stream;
        ExcelPackage _package;

        private static void PopulateFrom<T>(ExcelWorksheet worksheet, IEnumerable<T> source, out int rows, out int cols) where T : class
        {
            var data = new List<IEnumerable<object>>();

            // Ignore properties which also should be ignored on JSON serialize
            var properties = source.First().GetType().GetProperties().Where(x => !x.IsDefined(typeof(JsonIgnoreAttribute)));

            // Add a single line for headers
            data.Add(properties.Select(x => x.Name).ToList());

            // Add a line each for each item in source
            data.AddRange(source.Select(item => properties.Select(x => x.GetValue(item)).ToList()));

            int col = 1;
            int row = 1;
            foreach (var line in data)
            {
                col = 1;
                foreach (var field in line)
                {
                    if (null != field)
                    {
                        worksheet.Cells[row, col].Value = field;

                        if (field.GetType() == typeof(DateTime))
                        {
                            worksheet.Cells[row, col].Style.Numberformat.Format = "m/d/yyyy";
                        }
                        else if (field.GetType() == typeof(decimal))
                        {
                            worksheet.Cells[row, col].Style.Numberformat.Format = "$#,##0.00";
                        }
                    }
                    ++col;
                }
                ++row;
            }

            // AutoFitColumns
            worksheet.Cells[1, 1, row, col].AutoFitColumns();

            // Result
            rows = row - 1;
            cols = col - 1;
        }

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
