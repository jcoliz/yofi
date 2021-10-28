using System.Collections.Generic;
using System.Linq;
using YoFi.Core.Models;
using YoFi.Core.Reports;

namespace YoFi.Tests.Helpers
{
    /// <summary>
    /// Holds the data in a form that a Report can be built from it
    /// </summary>
    /// <remarks>
    /// The idea is that we contain some number of series, where each series is
    /// an IQueryable(IReportable), and optionally has a name. (Use string.empty
    /// if no name).
    /// Multiple series can use the same name, in which case they are all aggregated
    /// into the subtotal column for that name.
    /// </remarks>
    public class NamedQueryList : List<NamedQuery>
    {
        public NamedQueryList() 
        { 
        }

        public NamedQueryList(IQueryable<IReportable> single)
        {
            Add(new NamedQuery() { Query = single });
        }
        
        public NamedQueryList(IEnumerable<NamedQuery> items)
        {
            AddRange(items);
        }

        public NamedQueryList(params NamedQueryList[] many)
        {
            foreach (var q in many)
                AddRange(q);
        }
        public NamedQueryList(params NamedQuery[] many)
        {
            AddRange(many);
        }

        public NamedQueryList Labeled(string label)
        {
            return new NamedQueryList(this.Select(x => new NamedQuery() { Name = label, Query = x.Query }));
        }

        public void Add(string key, IQueryable<IReportable> value)
        {
            Add(new NamedQuery() { Name = key, Query = value });
        }
    }
}
