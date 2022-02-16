using Common.NET.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfxSharp;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Transaction = YoFi.Core.Models.Transaction;

namespace YoFi.Tests.Database
{
    [TestClass]
    public class TransactionControllerTest
    {
        static IEnumerable<Transaction> TransactionItemsLong;

        // This is public in case someone ELSE wants a big boatload of transactions
        public static async Task<IEnumerable<Transaction>> GetTransactionItemsLong()
        {
            if (null == TransactionItemsLong)
            {
                using var stream = SampleData.Open("FullSampleData-Month02.ofx");
                OfxDocument Document = await OfxDocumentReader.FromSgmlFileAsync(stream);
                TransactionItemsLong = Document.Statements.SelectMany(x=>x.Transactions).Select(tx=> new Transaction() { Amount = tx.Amount, Payee = tx.Memo.Trim(), BankReference = tx.ReferenceNumber?.Trim(), Timestamp = tx.Date.Value.DateTime });
            }
            return TransactionItemsLong;
        }
    }
}
