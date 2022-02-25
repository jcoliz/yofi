using Common.DotNet;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;

namespace YoFi.Core.Repositories
{
    public class ReceiptRepositoryInDb : IReceiptRepository
    {
        #region Fields

        private readonly IDataContext _context;
        private readonly ITransactionRepository _txrepo;
        private readonly IStorageService _storage;
        private readonly IClock _clock;

        #endregion

        #region Constructor

        public ReceiptRepositoryInDb(IDataContext context, ITransactionRepository txrepo, IStorageService storage, IClock clock)
        {
            _context = context;
            _txrepo = txrepo;
            _storage = storage;
            _clock = clock;
        }

        public async Task<int> AssignAll()
        {
            int result = 0;
            var receipts = await GetAllAsync();
            foreach (var receipt in receipts)
            {
                // If this receipt has ONLY ONE match
                if (receipt.Matches.Any() && !receipt.Matches.Skip(1).Any())
                {
                    var tx = receipt.Matches.Single();
                    await AssignReceipt(receipt, tx);
                    ++result;
                }
            }

            return result;
        }

        #endregion

        public async Task AssignReceipt(Receipt receipt, Transaction tx)
        {
            // Which transaction will own the receipt now?
            // Get the receipt
            var stream = new MemoryStream();
            var contenttype = await _storage.DownloadBlobAsync("receipt/" + receipt.Filename, stream);
            stream.Seek(0, SeekOrigin.Begin);

            // Add to the transaction
            await _txrepo.UploadReceiptAsync(tx, stream, contenttype);

            // Remove it from our purview
            await _storage.RemoveBlobAsync("receipt/" + receipt.Filename);
            _context.Remove(receipt);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Receipt receipt)
        {
            var hasitem = await _context.AnyAsync(_context.Get<Receipt>().Where(x => x.ID == receipt.ID));
            if (hasitem)
            {
                _context.Remove(receipt);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Receipt>> GetAllAsync()
        {
            var receipts = await _context.ToListNoTrackingAsync(_context.Get<Receipt>()) as IEnumerable<Receipt>;

            // Now, need to match transactions for each

            // Narrow down the universe of possible transactions
            var txs = await _context.ToListNoTrackingAsync( Receipt.TransactionsForReceipts(_txrepo.All, receipts) );

            foreach (var receipt in receipts)
            {
                var m = txs
                        .Select(t => (receipt.MatchesTransaction(t), t));

                receipt.Matches = txs
                                    .Select(t => (receipt.MatchesTransaction(t), t))
                                    .Where(x => x.Item1 > 0)
                                    .OrderByDescending(x => x.Item1)
                                    .Select(x => x.t)
                                    .ToList();
            }

            return receipts;
        }

        public async Task<IEnumerable<Receipt>> GetMatchingAsync(Transaction tx)
        {
            var receipts = await _context.ToListNoTrackingAsync(_context.Get<Receipt>()) as IEnumerable<Receipt>;

            var result = receipts
                    .Select(r => (r.MatchesTransaction(tx), r))
                    .Where(x => x.Item1 > 0)
                    .OrderByDescending(x => x.Item1)
                    .Select(x => x.r)
                    .ToList();

            return result;
        }

        public async Task UploadReceiptAsync(string filename, Stream stream, string contenttype)
        {
            await _storage.UploadBlobAsync("receipt/" + filename, stream, contenttype);
            var item = Receipt.FromFilename(filename,_clock);
            _context.Add(item);
        }
    }
}
