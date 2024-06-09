namespace YoFi.Core.Reports;

/// <summary>
/// Parameters used to build a report at one moment in time
/// </summary>
/// <remarks>
/// Moved these into a class so I can make a single change in calling convention here
/// and have it propagate out to the controller endpoints automatically
/// </remarks>
public class ReportParameters
{
    /// <summary>
    /// The url slug, or short name, of the report we want
    /// </summary>
    public string slug { get; set; }

    /// <summary>
    /// Optionally set the constraint year, else will use current year
    /// </summary>
    public int? year { get; set; }

    /// <summary>
    /// Optionally set the ending month, else will report on data from
    /// current year through current month for this year, or if a 
    /// previous year, then through the end of that year
    /// </summary>
    public int? month { get; set; }

    /// <summary>
    /// Optionally whether to show month columns, else will use the default for
    /// the given report id.
    /// </summary>
    public bool? showmonths { get; set; }

    /// <summary>
    /// Optionally how many levels deep to show, else will use the dafault for
    /// the given report id
    /// </summary>
    public int? level { get; set; }
}
