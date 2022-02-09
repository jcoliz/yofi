using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using YoFi.Core.Models;

namespace YoFi.Tests.Integration.Helpers
{
    public class SampleDataStore
    {
        public List<Transaction> Transactions { get; set; }

        public List<Payee> Payees { get; set; }

        public List<BudgetTx> BudgetTxs { get; set; }

        public async Task SerializeAsync(Stream stream)
        {
            await JsonSerializer.SerializeAsync<SampleDataStore>(stream, this);
        }

        public async Task DeSerializeAsync(Stream stream)
        {
            var incoming = await JsonSerializer.DeserializeAsync<SampleDataStore>(stream);

            Transactions = incoming.Transactions;
            Payees = incoming.Payees;
            BudgetTxs = incoming.BudgetTxs;
        }

        public static readonly string FileName = "FullSampleData.json";
        public static SampleDataStore Single = null;

        public static async Task LoadSingleAsync()
        {
            if (null == Single)
            {
                var stream = SampleData.Open(FileName);
                Single = await JsonSerializer.DeserializeAsync<SampleDataStore>(stream);
            }

            // Otherwise problems occur in back-to-back tests with same data
            foreach (var tx in Single.Transactions)
            {
                tx.ID = default;
                if (tx.HasSplits)
                    foreach (var s in tx.Splits)
                        s.ID = default;
            }
        }
    }
}
