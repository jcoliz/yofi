using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Core.SampleGen
{
    /// <summary>
    /// Provides a way for UI components to manage sample data in the system
    /// </summary>
    public interface ISampleDataLoader
    {
        Task<IEnumerable<ISampleDataSeedOffering>> GetSeedOfferingsAsync();

        /// <summary>
        /// Add sample data to the runtime database
        /// </summary>
        /// <param name="id">Unique identifier for which offering to seed</param>
        /// <returns></returns>
        Task Seed(string id);
        Task<IEnumerable<ISampleDataDownloadOffering>> GetDownloadOfferings();
        Task<Stream> DownloadSampleData();
    }

    public interface ISampleDataSeedOffering
    {
        /// <summary>
        /// Unique identifier for this offering
        /// </summary>
        string ID { get; }
        string Title { get; }
        string Description { get; }
        bool Recommended { get; }
        /// <summary>
        /// Whether this offering can be seeded, given the current state of the DB
        /// </summary>
        bool Available { get; }
    }

    public enum SampleDataDownloadOfferingKind { None = 0, Primary, MonthlyXlsx, MonthlyOfx }

    public interface ISampleDataDownloadOffering
    {
        string ID { get; }
        IEnumerable<string> Title { get; }
        SampleDataDownloadOfferingKind Kind { get; }
    }

}
