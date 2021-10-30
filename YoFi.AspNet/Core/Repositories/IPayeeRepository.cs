using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;

namespace YoFi.Core.Repositories
{
    public interface IPayeeRepository: IRepository<Payee>
    {
        Task BulkEdit(string category);
        Task BulkDelete();
        Task<Payee> NewFromTransaction(int txid);
    }
}
