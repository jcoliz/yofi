using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.AspNet.Core.Repositories
{
    public interface ITransactionRepository: IRepository<Transaction>
    {
    }
}
