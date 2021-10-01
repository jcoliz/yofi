using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace jcoliz.OfficeOpenXml.Easy
{
    /// <summary>
    /// Read spreadsheets into memory, using Office OpenXML
    /// </summary>
    /// <see href="https://github.com/OfficeDev/Open-XML-SDK"/>
    /// <remarks>
    /// Originally, I used EPPlus. However, that library has terms for commercial use.
    /// </remarks>
    public class OpenXmlSpreadsheetReader : ISpreadsheetReader
    {
        #region ISpreadsheetReader (Public Interface)

        /// <summary>
        /// The names of all the individual sheets
        /// </summary>
        public IEnumerable<string> SheetNames { get; private set; }

        /// <summary>
        /// Open the reader for reading from <paramref name="stream"/>
        /// </summary>
        /// <param name="stream">Where to read from</param>
        public void Open(Stream stream)
        {
            spreadSheet = SpreadsheetDocument.Open(stream, isEditable: false);
            var workbookpart = spreadSheet.WorkbookPart;
            SheetNames = workbookpart.Workbook.Descendants<Sheet>().Select(x => x.Name.Value).ToList();
        }
        
        /// <summary>
        /// Read the sheet named <paramref name="sheetname"/> into items
        /// </summary>
        /// <remarks>
        /// This can be called multiple times on the same open reader
        /// </remarks>
        /// <typeparam name="T">Type of the items to return</typeparam>
        /// <param name="sheetname">Name of sheet. Will be inferred from name of <typeparamref name="T"/> if not supplied.
        /// Will use first sheet in workbook if it's not found.</param>
        /// <param name="exceptproperties">Properties to exclude from the import</param>
        /// <returns>Enumerable of <typeparamref name="T"/> items, OR null if  <paramref name="sheetname"/> is not found</returns>
        public IEnumerable<T> Read<T>(string sheetname = null, IEnumerable<string> exceptproperties = null) where T : class, new()
        {
            // Fill in default name if not specified
            var name = string.IsNullOrEmpty(sheetname) ? typeof(T).Name : sheetname;

            // Find the worksheet

            var workbookpart = spreadSheet.WorkbookPart;
            var matching = workbookpart.Workbook.Descendants<Sheet>().Where(x => x.Name == name);

            if (matching.Any())
            {
                if (matching.Skip(1).Any())
                    throw new ApplicationException($"Ambiguous sheet name. Shreadsheet has multiple sheets matching {name}.");
            }
            else
            {
                matching = workbookpart.Workbook.Descendants<Sheet>();

                if (!matching.Any())
                    return null;
            }

            var sheet = matching.Single();
            WorksheetPart worksheetPart = (WorksheetPart)(workbookpart.GetPartById(sheet.Id));

            // Determine the extents of the cells contained within in this sheet.
            // Transform cells into a usable dictionary
            var celldict = worksheetPart.Worksheet.Descendants<Cell>().ToDictionary(x => x.CellReference.Value, x => x);

            // Determine extent of cells

            // Note that rows are 1-based, and columns are 0-based, to make them easier to convert to/from letters
            var regex = new Regex(@"([A-Za-z]+)(\d+)");
            var matches = celldict.Keys.Select(x => regex.Match(x).Groups);
            var maxrow = matches.Max(x => Convert.ToInt32(x[2].Value));
            var maxcol = matches.Max(x => ColNumberFor(x[1].Value));

            // There needs to be at least a header and one data value to be useful
            if (maxrow < 2U)
                return null;

            // Read row 1 into the headers
            var headers = ReadRow(celldict, 1, maxcol);

            // Read rows 2+ into the result items
            var result = new List<T>();
            for (uint row = 2; row <= maxrow; row++)
            {
                // Extract raw row data
                var rowdata = ReadRow(celldict, row, maxcol);

                // Transform keys based on headers
                // Removing properties we don't want
                var line = rowdata
                    .Where(
                        x => exceptproperties?.Any(p=>p == headers[x.Key]) != true
                        &&
                        headers.ContainsKey(x.Key)
                        &&
                        ! string.IsNullOrEmpty(headers[x.Key])
                    )
                    .ToDictionary(x => headers[x.Key], x => x.Value);

                // Transform into result object
                var item = CreateFromDictionary<T>(dictionary: line);

                result.Add(item);
            }

            return result;
        }

        #endregion

        #region Internals

        /// <summary>
        /// Read a single row out of a sheet
        /// </summary>
        /// <param name="cells">All cells in sheet</param>
        /// <param name="row">Which row, from 1</param>
        /// <param name="maxcol">Largest valid column number, from 0</param>
        /// <returns>Cell values mapped to column number where found, from 0</returns>
        private Dictionary<uint, string> ReadRow(Dictionary<string, Cell> cells, uint row, uint maxcol)
        {
            var result = new Dictionary<uint, string>();

            for (uint col = 0; col <= maxcol; col++)
            {
                var value = string.Empty;
                var celref = ColNameFor(col) + row;
                var cell = cells.GetValueOrDefault(celref);
                if (null != cell)
                {
                    if (cell.DataType != null && cell.DataType == CellValues.SharedString)
                        value = FindSharedStringItem(cell.CellValue?.Text);

                    else if (!string.IsNullOrEmpty(cell.CellValue?.Text))
                        value = cell.CellValue.Text;
                }
                result[col] = value;
            }

            return result;
        }

        /// <summary>
        /// Look up a string from the shared string table part
        /// </summary>
        /// <param name="id">ID for the string, 0-based integer in string form</param>
        /// <exception cref="ApplicationException">
        /// Throws if there is no string table, or if the string can't be found.
        /// </exception>
        /// <returns>The string found</returns>
        private string FindSharedStringItem(string id)
        {
            var shareStringPart = spreadSheet.WorkbookPart.GetPartsOfType<SharedStringTablePart>().SingleOrDefault();

            if (null == shareStringPart)
                throw new ApplicationException("Shared string cell found, but no shared string table!");

            var table = shareStringPart.SharedStringTable;
            var found = table.Skip(Convert.ToInt32(id));
            var result = found.FirstOrDefault()?.InnerText;

            if (null == result)
                throw new ApplicationException($"Unable to find shared string reference for id {id}!");

            return result;
        }

        #endregion

        #region Static Internals

        /// <summary>
        /// Create an object from a <paramref name="dictionary"/> of property values
        /// </summary>
        /// <typeparam name="T">What type of object to create</typeparam>
        /// <param name="dictionary">Dictionary of strings to values, where each key is a property name</param>
        /// <returns>The created object of type <typeparamref name="T"/></returns>
        private static T CreateFromDictionary<T>(Dictionary<string, string> dictionary) where T : class, new()
        {
            var item = new T();

            foreach (var kvp in dictionary)
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

                        if ( double.TryParse(kvp.Value, out double dvalue) )
                        {
                            var value = DateTime.FromOADate(dvalue);
                            property.SetValue(item, value);
                        }
                    }
                    else if (property.PropertyType == typeof(int))
                    {
                        if (int.TryParse(kvp.Value, out int value))
                            property.SetValue(item, value);
                    }
                    else if (property.PropertyType == typeof(decimal))
                    {
                        if (decimal.TryParse(kvp.Value, out decimal value))
                            property.SetValue(item, value);
                    }
                    else if (property.PropertyType == typeof(bool))
                    {
                        // Bool is represented as 0/1.
                        // But maybe somettimes it will come in as true/false
                        // So I'll deal with each

                        if (int.TryParse(kvp.Value, out int intvalue))
                            property.SetValue(item, intvalue != 0);
                        else
                        {
                            if (bool.TryParse(kvp.Value, out bool value))
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

        /// <summary>
        /// Convert string column name to integer index
        /// </summary>
        /// <param name="colname">Base 26-style column name, e.g. "AF"</param>
        /// <returns>0-based integer column number, e.g. "A" = 0</returns>
        private static uint ColNumberFor(IEnumerable<char> colname)
        {
            if (colname == null || !colname.Any())
                return 0;

            var last = (uint)colname.Last() - (uint)'A';
            var others = ColNumberFor(colname.SkipLast(1));

            return last + 26U * (1 + others);
        }

        /// <summary>
        /// Convert column number to spreadsheet name
        /// </summary>
        /// <param name="colnumber">0-based integer column number, e.g. "A" = 0</param>
        /// <returns>Base 26-style column name, e.g. "AF"</returns>
        private static string ColNameFor(uint number)
        {
            if (number < 26)
                return new string(new char[] { (char)((int)'A' + number) });
            else
                return ColNameFor((number / 26) - 1) + ColNameFor(number % 26);
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
