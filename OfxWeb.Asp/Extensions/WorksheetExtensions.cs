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
        public static void ExtractInto<T>(this ExcelWorksheet worksheet, ICollection<T> result) where T : new()
        {
            var cols = new List<String>();

            // Read headers
            for (int col = 1; col <= worksheet.Dimension.Columns; col++)
            {
                cols.Add(worksheet.Cells[1, col].Text);
            }

            var columns = new Dictionary<string, int>();
            foreach (var property in typeof(T).GetProperties())
            {
                if (cols.Contains(property.Name))
                    columns[property.Name] = 1 + cols.IndexOf(property.Name);
            }

            // Read rows
            for (int row = 2; row <= worksheet.Dimension.Rows; row++)
            {
                var item = new T();

                try
                {
                    foreach (var property in typeof(T).GetProperties())
                    {
                        if (cols.Contains(property.Name))
                        {
                            var col = columns[property.Name];

                            if (property.PropertyType == typeof(DateTime))
                            {
                                // Fix #767: Sometimes datetimes are datetimes, othertimes they're doubles
                                DateTime value = DateTime.MinValue;
                                var xlsvalue = worksheet.Cells[row, col].Value;
                                var type = xlsvalue.GetType();
                                if (type == typeof(DateTime))
                                    value = (DateTime)xlsvalue;
                                else if (type == typeof(Double))
                                    value = new DateTime(1900, 1, 1) + TimeSpan.FromDays((Double)xlsvalue - 2.0);
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
                catch (Exception)
                {
                    // Generally if there is an exception creating an item, we will just move onto the next row and ignore it
                }
            }
        }

        public static void PopulateFrom<T>(this ExcelWorksheet worksheet, ICollection<T> source, out int rows, out int cols) where T : class
        {
            // First add the headers

            var properties = typeof(T).GetProperties();
            int col = 1;
            foreach (var property in properties)
            {
                worksheet.Cells[1, col].Value = property.Name;
                ++col;
            }

            // Add values

            int row = 2;
            foreach (var item in source)
            {
                col = 1;
                foreach (var property in properties)
                {
                    worksheet.Cells[row, col].Value = property.GetValue(item);

                    if (property.PropertyType == typeof(DateTime))
                    {
                        worksheet.Cells[row, col].Style.Numberformat.Format = "m/d/yyyy";
                    }
                    else if (property.PropertyType == typeof(decimal))
                    {
                        worksheet.Cells[row, col].Style.Numberformat.Format = "$#,##0.00";
                    }
                    ++col;
                }
                ++row;
            }

            // AutoFitColumns
            worksheet.Cells[1, 1, row, col].AutoFitColumns();

            // Result
            rows = row - 1;
            cols = col - 1;
        }
    }
}
