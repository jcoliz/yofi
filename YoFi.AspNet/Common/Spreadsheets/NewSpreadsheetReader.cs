using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace YoFi.AspNet.Common
{
    public class NewSpreadsheetReader : ISpreadsheetReader
    {
        #region ISpreadsheetReader (Public Interface)

        public IEnumerable<string> SheetNames { get; private set; }

        public void Open(Stream stream)
        {
            // Open the document for not editing.
            spreadSheet = SpreadsheetDocument.Open(stream, isEditable: false);
            var workbookpart = spreadSheet.WorkbookPart;
            SheetNames = workbookpart.Workbook.Descendants<Sheet>().Select(x => x.Name.Value).ToList();
        }

        public IEnumerable<T> Read<T>(string sheetname = null, bool? includeids = false) where T : class, new()
        {
            var name = string.IsNullOrEmpty(sheetname) ? typeof(T).Name : sheetname;

            var workbookpart = spreadSheet.WorkbookPart;
            var matching = workbookpart.Workbook.Descendants<Sheet>().Where(x => x.Name == name);
            if (!matching.Any())
                return null;

            if (matching.Skip(1).Any())
                throw new ApplicationException($"Ambiguous sheet name. Shreadsheet has multiple sheets matching {name}.");

            var sheet = matching.Single();

            // Retrieve a reference to the worksheet part.
            WorksheetPart worksheetPart = (WorksheetPart)(workbookpart.GetPartById(sheet.Id));

            // Also let's get the string table if exists
            var shareStringPart = spreadSheet.WorkbookPart.GetPartsOfType<SharedStringTablePart>().SingleOrDefault();

            // We need to figure out the extents of the cells in this worksheet.

            // First, all the cell references in one place
            var cells = worksheetPart.Worksheet.Descendants<Cell>();
            var cellrefs = cells.Select(x => x.CellReference);

            // Note that rows are 1-based, and columns are 0-based, to make them easier to convert to/from letters
            var regex = new Regex(@"([A-Za-z]+)(\d+)");
            var matches = cellrefs.Select(x => regex.Match(x.Value).Groups);
            var maxrow = matches.Max(x => Convert.ToInt32(x[2].Value));
            var maxcol = matches.Max(x => ColNumberFor(x[1].Value));

            // There needs to be at least a header and one data value to be useful
            if (maxrow < 2U)
                return null;

            // Read row 1 into the headers
            var headers = ReadRow(cells, 1, maxcol);

            // Read rows 2+ into the result items
            var result = new List<T>();
            for (uint row = 2; row <= maxrow; row++)
            {
                // Extract raw row data
                var rowdata = ReadRow(cells, row, maxcol);

                // Transform keys based on headers
                var line = rowdata.ToDictionary(x => headers[x.Key], x => x.Value);

                // Transform into result object
                var item = CreateFromDictionary<T>(source: line, includeints: includeids ?? false);

                result.Add(item);
            }

            return result;
        }

        private Dictionary<uint, string> ReadRow(IEnumerable<Cell> cells, uint row, uint maxcol)
        {
            var shareStringPart = spreadSheet.WorkbookPart.GetPartsOfType<SharedStringTablePart>().SingleOrDefault();

            var line = new Dictionary<uint, string>();
            for (uint col = 0; col <= maxcol; col++)
            {
                var celref = ColNameFor(col) + row;
                var cell = cells.Where(x => x.CellReference.Value == celref).SingleOrDefault();
                string value = string.Empty;
                if (null != cell)
                {
                    if (cell.DataType != null && cell.DataType == CellValues.SharedString)
                    {
                        if (null == shareStringPart)
                            throw new ApplicationException("Shared string cell found, but no shared string table!");

                        value = FindSharedStringItem(cell.CellValue?.Text, shareStringPart);
                    }
                    else if (!string.IsNullOrEmpty(cell.CellValue?.Text))
                    {
                        value = cell.CellValue.Text;
                    }
                }
                line[col] = value;
            }

            return line;
        }

        private static T CreateFromDictionary<T>(Dictionary<string, string> source, bool includeints = false) where T : class, new()
        {
            var item = new T();

            foreach (var kvp in source)
            {
                var property = typeof(T).GetProperties().Where(x => x.Name == kvp.Key).SingleOrDefault();
                if (null != property?.SetMethod)
                {
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
                        // By the time datetimes get here, we expect them to be OADates.
                        // If the original source is an actual date type, that should
                        // be adjusted before now.

                        double dvalue;
                        if ( double.TryParse(kvp.Value, out dvalue) )
                        {
                            var value = DateTime.FromOADate(dvalue);
                            property.SetValue(item, value);
                        }
                    }
                    else if (property.PropertyType == typeof(int) && includeints)
                    {
                        int value;
                        if (int.TryParse(kvp.Value, out value))
                            property.SetValue(item, value);
                    }
                    else if (property.PropertyType == typeof(decimal))
                    {
                        decimal value;
                        if (decimal.TryParse(kvp.Value, out value))
                            property.SetValue(item, value);
                    }
                    else if (property.PropertyType == typeof(bool))
                    {
                        // Bool is represented as 0/1.
                        // But maybe somettimes it will come in as true/false
                        // So I'll deal with each

                        int intvalue;
                        if (int.TryParse(kvp.Value, out intvalue))
                            property.SetValue(item, intvalue != 0);
                        else
                        {
                            bool value;
                            if (bool.TryParse(kvp.Value, out value))
                                property.SetValue(item, value);
                        }
                    }
                    else if (property.PropertyType == typeof(string))
                    {
                        var value = kvp.Value?.Trim();
                        if (!string.IsNullOrEmpty(value))
                            property.SetValue(item, value);
                    }
                }
            }

            return item;
        }

        private static string FindSharedStringItem(string id, SharedStringTablePart shareStringPart)
        {
            var table = shareStringPart.SharedStringTable;
            var found = table.Skip(Convert.ToInt32(id));
            var result = found.FirstOrDefault()?.InnerText;

            if (null == result)
                throw new ApplicationException($"Unable to find shared string reference for id {id}!");

            return result;
        }

        private static uint ColNumberFor(IEnumerable<char> colname)
        {
            if (colname == null || !colname.Any())
                return 0;

            var last = (uint)colname.Last() - (uint)'A';
            var others = ColNumberFor(colname.SkipLast(1));

            return last + 26U * others;
        }

        private static string ColNameFor(uint colnumber)
        {
            if (colnumber < 26)
                return new string(new char[] { (char)((uint)'A' + colnumber) });
            else
                return ColNameFor(colnumber / 26) + ColNameFor(colnumber % 26);
        }


        #endregion

        #region Fields
        SpreadsheetDocument spreadSheet;
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
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~NewSpreadsheetReader()
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
