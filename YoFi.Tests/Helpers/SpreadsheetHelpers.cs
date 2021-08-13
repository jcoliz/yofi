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
        public SpreadsheetDocument Create(Stream stream)
        {
            var result = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);

            result.WorkbookPart.AddNewPart<WorksheetPart>();

            return result;
        }

        public Sheet AddSheet(SpreadsheetDocument doc,string name)
        {
            // https://docs.microsoft.com/en-us/office/open-xml/how-to-insert-a-new-worksheet-into-a-spreadsheet

            WorksheetPart newWorksheetPart = doc.WorkbookPart.GetPartsOfType<WorksheetPart>().Single();
            newWorksheetPart.Worksheet = new Worksheet(new SheetData());

            Sheets sheets = doc.WorkbookPart.Workbook.GetFirstChild<Sheets>();
            string relationshipId = doc.WorkbookPart.GetIdOfPart(newWorksheetPart);

            // Get a unique ID for the new worksheet.
            uint sheetId = sheets.Elements<Sheet>().Select(s => s.SheetId.Value).DefaultIfEmpty().Max() + 1;

            // Append the new worksheet and associate it with the workbook.
            Sheet sheet = new Sheet()
            { Id = relationshipId, SheetId = sheetId, Name = name };
            sheets.Append(sheet);

            return sheet;
        }

        public void AddItems<T>(Sheet sheet,IEnumerable<T> items)
        {
            // TODO
        }

        // Given a column name, a row index, and a WorksheetPart, inserts a cell into the worksheet. 
        // If the cell already exists, returns it. 
        private static Cell InsertCellInWorksheet(string columnName, uint rowIndex, WorksheetPart worksheetPart)
        {
            // https://docs.microsoft.com/en-us/office/open-xml/how-to-insert-text-into-a-cell-in-a-spreadsheet

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
                    if (cell.CellReference.Value.Length == cellReference.Length)
                    {
                        if (string.Compare(cell.CellReference.Value, cellReference, true) > 0)
                        {
                            refCell = cell;
                            break;
                        }
                    }
                }

                Cell newCell = new Cell() { CellReference = cellReference };
                row.InsertBefore(newCell, refCell);

                worksheet.Save();
                return newCell;
            }
        }
    }
}
