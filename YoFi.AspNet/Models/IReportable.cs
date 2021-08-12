using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.AspNet.Models
{
    public interface IReportable
    {
        decimal Amount { get; }
        DateTime Timestamp { get; }
        string Category { get; }
    }

    public interface ISubReportable: IReportable
    {
        string SubCategory { get; }
    }
}
