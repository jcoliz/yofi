using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Models;

namespace YoFi.Core
{
    public interface IDataContext
    {
        IQueryable<Payee> ReadPayees { get; }
    }
}
