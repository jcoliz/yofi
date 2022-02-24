using Common.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.Core.Repositories
{
    /// <summary>
    /// Contains a queue of unmatched transactions, awaiting a transaction to be matched with
    /// </summary>
    /// <remarks>
    /// Note that currently, we use azure storage as the source of truth. THe receipts
    /// are not stored to the DB, so no IDataSource needed. Not sure if that's the right move.
    /// </remarks>
    public class ReceiptRepository : IReceiptRepository
    {
        #region Fields

        private readonly ITransactionRepository _txrepo;
        private readonly IStorageService _storage;
        private readonly IClock _clock;

        #endregion

        #region Constructor

        public ReceiptRepository(ITransactionRepository txrepo, IStorageService storage, IClock clock)
        {
            _txrepo = txrepo;
            _storage = storage;
            _clock = clock;
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Assign the give <paramref name="receipt"/> to the given <paramref name="tx"/>
        /// </summary>
        /// <remarks>
        /// Note that this also removes it from the repository, as its now owned by the transaction
        /// </remarks>
        /// <param name="receipt"></param>
        /// <param name="tx"></param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
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
        }

        /// <summary>
        /// Assigned all receipts to their matching transaction, only if the receipt
        /// matches just a single transaction
        /// </summary>
        /// <returns>The number of matched receipts</returns>
        public async Task<int> AssignAll()
        {
            int result = 0;
            var receipts = await GetAllAsync();
            foreach(var receipt in receipts)
            {
                // If this receipt has ONLY ONE match
                if (receipt.Matches.Any() && ! receipt.Matches.Skip(1).Any())
                {
                    var tx = receipt.Matches.Single();
                    await AssignReceipt(receipt, tx);
                    ++result;
                }
            }

            return result;
        }

        /// <summary>
        /// Remove the specified receipt from the system
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task DeleteAsync(Receipt receipt)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Retrieve all receipts waiting for matches
        /// </summary>
        /// <remarks>
        /// Note that this does tranaction matching here, and will fill in all the matching transactions
        /// </remarks>
        /// <returns></returns>
        public async Task<IEnumerable<Receipt>> GetAllAsync()
        {
            var filenames = await _storage.GetBlobNamesAsync("receipt/");
            var result = filenames.Select(x => Receipt.FromFilename(x[8..],_clock)).ToList();

            // Now, need to match transactions for each

            // Narrow down the universe of possible transactions
            var txs = Receipt.TransactionsForReceipts(_txrepo.All, result).ToList();
            // TODO: ToListAsync();

            foreach(var receipt in result)
            {
                var m = txs
                        .Select(t => (receipt.MatchesTransaction(t), t));

                receipt.Matches = txs
                                    .Select(t => (receipt.MatchesTransaction(t),t))
                                    .Where(x=>x.Item1 > 0)
                                    .OrderByDescending(x=>x.Item1)
                                    .Select(x=>x.t)
                                    .ToList();
            }

            return result;
        }

        /// <summary>
        /// Find all the receipts which match this transaction
        /// </summary>
        /// <param name="tx"></param>
        /// <returns>Matching receipts ordered by better match</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task<IEnumerable<Receipt>> GetMatchingAsync(Transaction tx)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Add a receipt to the system
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="filename"></param>
        /// <param name="contenttype"></param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public async Task UploadReceiptAsync(string filename, Stream stream, string contenttype)
        {
            await _storage.UploadBlobAsync("receipt/" + filename, stream, contenttype);
        }

        #endregion

    }
}
