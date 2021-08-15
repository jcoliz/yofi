using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace YoFi.AspNet.Common
{
    public class SpreadsheetReader : ISpreadsheetReader
    {
        #region ISpreadsheetReader (Public Interface)

        public void Open(Stream stream)
        {
            _package = new ExcelPackage(stream);
        }

        public IEnumerable<T> Read<T>(string sheetname = null, IEnumerable<string> exceptproperties = null) where T : class, new()
        {
            List<T> result = null;

            var name = string.IsNullOrEmpty(sheetname) ? typeof(T).Name : sheetname;

            var found = _package.Workbook.Worksheets.Where(x => x.Name == name);
            if (found.Any())
            {
                result = new List<T>();
                var worksheet = found.First();
                ExtractInto(worksheet, result, exceptproperties);
            }

            return result;
        }

        public IEnumerable<string> SheetNames => _package.Workbook.Worksheets.Select(x => x.Name);

        #endregion

        #region Fields

        ExcelPackage _package;

        #endregion

        #region Internals

        private static void ExtractInto<T>(ExcelWorksheet worksheet, ICollection<T> result, IEnumerable<string> exceptproperties) where T : new()
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
                        if (cols.Contains(property.Name) && ! (exceptproperties?.Any(p=>p==property.Name) ?? false))
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
                            else if (property.PropertyType == typeof(Int32))
                            {
                                var value = Convert.ToInt32((double)worksheet.Cells[row, col].Value);
                                property.SetValue(item, value);
                            }
                            else if (property.PropertyType == typeof(decimal))
                            {
                                var value = Convert.ToDecimal((double)worksheet.Cells[row, col].Value);
                                property.SetValue(item, value);
                            }
                            else if (property.PropertyType == typeof(bool))
                            {
                                if (property.SetMethod != null)
                                {
                                    var value = Convert.ToBoolean(worksheet.Cells[row, col].Value);
                                    property.SetValue(item, value);
                                }
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

        #endregion

        #region IDispose
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
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

}
