using Common.DotNet;
using System.Collections.Generic;
using System.IO;
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
        /// <param name="tx">If null, will assign to the top match</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task AssignReceipt(Receipt receipt, Transaction tx = null)
        {
            throw new System.NotImplementedException();
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
        public Task<IEnumerable<Receipt>> GetAllAsync()
        {
            throw new System.NotImplementedException();
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
        public Task UploadReceiptAsync(Stream stream, string filename, string contenttype)
        {
            throw new System.NotImplementedException();
        }

        #endregion

    }
}
