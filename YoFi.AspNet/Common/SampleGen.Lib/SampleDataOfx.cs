using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Models;

namespace YoFi.SampleGen
{
    public static class SampleDataOfx
    {
        public static void WriteToOfx(IEnumerable<Transaction> transactions, Stream stream)
        {
            // Write header

            using var header = Common.NET.Data.SampleData.Open("ofx-header.txt");
            header.CopyTo(stream);

            // Write daterange

            /*
					<DTSTART>20170818
					<DTEND>20180602
             */
            var dtstart = transactions.Min(x => x.Timestamp);
            var dtend = transactions.Max(x => x.Timestamp);
            using var writer = new StreamWriter(stream);
            writer.WriteLine($"<DTSTART>{dtstart:yyyyMMdd}");
            writer.WriteLine($"<DTEND>{dtend:yyyyMMdd}");

            // Write each transaction

            /*
					<STMTTRN>
						<TRNTYPE>DEBIT
						<DTPOSTED>20180602000000.000[-08:PST]
						<TRNAMT>-49.68

						<FITID>20180602 469976 4,968 2,018,060,113,734
						<REFNUM>476365570
						<MEMO>Ext Credit Card Debit SAFEWAY FUEL 0490       BELLEVUE     WA USA
					</STMTTRN>
             */

            int id = 1;
            foreach (var tx in transactions)
            {
                writer.WriteLine("<STMTTRN>");
                if (tx.Amount < 0)
                    writer.WriteLine("<TRNTYPE>DEBIT");
                else
                    writer.WriteLine("<TRNTYPE>CREDIT");
                writer.WriteLine($"<DTPOSTED>{tx.Timestamp:yyyyMMdd000000.000}");
                writer.WriteLine($"<FITID>{tx.Timestamp:yyyyMMdd}{id++:D8}");
                writer.WriteLine($"<TRNAMT>{tx.Amount:F2}");
                writer.WriteLine($"<MEMO>{tx.Payee}");
                if (!string.IsNullOrEmpty(tx.BankReference))
                    writer.WriteLine($"<REFNUM>{tx.BankReference}");

                writer.WriteLine("</STMTTRN>");
            }


            // Write footer
            writer.Flush();
            using var footer = Common.NET.Data.SampleData.Open("ofx-footer.txt");
            footer.CopyTo(stream);
        }
    }
}
