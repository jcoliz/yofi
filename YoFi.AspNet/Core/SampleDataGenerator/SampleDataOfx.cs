using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;

namespace YoFi.Core.SampleGen
{
    public static class SampleDataOfx
    {
        public static void WriteToOfx(IEnumerable<Transaction> transactions, Stream stream)
        {
            // Needs to be left open in case stream is a memory stream that we are going to use later
            var writer = new StreamWriter(stream);

            // Write header

            using var header = Common.NET.Data.SampleData.Open("ofx-header.txt");
            using var headerreader = new StreamReader(header);
            writer.Write(headerreader.ReadToEnd());

            // Write daterange

            /*
					<DTSTART>20170818
					<DTEND>20180602
             */
            var dtstart = transactions.Min(x => x.Timestamp);
            var dtend = transactions.Max(x => x.Timestamp);
            var tabs5 = "\t\t\t\t\t";
            writer.WriteLine($"{tabs5}<DTSTART>{dtstart:yyyyMMdd}");
            writer.WriteLine($"{tabs5}<DTEND>{dtend:yyyyMMdd}");

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
            var tabs6 = tabs5 + "\t";
            foreach (var tx in transactions)
            {
                writer.WriteLine($"{tabs5}<STMTTRN>");
                if (tx.Amount < 0)
                    writer.WriteLine($"{tabs6}<TRNTYPE>DEBIT");
                else
                    writer.WriteLine($"{tabs6}<TRNTYPE>CREDIT");
                writer.WriteLine($"{tabs6}<DTPOSTED>{tx.Timestamp:yyyyMMdd000000.000}");
                writer.WriteLine($"{tabs6}<FITID>{tx.Timestamp:yyyyMMdd}{id++:D8}");
                writer.WriteLine($"{tabs6}<TRNAMT>{tx.Amount:F2}");
                writer.WriteLine($"{tabs6}<MEMO>{tx.Payee}");
                if (!string.IsNullOrEmpty(tx.BankReference))
                    writer.WriteLine($"{tabs6}<REFNUM>{tx.BankReference}");

                writer.WriteLine($"{tabs5}</STMTTRN>");
            }


            // Write footer
            writer.Flush();
            using var footer = Common.NET.Data.SampleData.Open("ofx-footer.txt");
            using var footerreader = new StreamReader(footer);
            writer.Write(footerreader.ReadToEnd());
            writer.Flush();
        }
    }
}
