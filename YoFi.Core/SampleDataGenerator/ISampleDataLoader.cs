using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace YoFi.Core.SampleData
{
    /// <summary>
    /// Provides a way for UI components to manage sample data in the system
    /// </summary>
    public interface ISampleDataLoader
    {
        /// <summary>
        /// Discover the known sample data offerings which can be seeded into
        /// the database
        /// </summary>
        /// <remarks>
        /// Notice the 'available' field. Some offerings are not available if there
        /// is already overlapping data, yet this will return them anyway, but set
        /// the 'available' property to false.
        /// </remarks>
        /// <returns>All known seed offerings</returns>
        Task<IEnumerable<ISampleDataSeedOffering>> ListSeedOfferingsAsync();

        /// <summary>
        /// Add sample data to the runtime database
        /// </summary>
        /// <param name="id">Unique identifier for which offering to seed</param>
        /// <param name="hidden">Whether to hide the transactions. Useful if you want
        /// to reveal them over time</param>
        /// <returns>Result message described what happened</returns>
        Task<string> SeedAsync(string id, bool hidden = false);

        /// <summary>
        /// Discover the available sample data download offerings
        /// </summary>
        /// <returns>All known offerings</returns>
        Task<IEnumerable<ISampleDataDownloadOffering>> ListDownloadOfferingsAsync();

        /// <summary>
        /// Retrieve a sample data download offering
        /// </summary>
        /// <param name="id">Unique identifier for which offering to download</param>
        /// <returns>Stream containing the desired file</returns>
        Task<Stream> DownloadSampleDataAsync(string id);
    }

    public enum SampleDataSeedOfferingCondition { Always = 0, Empty, MoreTransactionsReady, NoTransactions, NoBudgetTxs, NoPayees };

    /// <summary>
    /// Describes a set of seed data which user could choose to apply to the
    /// database
    /// </summary>
    public interface ISampleDataSeedOffering
    {
        /// <summary>
        /// Unique identifier for this offering
        /// </summary>
        string ID { get; }
        /// <summary>
        /// Short title for a heading
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Longer description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Whether this offering can be seeded, given the current state of the DB
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Describes the features of this seeding offering
        /// </summary>
        /// <remarks>
        /// Each rule gives one kind of sample data that's included in this seeding
        /// </remarks>
        IEnumerable<string> Rules { get; }
    }

    public enum SampleDataDownloadOfferingKind { None = 0, Primary, Monthly }

    public enum SampleDataDownloadFileType { None = 0, XLSX, OFX }

    /// <summary>
    /// Describes a set of sample data which the user could choose to download
    /// </summary>
    public interface ISampleDataDownloadOffering
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        string ID { get; }

        /// <summary>
        /// Extension of the downloaded file
        /// </summary>
        SampleDataDownloadFileType FileType { get; }

        /// <summary>
        /// Multi-line description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// What kind of data it is
        /// </summary>
        /// <remarks>
        /// Useful for visual grouping
        /// </remarks>
        SampleDataDownloadOfferingKind Kind { get; }
    }

    /// <summary>
    /// Necessary configuration for SampleDataLoader
    /// </summary>
    public interface ISampleDataConfiguration
    {
        /// <summary>
        /// Directory where the sample data files are stored
        /// </summary>
        string Directory { get; }
    }
}
