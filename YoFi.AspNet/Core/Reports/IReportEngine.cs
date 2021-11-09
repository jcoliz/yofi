using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Core.Reports
{
    /// <summary>
    /// UI-facing interface of the reports subsystem
    /// </summary>
    public interface IReportEngine
    {
        /// <summary>
        /// The report definitions known to the system
        /// </summary>
        IEnumerable<ReportDefinition> Definitions { get; }

        /// <summary>
        /// Build a particular report
        /// </summary>
        /// <param name="parameters">The parameters describing which report and how to build it</param>
        /// <returns>The build report</returns>
        Report Build(ReportParameters parameters);
    }
}
