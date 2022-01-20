using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace YoFi.Core.SampleGen
{
    /// <summary>
    /// Provides a way for UI components to manage sample data in the system
    /// </summary>
    public interface ISampleDataLoader
    {
        /// <summary>
        /// Discover the available sample data offerings which can be seeded into
        /// the database
        /// </summary>
        /// <remarks>
        /// Notice the 'available' field. Some offerings are not available if there
        /// is already overlapping data, yet this will return them anyway, but set
        /// the 'available' property to false.
        /// </remarks>
        /// <returns></returns>
        Task<IEnumerable<ISampleDataSeedOffering>> GetSeedOfferingsAsync();

        /// <summary>
        /// Add sample data to the runtime database
        /// </summary>
        /// <param name="id">Unique identifier for which offering to seed</param>
        /// <returns></returns>
        Task SeedAsync(string id);

        /// <summary>
        /// Discover the available sample data download offerings
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<ISampleDataDownloadOffering>> GetDownloadOfferingsAsync();

        /// <summary>
        /// Retrieve a sample data download offering
        /// </summary>
        /// <param name="id">Unique identifier for which offering to download</param>
        /// <returns></returns>
        Task<Stream> DownloadSampleDataAsync(string id);
    }

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
        /// Whether this is the recommended choice among the currently
        /// available offerings
        /// </summary>
        bool IsRecommended { get; }

        /// <summary>
        /// Whether this offering can be seeded, given the current state of the DB
        /// </summary>
        bool IsAvailable { get; }
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
        string FileType { get; }

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
}
