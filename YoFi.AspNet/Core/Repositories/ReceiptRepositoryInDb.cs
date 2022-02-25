using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using YoFi.Core.Models;

namespace YoFi.Core.Repositories
{
    public class ReceiptRepositoryInDb : IReceiptRepository
    {
        public Task AssignReceipt(Receipt receipt, Transaction tx)
        {
            throw new System.NotImplementedException();
        }

        public Task DeleteAsync(Receipt receipt)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<Receipt>> GetAllAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<Receipt>> GetMatchingAsync(Transaction tx)
        {
            throw new System.NotImplementedException();
        }

        public Task UploadReceiptAsync(string filename, Stream stream, string contenttype)
        {
            throw new System.NotImplementedException();
        }
    }
}
