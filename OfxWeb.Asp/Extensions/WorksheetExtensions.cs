using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace OfficeOpenXml
{
    public static class WorksheetExtensions
    {
        public static void ExtractInto<T>(this ExcelWorksheet worksheet, ICollection<T> result) where T: new()
        {
            var cols = new List<String>();

            // Read headers
            for (int i = 1; i <= worksheet.Dimension.Columns; i++)
            {
                cols.Add(worksheet.Cells[1, i].Text);
            }

            var columns = new Dictionary<string, int>();
            foreach(var property in typeof(T).GetProperties())
            {
                if (cols.Contains(property.Name))
                    columns[property.Name] = 1 + cols.IndexOf(property.Name);
            }

            // Read rows
            for (int i = 2; i <= worksheet.Dimension.Rows; i++)
            {
                var item = new T();

                foreach (var property in typeof(T).GetProperties())
                {
                    if (cols.Contains(property.Name))
                    {
                        var col = columns[property.Name];
                        var value = worksheet.Cells[i, col].Text.Trim();

                        if (!string.IsNullOrEmpty(value))
                        {
                            property.SetValue(item, value);
                        }
                    }
                }

                result.Add(item);
            }
        }
    }
}
