using System.Collections.Generic;
using YoFi.Core.Reports;

namespace YoFi.AspNet.Pages
{
    /// <summary>
    /// The information we need to display the common nav bar for reports
    /// </summary>
    public interface IReportNavbarViewModel
    {
        /// <summary>
        /// The parameters used to generate the currently-viewed report
        /// </summary>
        ReportParameters Parameters { get; }

        /// <summary>
        /// The set of all available report definitions
        /// </summary>
        IEnumerable<ReportDefinition> Definitions { get; }
    }
}
