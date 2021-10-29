using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Core.Models
{
    /// <summary>
    /// Any model item in our system needs to fulfil these things
    /// </summary>
    public interface IModelItem<T>: IID
    {
        /// <summary>
        /// Comparison to discover whether two of the same kind of item
        /// are duplicates during the import process
        /// </summary>
        IEqualityComparer<T> ImportDuplicateComparer { get; }

        IQueryable<T> InDefaultOrder(IQueryable<T> original);
    }
}
