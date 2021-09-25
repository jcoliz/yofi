using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace YoFi.AspNet.Models
{
    /// <summary>
    /// A mapping from one categorization scheme to another
    /// </summary>
    /// <remarks>
    /// This class is deprecated and will be removed in the future
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public class CategoryMap
    {
        /// <summary>
        /// Object identity in Entity Framework
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Categorization of the existing item to match
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Second-level categorization of the existing item to match
        /// </summary>
        public string SubCategory { get; set; }

        /// <summary>
        /// First-level resulting category
        /// </summary>
        /// <remarks>
        /// After an object has been remapped, the category will consist
        /// of all the Key1-4 items from here separated by colons.
        /// </remarks>
        public string Key1 { get; set; }

        /// <summary>
        /// Second-level resulting category
        /// </summary>
        public string Key2 { get; set; }

        /// <summary>
        /// Third-level resulting category
        /// </summary>
        public string Key3 { get; set; }
    }
}
