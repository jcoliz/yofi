using System.IO;
using System.Threading.Tasks;
using YoFi.Core.Models;

namespace YoFi.Core.Repositories
{
    public interface ITransactionRepository: IRepository<Transaction>
    {
        public Task<Transaction> GetWithSplitsByIdAsync(int? id);
        Task<bool> AssignPayeeAsync(Transaction transaction);

        /// <summary>
        /// Change category of all selected items to <paramref name="category"/>
        /// </summary>
        /// <param name="category">Next category</param>
        Task BulkEdit(string category);
        Task<Stream> AsSpreadsheet(int year, bool allyears, string q);

        Task<int> AddSplitToAsync(int id);
        Task UploadReceiptAsync(Transaction transaction, Stream stream, string contenttype);
        Task<(Stream stream, string contenttype, string name)> GetReceiptAsync(Transaction transaction);

        Task CancelImportAsync();
    }
}
