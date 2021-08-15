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
    /// Provides ISpreadsheetWriter using Microsoft-supported libraries
    /// </summary>
    public class NewSpreadsheetWriter : ISpreadsheetWriter
    {
        #region ISpreadsheetWriter (Public Interface)

        public IEnumerable<string> SheetNames => throw new NotImplementedException();

        public void Open(Stream stream)
        {
            spreadSheet = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
            var workbookpart = spreadSheet.AddWorkbookPart();
            workbookpart.Workbook = new Workbook();
            var shareStringPart = workbookpart.AddNewPart<SharedStringTablePart>();
            shareStringPart.SharedStringTable = new SharedStringTable();
        }

        public void Write<T>(IEnumerable<T> items, string sheetname = null) where T : class
        {
            var name = string.IsNullOrEmpty(sheetname) ? typeof(T).Name : sheetname;
            InsertItems(spreadSheet, items, name);
        }

        #endregion

        #region Fields
        SpreadsheetDocument spreadSheet;
        #endregion
        
        #region Sample Code

        // All this code came from:
        // https://docs.microsoft.com/en-us/office/open-xml/how-to-insert-text-into-a-cell-in-a-spreadsheet

        // Then I modified it to work more generically, and refactored it for simplicity

        public static void InsertItems<T>(SpreadsheetDocument spreadSheet, IEnumerable<T> items, string sheetName)
        {
            var rows = new List<IEnumerable<object>>();

            // Add the properties as a header row

            // If we don't want a property to show up when it's being json serialized, we also don't want 
            // it to show up when we're exporting it.
            var properties = typeof(T).GetProperties().Where(x => !x.IsDefined(typeof(JsonIgnoreAttribute)));
            rows.Add(properties.Select(x => x.Name));

            // Add the items as remaining rows
            rows.AddRange(items.Select(item => properties.Select(x => x.GetValue(item))));

            var shareStringPart = spreadSheet.WorkbookPart.GetPartsOfType<SharedStringTablePart>().Single();

            WorksheetPart worksheetPart = InsertWorksheet(spreadSheet.WorkbookPart, sheetName);

            uint rowid = 1;
            foreach (var row in rows)
            {
                int colid = 0;
                foreach (var cel in row)
                {
                    if (cel != null)
                    {
                        var t = cel.GetType();
                        string value = null;
                        CellValues datatype = CellValues.Number;

                        if (t == typeof(string))
                        {
                            value = InsertSharedStringItem(cel as string, shareStringPart).ToString();
                            datatype = CellValues.SharedString;
                        }
                        else if (t == typeof(decimal))
                        {
                            value = cel.ToString();
                        }
                        else if (t == typeof(Int32))
                        {
                            value = cel.ToString();
                        }
                        else if (t == typeof(DateTime))
                        {
                            // https://stackoverflow.com/questions/39627749/adding-a-date-in-an-excel-cell-using-openxml
                            double oaValue = ((DateTime)cel).ToOADate();
                            value = oaValue.ToString(CultureInfo.InvariantCulture);
                        }
                        else if (t == typeof(Boolean))
                        {
                            value = ((Boolean)cel)?"1":"0";
                            datatype = CellValues.Boolean;
                        }

                        // If we actually handled it, then do add it, else ignore
                        if (null != value)
                        {
                            Cell cell = InsertCellInWorksheet(ColNameFor(colid), rowid, worksheetPart);
                            cell.CellValue = new CellValue(value);
                            cell.DataType = new EnumValue<CellValues>(datatype);
                        }
                    }

                    ++colid;
                }
                ++rowid;
            }

            worksheetPart.Worksheet.Save();
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
        private static int InsertSharedStringItem(string text, SharedStringTablePart shareStringPart)
        {
            var table = shareStringPart.SharedStringTable;
            var elements = table.Elements<SharedStringItem>();
            var prior = elements.TakeWhile(x => x.InnerText != text);
            var id = prior.Count();

            if (id == elements.Count())
            {
                // The text does not exist in the part. Create the SharedStringItem.
                table.AppendChild(new SharedStringItem(new Text(text)));
                table.Save();
            }

            return id;
        }

        // Given a WorkbookPart, inserts a new worksheet.
        private static WorksheetPart InsertWorksheet(WorkbookPart workbookPart, string sheetName)
        {
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

        // Given a column name, a row index, and a WorksheetPart, inserts a cell into the worksheet. 
        // If the cell already exists, returns it. 
        private static Cell InsertCellInWorksheet(string columnName, uint rowIndex, WorksheetPart worksheetPart)
        {
            Cell result = null;

            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();
            string cellReference = columnName + rowIndex;

            // Find the existing row
            Row row = sheetData.Elements<Row>().Where(r => r.RowIndex == rowIndex).SingleOrDefault();

            // Or create one if that doesn't exist.
            if (null == row)
            {
                row = new Row() { RowIndex = rowIndex, Spans = new ListValue<StringValue>() };
                sheetData.Append(row);
            }

            // Find the existing cell
            result = row.Elements<Cell>().Where(c => c.CellReference.Value == cellReference).SingleOrDefault();

            // If not...
            if (null == result)
            {
                // Create a new one!
                result = new Cell() { CellReference = cellReference };

                // Insert in the correct place. Must be in sequential order according to CellReference.
                var refCell = row.Elements<Cell>().FirstOrDefault(x=> string.Compare(x.CellReference.Value, cellReference, true) > 0);
                row.InsertBefore(result, refCell);

                worksheet.Save();
            }

            return result;
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
