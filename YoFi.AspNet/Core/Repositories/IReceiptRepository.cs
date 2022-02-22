using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using YoFi.Core.Models;

namespace YoFi.AspNet.Core.Repositories
{
    public interface IReceiptRepository
    {
        Task UploadReceiptAsync(Stream stream, string filename, string contenttype);
        Task<IEnumerable<Receipt>> GetAllAsync();
        Task<IEnumerable<Receipt>> GetMatchingAsync(Transaction tx);
        Task DeleteAsync(string filename);
        Task AssignReceipt(Receipt receipt, Transaction tx = null);
    }
}
