using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Core.Models
{
    /// <summary>
    /// Any model item in our system needs to fulfill these things
    /// </summary>
    public interface IModelItem<T>: IID
    {
        /// <summary>
        /// Generate a query to place the <paramref name="original"/> items in the correct
        /// default order for this kind of item
        /// </summary>
        /// <param name="original"></param>
        IQueryable<T> InDefaultOrder(IQueryable<T> original);
    }
}
