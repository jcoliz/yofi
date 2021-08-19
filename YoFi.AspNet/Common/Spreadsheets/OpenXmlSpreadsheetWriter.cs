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

        public void InsertItems<T>(IEnumerable<T> items, string sheetName)
        {
            var rows = new List<IEnumerable<object>>();

            // Add the properties as a header row

            // If we don't want a property to show up when it's being json serialized, we also don't want 
            // it to show up when we're exporting it.
            var properties = typeof(T).GetProperties().Where(x => !x.IsDefined(typeof(JsonIgnoreAttribute)));
            rows.Add(properties.Select(x => x.Name));

            // Add the items as remaining rows
            rows.AddRange(items.Select(item => properties.Select(x => x.GetValue(item))));

            // Transform the rows into cell seeds
            var seeds = rows.Select(r => { int col = 0; return r.Select(x => new CellSeed(x) { colindex = col++ }).Where(x=>x.IsValid); });

            // Write the seeds as cells into the worksheet
            WorksheetPart worksheetPart = InsertWorksheet(sheetName);
            InsertIntoSheet(worksheetPart, seeds);
            worksheetPart.Worksheet.Save();
        }

        // Given a WorkbookPart, inserts a new worksheet.
        private WorksheetPart InsertWorksheet(string sheetName)
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
            sheets.Append(new Sheet() { Id = relationshipId, SheetId = sheetId, Name = sheetName });
            workbookPart.Workbook.Save();

            return newWorksheetPart;
        }

        private void InsertIntoSheet(WorksheetPart worksheetPart, IEnumerable<IEnumerable<CellSeed>> seeds)
        {
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

            uint rowindex = 1;
            foreach (var rowseed in seeds)
            {
                var children = rowseed.Select(x =>
                    new Cell()
                    {
                        CellReference = ColNameFor(x.colindex) + rowindex,
                        CellValue = new CellValue(x.IsSharedString ? InsertSharedStringItem(x.Value) : x.Value),
                        DataType = new EnumValue<CellValues>(x.DataType)
                    }
                );
                var row = new Row(children) { RowIndex = rowindex, Spans = new ListValue<StringValue>() };
                sheetData.Append(row);
                ++rowindex;
            }
        }

        private static string ColNameFor(int colnumber)
        {
            if (colnumber < 26)
                return new string(new char[] { (char)((int)'A' + colnumber) });
            else
                return ColNameFor(colnumber / 26) + ColNameFor(colnumber % 26);
        }

        // Given text and a SharedStringTablePart, creates a SharedStringItem with the specified text 
        // and inserts it into the SharedStringTablePart. If the item already exists, returns its index.
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

        private void Flush()
        {
            // Generally each part is written as it's created, but the string table can
            // be shared, so has to be written at the end.

            var shareStringPart = spreadSheet.WorkbookPart.AddNewPart<SharedStringTablePart>();
            var items = stringTable.Select(x => new SharedStringItem(new Text(x)));
            shareStringPart.SharedStringTable = new SharedStringTable(items);
        }



        class CellSeed
        {
            public int colindex { get; set; } = 0; // 0-based

            public bool IsSharedString => DataType == CellValues.SharedString;

            public string Value { get; set; } = null;

            public CellValues DataType { get; set; } = CellValues.Number;

            public bool IsValid => Value != null;

            public CellSeed() { }

            public CellSeed(object cel)
            {
                if (cel != null)
                {
                    var t = cel.GetType();

                    if (t == typeof(string))
                    {
                        Value = cel as string;//InsertSharedStringItem(cel as string);
                        DataType = CellValues.SharedString;
                    }
                    else if (t == typeof(decimal))
                    {
                        Value = cel.ToString();
                    }
                    else if (t == typeof(Int32))
                    {
                        Value = cel.ToString();
                    }
                    else if (t == typeof(DateTime))
                    {
                        // https://stackoverflow.com/questions/39627749/adding-a-date-in-an-excel-cell-using-openxml
                        double oaValue = ((DateTime)cel).ToOADate();
                        Value = oaValue.ToString(CultureInfo.InvariantCulture);
                    }
                    else if (t == typeof(Boolean))
                    {
                        Value = ((Boolean)cel) ? "1" : "0";
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
