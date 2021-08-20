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

        Dictionary<string, string> stringTableIDs = new Dictionary<string, string>();
        List<string> stringTable = new List<string>();
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
            //
            // My strategy is to break this problem into a three stage pipeline.
            //
            // 1. Transform the supplied items into a generic type-less matrix
            // 2. Transform those into "cell seeds" which the bare minimum of data
            // we will later need to create cells.
            // 3. Transform those into actual spreadsheet rows/cells.
            //
            // It may be possible to refactor by combinining these stages.
            // However, it's not obvious that doing so would add anything.

            // Stage 1: Transform items into generic matrix
            var rows = new List<IEnumerable<object>>();

            // Add the properties names as a header row

            // If we don't want a property to show up when it's being json serialized, we also don't want 
            // it to show up when we're exporting it.
            var properties = typeof(T).GetProperties().Where(x => !x.IsDefined(typeof(JsonIgnoreAttribute)));
            rows.Add(properties.Select(x => x.Name));

            // Add the property values for each item as a row
            rows.AddRange(items.Select(item => properties.Select(x => x.GetValue(item))));

            // Stage 2: Transform the rows into cell seeds
            var seeds = rows.Select(row => { int colnum = 0; return row.Select(obj => new CellSeed(obj) { colindex = colnum++ }).Where(seed=>seed.IsValid); });

            // Stage 3: Write the seeds as cells into the worksheet
            WorksheetPart worksheetPart = InsertWorksheet(name);
            InsertIntoSheet(worksheetPart, seeds);
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
        /// Insert a matrix of <paramref name="seeds"/> into the given <paramref name="worksheetPart"/>
        /// </summary>
        /// <param name="worksheetPart">Where to insert the cells</param>
        /// <param name="seeds">Sparse matrix of cell seeds, outer enumerable is rows, inner is columns</param>
        private void InsertIntoSheet(WorksheetPart worksheetPart, IEnumerable<IEnumerable<CellSeed>> seeds)
        {
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

            uint rowindex = 1;
            var rows = seeds.Select
            (
                row => new Row
                (
                    row.Select(cell =>
                        new Cell()
                        {
                            CellReference = ColNameFor(cell.colindex) + rowindex,
                            CellValue = new CellValue(cell.IsSharedString ? InsertSharedStringItem(cell.Value) : cell.Value),
                            DataType = new EnumValue<CellValues>(cell.DataType)
                        }
                    )
                ) 
                { 
                    RowIndex = rowindex++, 
                    Spans = new ListValue<StringValue>() 
                }
            );
            sheetData.Append(rows);
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
            string result = null;
            if (!stringTableIDs.TryGetValue(text,out result))
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

        /// <summary>
        /// All the information needed to create a Cell object
        /// </summary>
        class CellSeed
        {
            /// <summary>
            /// Zero-based index describing the cell's column position
            /// </summary>
            public int colindex { get; set; } = 0;

            /// <summary>
            /// Whether this is a "shared string", so should be placed into string table
            /// before committed to output stream
            /// </summary>
            public bool IsSharedString => DataType == CellValues.SharedString;

            /// <summary>
            /// Whether this cell has had a value assigned to it
            /// </summary>
            public bool IsValid => Value != null;

            /// <summary>
            /// The cell value, or null if cell is empty
            /// </summary>
            public string Value { get; set; } = null;

            /// <summary>
            /// The type of cell value
            /// </summary>
            public CellValues DataType { get; set; } = CellValues.Number;

            /// <summary>
            /// Empty constructor
            /// </summary>
            public CellSeed() { }

            /// <summary>
            /// Construct a cell seed from the given <paramref name="object"/>
            /// </summary>
            /// <param name="object">Object to create from</param>
            public CellSeed(object @object)
            {
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
                    }
                    else if (t == typeof(Int32))
                    {
                        Value = @object.ToString();
                    }
                    else if (t == typeof(DateTime))
                    {
                        // https://stackoverflow.com/questions/39627749/adding-a-date-in-an-excel-cell-using-openxml
                        double oaValue = ((DateTime)@object).ToOADate();
                        Value = oaValue.ToString(CultureInfo.InvariantCulture);
                    }
                    else if (t == typeof(Boolean))
                    {
                        Value = ((Boolean)@object) ? "1" : "0";
                        DataType = CellValues.Boolean;
                    }
                }
            }
        };

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
