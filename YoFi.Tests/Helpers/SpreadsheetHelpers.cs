using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace YoFi.Tests.Helpers
{
    public class SpreadsheetHelpers
    {
        #region Sample Code

        // https://docs.microsoft.com/en-us/office/open-xml/how-to-insert-text-into-a-cell-in-a-spreadsheet

        // Given a document name and text, 
        // inserts a new work sheet and writes the text to cell "A1" of the new worksheet.

        // "data" is an enumerable of rows, where rows are an enumerable of columns
        // supported types are string, DateTime, decimal, and int

        public static void InsertItems(Stream stream, IEnumerable<object> items, string sheetName)
        {
            var data = new List<IEnumerable<object>>();

            // First add the headers
            // If we don't want a property to show up when it's being json serialized, we also don't want 
            // it to show up when we're exporting it.
            var properties = items.First().GetType().GetProperties().Where(x => !x.IsDefined(typeof(JsonIgnoreAttribute)));

            data.Add(properties.Select(x => x.Name).ToList());

            // Second, add the items

            data.AddRange(items.Select(item => properties.Select(x => x.GetValue(item)).ToList()));

            // Open the document for editing.
            using (SpreadsheetDocument spreadSheet = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
            {
                // Add a WorkbookPart to the document.
                WorkbookPart workbookpart = spreadSheet.AddWorkbookPart();
                workbookpart.Workbook = new Workbook();

                // Get the SharedStringTablePart. If it does not exist, create a new one.
                SharedStringTablePart shareStringPart;
                if (spreadSheet.WorkbookPart.GetPartsOfType<SharedStringTablePart>().Count() > 0)
                {
                    shareStringPart = spreadSheet.WorkbookPart.GetPartsOfType<SharedStringTablePart>().First();
                }
                else
                {
                    shareStringPart = spreadSheet.WorkbookPart.AddNewPart<SharedStringTablePart>();
                }

                // Insert a new worksheet.
                WorksheetPart worksheetPart = InsertWorksheet(spreadSheet.WorkbookPart, sheetName);

                uint rowid = 1;
                foreach (var row in data)
                {
                    int colid = 0;
                    foreach (var cel in row)
                    {
                        if (cel != null)
                        {
                            var t = cel.GetType();

                            if (t == typeof(string))
                            {
                                // Insert the text into the SharedStringTablePart.
                                int index = InsertSharedStringItem(cel as string, shareStringPart);

                                // Insert cell into the new worksheet.
                                Cell cell = InsertCellInWorksheet(ColNameFor(colid), rowid, worksheetPart);

                                // Set the value of cell
                                cell.CellValue = new CellValue(index.ToString());
                                cell.DataType = new EnumValue<CellValues>(CellValues.SharedString);
                            }
                            else if (t == typeof(decimal))
                            {
                                // Insert cell into the new worksheet.
                                Cell cell = InsertCellInWorksheet(ColNameFor(colid), rowid, worksheetPart);

                                cell.CellValue = new CellValue((decimal)cel);
                                cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                            }
                            else if (t == typeof(Int32))
                            {
                                // Insert cell into the new worksheet.
                                Cell cell = InsertCellInWorksheet(ColNameFor(colid), rowid, worksheetPart);

                                cell.CellValue = new CellValue((Int32)cel);
                                cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                            }
                            else if (t == typeof(DateTime))
                            {
                                // Insert cell into the new worksheet.
                                Cell cell = InsertCellInWorksheet(ColNameFor(colid), rowid, worksheetPart);

                                cell.CellValue = new CellValue((DateTime)cel);
                                cell.DataType = new EnumValue<CellValues>(CellValues.Date);
                            }
                            else if (t == typeof(Boolean))
                            {
                                // Insert cell into the new worksheet.
                                Cell cell = InsertCellInWorksheet(ColNameFor(colid), rowid, worksheetPart);

                                cell.CellValue = new CellValue((Boolean)cel);
                                cell.DataType = new EnumValue<CellValues>(CellValues.Boolean);
                            }
                            // else leave it alone?
                        }

                        ++colid;
                    }
                    ++rowid;
                }

                // Save the new worksheet.
                worksheetPart.Worksheet.Save();
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
        private static int InsertSharedStringItem(string text, SharedStringTablePart shareStringPart)
        {
            // If the part does not contain a SharedStringTable, create one.
            if (shareStringPart.SharedStringTable == null)
            {
                shareStringPart.SharedStringTable = new SharedStringTable();
            }

            int i = 0;

            // Iterate through all the items in the SharedStringTable. If the text already exists, return its index.
            foreach (SharedStringItem item in shareStringPart.SharedStringTable.Elements<SharedStringItem>())
            {
                if (item.InnerText == text)
                {
                    return i;
                }

                i++;
            }

            // The text does not exist in the part. Create the SharedStringItem and return its index.
            shareStringPart.SharedStringTable.AppendChild(new SharedStringItem(new DocumentFormat.OpenXml.Spreadsheet.Text(text)));
            shareStringPart.SharedStringTable.Save();

            return i;
        }

        // Given a WorkbookPart, inserts a new worksheet.
        private static WorksheetPart InsertWorksheet(WorkbookPart workbookPart, string sheetName)
        {
            // Add a new worksheet part to the workbook.
            WorksheetPart newWorksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            newWorksheetPart.Worksheet = new Worksheet(new SheetData());

            // Add Sheets to the Workbook.
            Sheets sheets = workbookPart.Workbook.
                AppendChild<Sheets>(new Sheets());

            newWorksheetPart.Worksheet.Save();

            string relationshipId = workbookPart.GetIdOfPart(newWorksheetPart);

            // Get a unique ID for the new sheet.
            uint sheetId = 1;
            if (sheets.Elements<Sheet>().Count() > 0)
            {
                sheetId = sheets.Elements<Sheet>().Select(s => s.SheetId.Value).Max() + 1;
            }

            // Append the new worksheet and associate it with the workbook.
            Sheet sheet = new Sheet() { Id = relationshipId, SheetId = sheetId, Name = sheetName };
            sheets.Append(sheet);
            workbookPart.Workbook.Save();

            return newWorksheetPart;
        }

        // Given a column name, a row index, and a WorksheetPart, inserts a cell into the worksheet. 
        // If the cell already exists, returns it. 
        private static Cell InsertCellInWorksheet(string columnName, uint rowIndex, WorksheetPart worksheetPart)
        {
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();
            string cellReference = columnName + rowIndex;

            // If the worksheet does not contain a row with the specified row index, insert one.
            Row row;
            if (sheetData.Elements<Row>().Where(r => r.RowIndex == rowIndex).Count() != 0)
            {
                row = sheetData.Elements<Row>().Where(r => r.RowIndex == rowIndex).First();
            }
            else
            {
                row = new Row() { RowIndex = rowIndex };
                sheetData.Append(row);
            }

            // If there is not a cell with the specified column name, insert one.  
            if (row.Elements<Cell>().Where(c => c.CellReference.Value == columnName + rowIndex).Count() > 0)
            {
                return row.Elements<Cell>().Where(c => c.CellReference.Value == cellReference).First();
            }
            else
            {
                // Cells must be in sequential order according to CellReference. Determine where to insert the new cell.
                Cell refCell = null;
                foreach (Cell cell in row.Elements<Cell>())
                {
                    if (string.Compare(cell.CellReference.Value, cellReference, true) > 0)
                    {
                        refCell = cell;
                        break;
                    }
                }

                Cell newCell = new Cell() { CellReference = cellReference };
                row.InsertBefore(newCell, refCell);

                worksheet.Save();
                return newCell;
            }
        }
        #endregion

    }
}
