using OfficeOpenXml;
using OfficeOpenXml.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace YoFi.AspNet.Common
{
    public class SpreadsheetWriter : ISpreadsheetWriter
    {
        #region ISpreadsheetWriter (Public Interface)

        public void Open(Stream stream)
        {
            _stream = stream;
            _package = new ExcelPackage();
        }

        public void Write<T>(IEnumerable<T> items, string sheetname = null) where T : class
        {
            var name = string.IsNullOrEmpty(sheetname) ? typeof(T).Name : sheetname;
            var worksheet = _package.Workbook.Worksheets.Add(name);

            int rows, cols;
            PopulateFrom(worksheet, items, out rows, out cols);

            var tbl = worksheet.Tables.Add(new ExcelAddressBase(fromRow: 1, fromCol: 1, toRow: rows, toColumn: cols), name);
            tbl.ShowHeader = true;
            tbl.TableStyle = TableStyles.Dark9;
        }
        public IEnumerable<string> SheetNames => _package.Workbook.Worksheets.Select(x => x.Name);

        #endregion

        #region Fields
        Stream _stream;
        ExcelPackage _package;
        #endregion

        #region Internals

        private void Flush()
        {
            _package.SaveAs(_stream);
        }

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
        #endregion

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
