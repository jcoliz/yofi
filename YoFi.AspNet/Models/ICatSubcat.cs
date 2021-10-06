using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.AspNet.Models
{
    /// <summary>
    /// Object has a Category and SubCategory field
    /// </summary>
    /// <remarks>
    /// Used by the category remapper. If it has a cat and subcat,
    /// it can be remapped.
    /// </remarks>
    public interface ICatSubcat
    {
        /// <summary>
        /// Cagtegorization of this item
        /// </summary>
        /// <remarks>
        /// Separate successive levels of depth with a colon, e.g. "Housing:Mortgage"
        /// </remarks>
        string Category { get; set; }
    }
}
