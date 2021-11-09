using System.Collections.Generic;
using YoFi.Core.Reports;

namespace YoFi.AspNet.Pages
{
    /// <summary>
    /// The information we need to display the common nav bar for reports
    /// </summary>
    public class ReportNavbarViewModel
    {
        /// <summary>
        /// The parameters used to generate the currently-viewed report
        /// </summary>
        public ReportParameters Parameters { get; set; }

        /// <summary>
        /// The set of all available report definitions
        /// </summary>
        public IEnumerable<ReportDefinition> Definitions { get; set; }
    }
}
