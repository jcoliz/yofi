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

        public static readonly string FileName = "FullSampleData.json";
        public static SampleDataStore Single = null;

        public static async Task LoadFullAsync()
        {
            var stream = SampleData.Open(FileName);
            Single = await JsonSerializer.DeserializeAsync<SampleDataStore>(stream);
        }

        public static async Task LoadPartialAsync()
        {
            Single = new SampleDataStore();
            await Single.LoadTransactions();
            await Single.LoadSampleBudgetTxs();
            await Single.LoadSampleManagedBudgetTxs();
        }

        protected async Task LoadTransactions()
        {
            if (Transactions is null)
            {
                using (var stream = SampleData.Open("Transactions1000.json"))
                Transactions = await JsonSerializer.DeserializeAsync<List<Transaction>>(stream);
            }
        }

        protected async Task LoadSampleBudgetTxs()
        {
            if (BudgetTxs is null)
            {
                using (var stream = SampleData.Open("BudgetTxs.json"))
                    BudgetTxs = await JsonSerializer.DeserializeAsync<List<BudgetTx>>(stream);
            }
        }

        protected async Task LoadSampleManagedBudgetTxs()
        {
            if (ManagedBudgetTxs is null)
            {
                using (var stream = SampleData.Open("BudgetTxsManaged.json"))
                    ManagedBudgetTxs = await JsonSerializer.DeserializeAsync<List<BudgetTx>>(stream);
            }
        }
    }
}
