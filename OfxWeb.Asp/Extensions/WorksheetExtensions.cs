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
            for (int col = 1; col <= worksheet.Dimension.Columns; col++)
            {
                cols.Add(worksheet.Cells[1, col].Text);
            }

            var columns = new Dictionary<string, int>();
            foreach(var property in typeof(T).GetProperties())
            {
                if (cols.Contains(property.Name))
                    columns[property.Name] = 1 + cols.IndexOf(property.Name);
            }

            // Read rows
            for (int row = 2; row <= worksheet.Dimension.Rows; row++)
            {
                var item = new T();

                foreach (var property in typeof(T).GetProperties())
                {
                    if (cols.Contains(property.Name))
                    {
                        var col = columns[property.Name];

                        if (property.PropertyType == typeof(DateTime))
                        {
                            var value = (DateTime)worksheet.Cells[row, col].Value;
                            property.SetValue(item, value);
                        }
                        else if (property.PropertyType == typeof(decimal))
                        {
                            var value = Convert.ToDecimal((double)worksheet.Cells[row, col].Value);
                            property.SetValue(item, value);
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
        }
    }
}
