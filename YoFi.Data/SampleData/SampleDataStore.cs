using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using YoFi.Core;
using YoFi.Core.Models;

namespace YoFi.Data.SampleData
{
    public class SampleDataStore
    {
        public List<Transaction> Transactions { get; set; }

        public List<Payee> Payees { get; set; }

        public List<BudgetTx> BudgetTxs { get; set; }

        public List<BudgetTx> ManagedBudgetTxs { get; set; }

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

        public static readonly string FileName = "SampleData-2022-Full.json";
        public static SampleDataStore Single = null;

        public static async Task SeedFullAsync(IDataProvider context)
        {
            var any = await context.AnyAsync(context.Get<Transaction>());
            if (!any)
            {
                await LoadFullAsync();

                // Just seed transactions for now!
                context.AddRange(Single.Transactions);
                await context.SaveChangesAsync();
            }
        }

        public static async Task LoadFullAsync()
        {
            var stream = SampleData.Open(FileName);
            Single = await JsonSerializer.DeserializeAsync<SampleDataStore>(stream);

            // TODO: Fix this hack
            // The generated sample data should NOT have IDs in the JSON. IDs are needed for the XLSX
            // but not the JSON data
            foreach (var transaction in Single.Transactions.Where(x => x.ID != 0))
            {
                transaction.ID = 0;
                foreach (var split in transaction.Splits)
                    split.TransactionID = 0;
            }
        }
    }
}
