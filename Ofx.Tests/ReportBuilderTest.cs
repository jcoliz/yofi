using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfxWeb.Asp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ofx.Tests
{
    [TestClass]
    public class ReportBuilderTest
    {
        [TestMethod]
        public void GenerateData()
        {
            // Generates a huge dataset
            const int numtx = 1000;

            var random = new Random();

            string[] categories = new string[] { "A", "A:B:C", "A:B:C:D", "E", "E:F", "E:F:G", "H", "H:I", "J", string.Empty };

            int i = numtx;
            while(i-- > 0)
            {
                var year = 2020;
                var month = random.Next(1, 12);
                var day = random.Next(1, DateTime.DaysInMonth(year,month));

                var tx = new Transaction() { Timestamp = new DateTime(year,month,day), Payee = i.ToString() };

                // Half the transactions will have splits
                if (random.Next(0,1) == 1)
                {
                    tx.Amount = ((decimal)random.Next(-1000000, 0)) / 100m;
                    tx.Category = categories[random.Next(0, categories.Length - 1)];
                }
                else
                {
                    tx.Splits = Enumerable.Range(0, random.Next(2, 7)).Select(x => new Split()
                    {
                        Amount = ((decimal)random.Next(-1000000, 0)) / 100m,
                        Category = categories[random.Next(0, categories.Length - 1)],
                        Memo = x.ToString()
                    })
                    .ToList();
                }

            }
        }
    }
}
