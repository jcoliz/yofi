using OfxWeb.Asp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Controllers.Reports
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
    public class Query : List<KeyValuePair<string, IQueryable<IReportable>>>
    {
        public Query() 
        { 
        }
        
        public Query(IQueryable<IReportable> single)
        {
            Add(new KeyValuePair<string, IQueryable<IReportable>>(string.Empty, single));
        }
        
        public Query(IEnumerable<KeyValuePair<string, IQueryable<IReportable>>> items)
        {
            AddRange(items);
        }

        public Query(params Query[] many)
        {
            foreach (var q in many)
                AddRange(q);
        }

        public Query Labeled(string label)
        {
            return new Query(this.Select(x => new KeyValuePair<string, IQueryable<IReportable>>(label, x.Value)));
        }

        public void Add(string key, IQueryable<IReportable> value)
        {
            Add(new KeyValuePair<string, IQueryable<IReportable>>(key, value));
        }

    }
}
