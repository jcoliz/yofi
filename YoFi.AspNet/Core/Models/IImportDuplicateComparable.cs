using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Core.Models
{
    /// <summary>
    /// Logic for testing whether two imported items are duplicates
    /// </summary>
    public interface IImportDuplicateComparable
    {
        /// <summary>
        /// Get a hash code base on the uniqueness defined in ImportEquals
        /// </summary>
        /// <returns>The hash code</returns>
        int GetImportHashCode();

        /// <summary>
        /// Whether this object is equal to <paramref name="other"/> for the purposes
        /// of import. That is, whether the other object is a duplicate during import
        /// </summary>
        /// <param name="other"></param>
        /// <returns>'true' if is an import duplicate of <paramref name="other"/></returns>
        bool ImportEquals(object other);
    }
}
