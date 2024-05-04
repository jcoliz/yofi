using System;

namespace YoFi.Core.Models
{
    /// <summary>
    /// Fundamental item contained in a report
    /// </summary>
    /// <remarks>
    /// Reports are pretty generic about what they report on. This is the fundamental
    /// info we need for any given item in a report.
    /// 
    /// Inherit this interface in a class to signify that reports can be generated
    /// on that class.
    /// </remarks>
    public interface IReportable
    {
        /// <summary>
        /// How much money changed hands?
        /// </summary>
        decimal Amount { get; }

        /// <summary>
        /// When did this happen?
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// How should we categorize this event?
        /// </summary>
        string Category { get; }
    }
}
