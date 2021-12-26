using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Reports;

namespace YoFi.AspNet.Pages
{
    /// <summary>
    /// Defines a view model to show a report and chart together
    /// </summary>
    public interface IReportAndChartViewModel
    {
        /// <summary>
        /// The report to show
        /// </summary>
        public IDisplayReport Report { get; }

        /// <summary>
        /// The chart to show
        /// </summary>
        /// <remarks>
        /// Already serialized to Json
        /// </remarks>
        public string ChartJson { get; }

        /// <summary>
        /// Whether to show the chart to the side of the report
        /// </summary>
        public bool ShowSideChart { get; }

        /// <summary>
        /// Whether to show the chart above the report
        /// </summary>
        public bool ShowTopChart { get; }
    }
}
