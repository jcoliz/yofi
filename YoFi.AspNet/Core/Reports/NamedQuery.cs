using YoFi.AspNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.AspNet.Controllers.Reports
{
    /// <summary>
    /// A query of reportable items, with an optional name
    /// </summary>
    public class NamedQuery
    {
        /// <summary>
        /// Query of reportable items
        /// </summary>
        public IQueryable<IReportable> Query { get; set; }
        /// <summary>
        /// Optional name for the query
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Whether this query should return leaf rows only, meaning that the report containing this query
        /// should not have totalling rows above it the way most reports do
        /// </summary>
        /// <remarks>
        /// Leaf rows only reports are useful for machine-readable reports
        /// </remarks>
        public bool LeafRowsOnly { get; set; } = false;

        /// <summary>
        /// Return a new query, which is the same as this query, but instead has the given <paramref name="newname"/>
        /// </summary>
        /// <param name="newname">New name for the resulting query</param>
        /// <returns>New query with the given <paramref name="newname"/></returns>
        public NamedQuery Labeled(string newname) => new NamedQuery() { Name = newname, Query = Query, LeafRowsOnly = LeafRowsOnly };

        /// <summary>
        /// Return a new query, which is the same as this query, but instead has the given <paramref name="leafrows"/>
        /// property.
        /// </summary>
        /// <param name="leafrows">New value for LeafRowsOnly property</param>
        /// <returns>New query with the given <paramref name="leafrows"/></returns>
        public NamedQuery AsLeafRowsOnly(bool leafrows) => new NamedQuery() { Name = Name, Query = Query, LeafRowsOnly = leafrows };
    }
}
