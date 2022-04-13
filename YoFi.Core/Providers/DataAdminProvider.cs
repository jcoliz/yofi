using Common.DotNet;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;

namespace YoFi.Core
{
    /// <summary>
    /// Providers administrator-driver services which operate on the
    /// whole of the data in single operations.
    /// </summary>
    public class DataAdminProvider : IDataAdminProvider
    {
        public const string TestMarker = "__test__";

        private readonly IDataProvider _context;
        private readonly IClock _clock;

        public DataAdminProvider(IDataProvider context, IClock clock)
        {
            _context = context;
            _clock = clock;
        }

        public async Task ClearDatabaseAsync(string id)
        {
            if ("budget" == id)
            {
                await _context.ClearAsync<BudgetTx>();
            }
            else if ("tx" == id)
            {
                await _context.ClearAsync<Transaction>();
            }
            else if ("payee" == id)
            {
                await _context.ClearAsync<Payee>();
            }
            else
            {
                // TODO: Return an error
            }
        }

        public async Task ClearTestDataAsync(string id)
        {
            if (id.Contains("receipt") || "all" == id)
                _context.RemoveRange(_context.Get<Receipt>().Where(x => x.Memo.Contains(TestMarker) || x.Name.Contains(TestMarker)));

            if (id.Contains("payee") || "all" == id)
                _context.RemoveRange(_context.Get<Payee>().Where(x => x.Category.Contains(TestMarker)));

            if (id.Contains("budgettx") || "all" == id)
                _context.RemoveRange(_context.Get<BudgetTx>().Where(x => x.Category.Contains(TestMarker)));

            if (id.Contains("trx") || "all" == id)
            {
                _context.RemoveRange(_context.Get<Transaction>().Where(x => x.Category.Contains(TestMarker) || x.Memo.Contains(TestMarker) || x.Payee.Contains(TestMarker)));
                _context.RemoveRange(_context.Get<Split>().Where(x => x.Category.Contains(TestMarker)));
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IDataStatus> GetDatabaseStatus()
        {
            var result = new DataStatus()
            {
                NumTransactions = await _context.CountAsync(_context.Get<Transaction>()),
                NumBudgetTxs = await _context.CountAsync(_context.Get<BudgetTx>()),
                NumPayees = await _context.CountAsync(_context.Get<Payee>()),
            };
            result.IsEmpty = result.NumTransactions == 0 && result.NumBudgetTxs == 0 && result.NumPayees == 0;

            return result;
        }

        public async Task UnhideTransactionsToToday()
        {
            var unhideme = _context.Get<Transaction>().Where(x => x.Timestamp <= _clock.Now && x.Hidden == true);
            var any = await _context.AnyAsync(unhideme);
            if (any)
            {
                foreach (var t in unhideme)
                    t.Hidden = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task SeedDemoSampleData(bool hiddenaftertoday, SampleData.ISampleDataProvider loader)
        {
            var status = await GetDatabaseStatus();
            if (status.IsEmpty)
                await loader.SeedAsync("all", hidden: hiddenaftertoday);

            if (hiddenaftertoday)
                await UnhideTransactionsToToday();
        }
    }

    public class DataStatus : IDataStatus
    {
        public bool IsEmpty { get; set; }

        public int NumTransactions { get; set; }

        public int NumBudgetTxs { get; set; }

        public int NumPayees { get; set; }
    }
}
