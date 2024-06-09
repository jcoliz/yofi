using System.Collections.Generic;

namespace YoFi.Core.Reports;

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

    /// <summary>
    /// Build a summary of all report data on one page
    /// </summary>
    /// <param name="parameters">The parameters describing how to build it</param>
    /// <returns>The reports which together form the summary</returns>
    IEnumerable<IEnumerable<IDisplayReport>> BuildSummary(ReportParameters parameters);
}
