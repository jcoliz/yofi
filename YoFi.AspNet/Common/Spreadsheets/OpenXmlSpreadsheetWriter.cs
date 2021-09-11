using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace YoFi.AspNet.Common
{
    /// <summary>
    /// Write spreadsheets from memory, using Office OpenXML
    /// </summary>
    /// <remarks>
    /// https://github.com/OfficeDev/Open-XML-SDK
    /// 
    /// Originally, I used EPPlus. However, that library is commercial use and closed source.
    /// </remarks>
    public class OpenXmlSpreadsheetWriter : ISpreadsheetWriter
    {
        #region ISpreadsheetWriter (Public Interface)

        /// <summary>
        /// The names of all the individual sheets
        /// </summary>
        public IEnumerable<string> SheetNames => throw new NotImplementedException();

        /// <summary>
        /// Open the reader for writing to <paramref name="stream"/>
        /// </summary>
        /// <param name="stream">Stream where to write</param>
        public void Open(Stream stream)
        {
            spreadSheet = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
            var workbookpart = spreadSheet.AddWorkbookPart();
            workbookpart.Workbook = new Workbook();
        }

        /// <summary>
        /// Write <paramref name="items"/> to a new sheet named <paramref name="sheetname"/>
        /// </summary>
        /// <remarks>
        /// This can be called multiple times on the same open reader
        /// </remarks>
        /// <param name="items">Which items to write</param>
        /// <param name="sheetname">Name of sheet. Will be inferred from name of T if not supplied</param>
        public void Write<T>(IEnumerable<T> items, string sheetname = null) where T : class
        {
            var name = string.IsNullOrEmpty(sheetname) ? typeof(T).Name : sheetname;
            InsertItems(items, name);
        }

        #endregion

        #region Fields
        SpreadsheetDocument spreadSheet;

        readonly Dictionary<string, string> stringTableIDs = new Dictionary<string, string>();
        readonly List<string> stringTable = new List<string>();
        int stringTableNextID = 0;
        #endregion
        
        #region Internals

        /// <summary>
        /// Insert <paramref name="items"/>items into a new worksheet with the given <paramref name="name"/>.
        /// </summary>
        /// <typeparam name="T">Type of items to insert</typeparam>
        /// <param name="items">Which items to insert</param>
        /// <param name="name">Name of spreadsheet where to insert them</param>
        private void InsertItems<T>(IEnumerable<T> items, string name)
        {
            WorksheetPart worksheetPart = InsertWorksheet(name);
            uint rowindex = 1;

            // Add the properties names as a header row

            var properties = typeof(T).GetProperties();
            InsertIntoSheet(worksheetPart, properties.Select(p => p.Name).ToList(), ref rowindex);

            // Add the property values for each item as a row

            InsertIntoSheet(worksheetPart, items.Select(item => properties.Select(p => p.GetValue(item))).ToList(), ref rowindex);

            worksheetPart.Worksheet.Save();
        }

        /// <summary>
        /// Insert a new worksheet with the given <paramref name="name"/> into the current
        /// workbook
        /// </summary>
        /// <param name="name">The name of the worksheet</param>
        /// <returns>The worksheetpart for the new worksheet</returns>
        private WorksheetPart InsertWorksheet(string name)
        {
            var workbookPart = spreadSheet.WorkbookPart;

            // Add a new worksheet part to the workbook.
            WorksheetPart newWorksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            newWorksheetPart.Worksheet = new Worksheet(new SheetData());

            // Are there already sheets?
            Sheets sheets = workbookPart.Workbook.GetFirstChild<Sheets>();
            if (null == sheets)
                // If not, sdd Sheets to the Workbook.
                sheets = workbookPart.Workbook.AppendChild<Sheets>(new Sheets());

            newWorksheetPart.Worksheet.Save();

            string relationshipId = workbookPart.GetIdOfPart(newWorksheetPart);

            // Get a unique ID for the new sheet.
            var sheetId = sheets.Elements<Sheet>().Select(s => s.SheetId.Value).DefaultIfEmpty().Max() + 1;

            // Append the new worksheet and associate it with the workbook.
            sheets.Append(new Sheet() { Id = relationshipId, SheetId = sheetId, Name = name });
            workbookPart.Workbook.Save();

            return newWorksheetPart;
        }

        /// <summary>
        /// Insert a set of <paramref name="objectrows"/> into the <paramref name="worksheetPart"/>, starting at
        /// row <paramref name="rowindex"/>
        /// </summary>
        /// <remarks>
        /// <paramref name="objectrows"/> are an enumerable of rows, where each row is an enumerable of
        /// arbitrary objects to be inserted.
        /// </remarks>
        /// <see cref="MakeCellFrom(object, string)"/>
        /// <param name="worksheetPart">Insert into this</param>
        /// <param name="objectrows">Insert these object row</param>
        /// <param name="rowindex">Starting at what row</param>
        private void InsertIntoSheet(WorksheetPart worksheetPart, IEnumerable<IEnumerable<object>> objectrows, ref uint rowindex)
        {
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

            var rows = new List<Row>();

            foreach (var or in objectrows)
            {
                uint rowlocal = rowindex;
                int colindex = 0;
                var row =
                    new Row
                    (
                        or.Select(o => MakeCellFrom(o, ColNameFor(colindex++) + rowlocal)).Where(c => c.DataType.Value != CellValues.Error)
                    )
                    {
                        RowIndex = rowindex++,
                        Spans = new ListValue<StringValue>()
                    };
                rows.Add(row);
            }

            sheetData.Append(rows);
        }

        /// <summary>
        /// Insert just a single <paramref name="objectrow"/> into <paramref name="worksheetPart"/>.
        /// </summary>
        /// <see cref="InsertIntoSheet(WorksheetPart, IEnumerable{IEnumerable{object}}, ref uint)"/>
        /// <param name="worksheetPart"></param>
        /// <param name="objectrow"></param>
        /// <param name="rowindex"></param>
        private void InsertIntoSheet(WorksheetPart worksheetPart, IEnumerable<object> objectrow, ref uint rowindex)
        {
            InsertIntoSheet(worksheetPart, new List<IEnumerable<object>>() { objectrow }, ref rowindex);
        }

        /// <summary>
        /// Make a spreadsheet cell from the given <paramref name="object"/>, at the
        /// given <paramref name="cellref"/>.
        /// </summary>
        /// <param name="object">Source object</param>
        /// <param name="cellref">Position in the resulting spreadsheet</param>
        /// <returns></returns>
        private Cell MakeCellFrom(object @object, string cellref)
        {
            string Value = null;
            CellValues DataType = CellValues.Error;

            if (@object != null)
            {
                var t = @object.GetType();

                if (t == typeof(string))
                {
                    Value = @object as string;
                    DataType = CellValues.SharedString;
                }
                else if (t == typeof(decimal))
                {
                    Value = @object.ToString();
                    DataType = CellValues.Number;
                }
                else if (t == typeof(Int32))
                {
                    Value = @object.ToString();
                    DataType = CellValues.Number;
                }
                else if (t == typeof(DateTime))
                {
                    // https://stackoverflow.com/questions/39627749/adding-a-date-in-an-excel-cell-using-openxml
                    double oaValue = ((DateTime)@object).ToOADate();
                    Value = oaValue.ToString(CultureInfo.InvariantCulture);
                    DataType = CellValues.Number;
                }
                else if (t == typeof(Boolean))
                {
                    Value = ((Boolean)@object) ? "1" : "0";
                    DataType = CellValues.Boolean;
                }
            }

            return new Cell()
            {
                CellReference = cellref,
                CellValue = new CellValue(DataType == CellValues.SharedString ? InsertSharedStringItem(Value) : Value),
                DataType = new EnumValue<CellValues>(DataType)
            };
        }
     
        /// <summary>
        /// Convert column <paramref name="number"/> to spreadsheet name
        /// </summary>
        /// <param name="number">0-based integer column number, e.g. "A" = 0</param>
        /// <returns>Base 26-style column name, e.g. "AF"</returns>
        private static string ColNameFor(int number)
        {
            if (number < 26)
                return new string(new char[] { (char)((int)'A' + number) });
            else
                return ColNameFor(number / 26) + ColNameFor(number % 26);
        }

        /// <summary>
        /// Insert <paramref name="text"/> into the current shared string table
        /// </summary>
        /// <param name="text">String to insert</param>
        /// <returns>Lookup key for where the string can be found in the table</returns>
        private string InsertSharedStringItem(string text)
        {
            if (!stringTableIDs.TryGetValue(text,out string result))
            {
                result = stringTableNextID.ToString();
                ++stringTableNextID;

                stringTableIDs[text] = result;
                stringTable.Add(text);
            }
            return result;
        }

        /// <summary>
        /// Perform any pending writing of data to output stream
        /// </summary>
        private void Flush()
        {
            // Generally each part is written as it's created, but the string table can
            // be shared, so has to be written at the end.

            var shareStringPart = spreadSheet.WorkbookPart.AddNewPart<SharedStringTablePart>();
            var items = stringTable.Select(x => new SharedStringItem(new Text(x)));
            shareStringPart.SharedStringTable = new SharedStringTable(items);
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
                    Flush();
                    
                    // TODO: dispose managed state (managed objects)
                    if (null != spreadSheet)
                    {
                        spreadSheet.Dispose();
                        spreadSheet = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~NewSpreadsheetWriter()
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
