using Common.DotNet;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;

namespace YoFi.Core.Repositories;

public class ReceiptRepositoryInDb : IReceiptRepository
{
    #region Fields

    private readonly IDataProvider _context;
    private readonly ITransactionRepository _txrepo;
    private readonly IStorageService _storage;
    private readonly IClock _clock;
    public const string Prefix = "r/";
    #endregion

    #region Constructor

    public ReceiptRepositoryInDb(IDataProvider context, ITransactionRepository txrepo, IStorageService storage, IClock clock)
    {
        _context = context;
        _txrepo = txrepo;
        _storage = storage;
        _clock = clock;
    }

    #endregion

    #region Public Interface


    /// <summary>
    /// Create a transaction, if possible based on this receipt, else just a blank transaction
    /// </summary>
    /// <param name="id">ID</param>
    /// <returns>New transaction</returns>
    public async Task<Transaction> CreateTransactionAsync(int? id)
    {
        if (id.HasValue && await TestExistsByIdAsync(id.Value))
        {
            var r = await GetByIdAsync(id.Value);
            var tx = r.AsTransaction();

            return tx;
        }
        else
        {
            return await _txrepo.CreateAsync();
        }
    }

    /// <summary>
    /// Assigned all receipts to their matching transaction, only if the receipt
    /// matches just a single transaction
    /// </summary>
    /// <returns>The number of matched receipts</returns>

    public async Task<int> AssignAll()
    {
        var result = 0;
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

    /// <summary>
    /// Assign the given <paramref name="receipt"/> to the given <paramref name="tx"/>
    /// </summary>
    /// <remarks>
    /// Note that this also removes it from the repository, as its now owned by the transaction
    /// </remarks>

    public async Task AssignReceipt(Receipt receipt, Transaction tx)
    {
        // Bug 1554: [Production Bug]: 400 Bad Request when matching receipts from Edit
        //
        // It should be supported to match a receipt that doesn't actually have ANY matching
        // attributes.
#if false
        // Ensure that the transaction and receipt at least somewhat match
        var match = receipt.MatchesTransaction(tx);
        if (match <= 0)
            throw new ArgumentException("Receipt and transaction do not match");
#endif
        // Add receipt to the transaction
        tx.ReceiptUrl = $"{Prefix}{receipt.ID}";

        // Copy over the memo, if exists
        if (!string.IsNullOrEmpty(receipt.Memo))
            tx.Memo = receipt.Memo;

        // Save the transaction
        await _txrepo.UpdateAsync(tx);

        // Remove it from our purview
        _context.Remove(receipt);
        await _context.SaveChangesAsync();
    }

    public async Task AssignReceipt(int id, int txid)
    {
        var receipt = await GetByIdAsync(id);
        var tx = await _txrepo.GetByIdAsync(txid);
        await AssignReceipt(receipt, tx);
    }

    /// <summary>
    /// Remove the specified <paramref name="receipt"/> from the system
    /// </summary>
    /// <param name="receipt">Which receipt to remove</param>
    /// <returns></returns>
    public async Task DeleteAsync(Receipt receipt)
    {
        var query = _context.Get<Receipt>().Where(x => x.ID == receipt.ID);
        if (await _context.AnyAsync(query))
            _context.Remove(receipt);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Retrieve all receipts waiting for matches
    /// </summary>
    /// <remarks>
    /// Note that this does tranaction matching here, and will fill in all the matching transactions
    /// </remarks>
    public async Task<IEnumerable<Receipt>> GetAllAsync()
    {
        // Get the receipts from the DB

        var receipts = await _context.ToListNoTrackingAsync(_context.Get<Receipt>().OrderByDescending(x => x.Timestamp).ThenBy(x => x.Name).ThenByDescending(x => x.Amount)) as IEnumerable<Receipt>;

        // Match transactions for each

        // Narrow down the universe of possible transactions
        if (receipts.Any())
        {
            var query = Receipt.TransactionsForReceipts(_txrepo.All, receipts);
            var txs = await _context.ToListNoTrackingAsync(query);

            foreach (var receipt in receipts)
                receipt.Matches = txs
                                    .Select(t => (quality: receipt.MatchesTransaction(t), t))
                                    .Where(x => x.quality > 0)
                                    .OrderByDescending(x => x.quality)
                                    .Select(x => x.t)
                                    .ToList();
        }

        return receipts;
    }

    /// <summary>
    /// Find all the receipts which match this <paramref name="transaction"/>
    /// </summary>
    /// <returns>Matching receipts ordered by better match first</returns>
    public async Task<ReceiptMatchResult> GetMatchingAsync(Transaction tx)
    {
        var any = await AnyAsync();
        var receipts = await _context.ToListNoTrackingAsync(_context.Get<Receipt>());
        var result = receipts
                .Select(r => (quality: r.MatchesTransaction(tx), r))
                .Where(x => x.quality > 0)
                .OrderByDescending(x => x.quality)
                .Select(x => x.r)
                .ToList();

        return new ReceiptMatchResult() { Any = any, Matches = result.Count, Suggested = result.FirstOrDefault() };
    }

    /// <summary>
    /// Retrieve all receipts waiting for matches, in order of how well it matches 
    /// this <paramref name="transaction"/>
    /// </summary>
    /// <remarks>
    /// Note that this DOES NOT fill in the "Matches" property
    /// </remarks>
    public async Task<IEnumerable<Receipt>> GetAllOrderByMatchAsync(Transaction tx)
    {
        // Get the receipts from the DB
        var receipts = await _context.ToListNoTrackingAsync(_context.Get<Receipt>());

        // Order by match level
        var result = receipts
                .Select(r => (quality: r.MatchesTransaction(tx), r))
                .OrderByDescending(x => x.quality)
                .ThenByDescending(x => x.r.Timestamp)
                .Select(x => x.r)
                .ToList();

        return result;
    }

    public async Task<IEnumerable<Receipt>> GetAllOrderByMatchAsync(int txid)
    {
        var tx = await _txrepo.GetByIdAsync(txid);
        var qresult = await GetAllOrderByMatchAsync(tx);

        return qresult;
    }

    /// <summary>
    /// Add a receipt to the system from <paramref name="stream"/>
    /// </summary>
    /// <param name="stream">Source of content</param>
    /// <param name="filename">Name of content</param>
    /// <param name="contenttype">Type of content</param>
    public async Task<Receipt> UploadReceiptAsync(string filename, Stream stream, string contenttype)
    {
        var item = Receipt.FromFilename(filename, _clock);
        _context.Add(item);
        await _context.SaveChangesAsync();
        await _storage.UploadBlobAsync($"{Prefix}{item.ID}", stream, contenttype);

        return item;
    }

    public async Task<Receipt> GetByIdAsync(int id)
    {
        var query = _context.Get<Receipt>().Where(x => x.ID == id);
        var list = await _context.ToListNoTrackingAsync(query);
        var result = list[0];

        // Now check for receipt matches
        var qt = Receipt.TransactionsForReceipts(_txrepo.All, new[] { result });
        var txs = await _context.ToListNoTrackingAsync(qt);
        result.Matches = txs
                            .Select(t => (quality: result.MatchesTransaction(t), t))
                            .Where(x => x.quality > 0)
                            .OrderByDescending(x => x.quality)
                            .Select(x => x.t)
                            .ToList();
        return result;
    }
    public Task<bool> TestExistsByIdAsync(int id) => _context.AnyAsync(_context.Get<Receipt>().Where(x => x.ID == id));

    public Task<bool> AnyAsync() => _context.AnyAsync(_context.Get<Receipt>());

#endregion
}
