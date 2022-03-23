using Common.DotNet;
using Common.DotNet.Data;
using jcoliz.OfficeOpenXml.Serializer;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using YoFi.Core.Models;

namespace YoFi.Core.SampleGen
{
    public class SampleDataLoader : ISampleDataLoader
    {
        private readonly IDataContext _context;
        private readonly IClock _clock;
        private readonly ISampleDataConfiguration _config;
 
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">Application data context</param>
        /// <param name="directory">Location of sample data file</param>
        public SampleDataLoader(IDataContext context, IClock clock, ISampleDataConfiguration config)
        {
            _context = context;
            _clock = clock;
            _config = config;
        }

        /// <summary>
        /// Retrieve a sample data download offering
        /// </summary>
        /// <param name="id">Unique identifier for which offering to download</param>
        /// <returns>Stream containing the desired file</returns>
        public Task<Stream> DownloadSampleDataAsync(string id)
        {
            Stream stream;

            var instream = File.OpenRead($"{_config.Directory}/SampleData-Full.xlsx");

            if ("full" == id)
            {
                // Just return it!
                stream = instream;
            }
            else if (nameof(BudgetTx) == id)
            {
                // Load in just the budget into memory
                using var ssr = new SpreadsheetReader();
                ssr.Open(instream);
                var items = ssr.Deserialize<BudgetTx>();

                // Then write that back out
                stream = new MemoryStream();
                using (var ssw = new SpreadsheetWriter())
                {
                    ssw.Open(stream);
                    ssw.Serialize(items);
                }
                stream.Seek(0, SeekOrigin.Begin);
            }
            else if (nameof(Payee) == id)
            {
                // Load in just the payees into memory
                using var ssr = new SpreadsheetReader();
                ssr.Open(instream);
                var items = ssr.Deserialize<Payee>();

                // Then write that back out
                stream = new MemoryStream();
                using (var ssw = new SpreadsheetWriter())
                {
                    ssw.Open(stream);
                    ssw.Serialize(items);
                }
                stream.Seek(0, SeekOrigin.Begin);
            }
            else
            {
                var split = id.Split('-');
                if (split.Length == 2 && int.TryParse(split[1], out var month))
                {
                    // At this point, only transactions are of interest
                    // Load in just the transactions and splits into memory
                    using var ssr = new SpreadsheetReader();
                    ssr.Open(instream);
                    var txs = ssr.Deserialize<Transaction>();
                    var splits = ssr.Deserialize<Split>();

                    // Narrow down to the required month
                    var outtxs = txs.Where(x => x.Timestamp.Month == month);
                    var outtxids = outtxs.Where(x => x.ID > 0).Select(x => x.ID).ToHashSet();
                    var outsplits = splits.Where(x => outtxids.Contains(x.TransactionID));

                    if (SampleDataDownloadFileType.XLSX == Enum.Parse<SampleDataDownloadFileType>(split[0]))
                    {
                        // Then write that back out
                        stream = new MemoryStream();
                        using (var ssw = new SpreadsheetWriter())
                        {
                            ssw.Open(stream);
                            ssw.Serialize(outtxs);
                            ssw.Serialize(outsplits);
                        }
                        stream.Seek(0, SeekOrigin.Begin);
                    }
                    else if (SampleDataDownloadFileType.OFX == Enum.Parse<SampleDataDownloadFileType>(split[0]))
                    {
                        // Write it as an OFX
                        stream = new MemoryStream();
                        SampleDataOfx.WriteToOfx(outtxs, stream);

                        stream.Seek(0, SeekOrigin.Begin);
                    }
                    else
                        throw new ApplicationException($"Not found sample data ID {id}");
                }
                else
                    throw new ApplicationException($"Not found sample data ID {id}");
            }

            return Task.FromResult(stream);
        }

        /// <summary>
        /// Discover the available sample data download offerings
        /// </summary>
        /// <returns>All known offerings</returns>
        public async Task<IEnumerable<ISampleDataDownloadOffering>> ListDownloadOfferingsAsync()
        {
            using var stream = Common.DotNet.Data.SampleData.Open("SampleDataDownloadOfferings.json");

            var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new JsonStringEnumConverter());
            var inputs = await JsonSerializer.DeserializeAsync<List<DownloadOffering>>(stream, options);

            var result = 
                inputs
                    .Where(x => x.Kind == SampleDataDownloadOfferingKind.Primary)
                    .Concat
                    (
                        inputs
                            .Where(x => x.Kind == SampleDataDownloadOfferingKind.Monthly)
                            .SelectMany(o => 
                                Enumerable.Range(1, 12)
                                .Select(m=> 
                                    new DownloadOffering() 
                                    { 
                                        FileType = o.FileType, 
                                        Kind = o.Kind,
                                        Description = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(m),
                                        ID = $"{o.FileType}-{m}"
                                    } 
                                )
                            )
                    )
                    .ToList();

            return result;
        }

        /// <summary>
        /// Discover the known sample data offerings which can be seeded into
        /// the database
        /// </summary>
        /// <remarks>
        /// Notice the 'available' field. Some offerings are not available if there
        /// is already overlapping data, yet this will return them anyway, but set
        /// the 'available' property to false. The SeedAsync() method will refuse to
        /// seed offerings which are not allowable at the time of seeding.
        /// </remarks>
        /// <returns>All known seed offerings</returns>
        public async Task<IEnumerable<ISampleDataSeedOffering>> ListSeedOfferingsAsync()
        {
            using var stream = Common.DotNet.Data.SampleData.Open("SampleDataSeedOfferings.json");

            var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new JsonStringEnumConverter());
            var result = await JsonSerializer.DeserializeAsync<List<SeedOffering>>(stream, options);

            foreach (var offering in result)
            {
                offering.IsAvailable = RulesOK(offering.Rules);
                offering.Description = offering.Description.Replace("{today}", _clock.Now.ToString("D"));
            }

            return result;
        }

        /// <summary>
        /// Add sample data to the runtime database
        /// </summary>
        /// <param name="id">Unique identifier for which offering to seed</param>
        /// <param name="hidden">Whether to hide the transactions. Useful if you want
        /// to reveal them over time</param>
        /// <returns>Result message described what happened</returns>
        public async Task<string> SeedAsync(string id, bool hidden = false)
        {
            var results = new List<string>();

            // First, get info about the offering
            var offerings = await ListSeedOfferingsAsync();
            var found = offerings.Where(x => x.ID == id);
            if (!found.Any())
                throw new ApplicationException($"Not found seed ID {id}");
            var offering = found.Single();

            // Ensure this is available. Note that re-listing them above ensures their
            // availability is recalculated.
            if (!offering.IsAvailable)
                throw new ApplicationException($"This data type is unavailable: {offering.Description}");

            // Load sample data
            var instream = File.OpenRead($"{_config.Directory}/SampleData-Full.xlsx");
            using var ssr = new SpreadsheetReader();
            ssr.Open(instream);

            // Add the kinds of data based on the rules
            if (offering.Rules.Contains(nameof(Transaction)))
            {
                var txs = ssr.Deserialize<Transaction>().ToList();

                if (hidden)
                    foreach (var tx in txs)
                        tx.Hidden = true;

                // Apply splits
                // TODO: DRY. Created a function for this repeated code
                if (ssr.SheetNames.Contains("Split"))
                {
                    var splits = ssr.Deserialize<Split>().ToLookup(x => x.TransactionID);
                    foreach(var tx in txs.Where(x=>x.ID > 0))
                    {
                        if (splits.Contains(tx.ID))
                        {
                            tx.Splits = splits[tx.ID].ToList();
                            tx.Category = null;
                            foreach (var split in tx.Splits)
                            {
                                // Clear any imported IDs
                                split.ID = 0;
                                split.TransactionID = 0;

                                // Workaround for Bug 1387: [Production Bug] Seed database with transactions does not save splits
                                // Splits need to be NOT EQUAL. So for now I'll cram in a memo to force inequality
                                split.Memo = tx.Timestamp.ToString("MM-dd");
                            }
                        }
                        // Also clear item ID, which will cause problems on import
                        tx.ID = 0;
                    }
                }

                await _context.BulkInsertAsync(txs);
                results.Add($"{txs.Count()} transactions");
            }
            if (offering.Rules.Contains("Today"))
            {
                var txs = ssr.Deserialize<Transaction>();

                DateTime last = DateTime.MinValue;
                var lastq = _context.Get<Transaction>().OrderByDescending(x => x.Timestamp).Select(x => x.Timestamp);
                if (lastq.Any())
                    last = lastq.First();

                var added = txs.Where(x => x.Timestamp > last && x.Timestamp <= _clock.Now).ToList();

                if (hidden)
                    foreach (var tx in added)
                        tx.Hidden = true;

                // Apply splits
                if (ssr.SheetNames.Contains("Split"))
                {
                    var splits = ssr.Deserialize<Split>().ToLookup(x => x.TransactionID);
                    foreach (var tx in added.Where(x => x.ID > 0))
                    {
                        if (splits.Contains(tx.ID))
                        {
                            tx.Splits = splits[tx.ID].ToList();
                            tx.Category = null;
                            foreach (var split in tx.Splits)
                            {
                                // Clear any imported IDs
                                split.ID = 0;
                                split.TransactionID = 0;
                            }
                        }
                        // Also clear item ID, which will cause problems on import
                        tx.ID = 0;
                    }
                }

                await _context.BulkInsertAsync(added);

                results.Add($"{added.Count()} transactions");
            }
            if (offering.Rules.Contains(nameof(BudgetTx)))
            {
                var added = ssr.Deserialize<BudgetTx>().ToList();
                await _context.BulkInsertAsync(added);
                results.Add($"{added.Count} budget line items");
            }
            if (offering.Rules.Contains(nameof(Payee)))
            {
                var added = ssr.Deserialize<Payee>().ToList();
                await _context.BulkInsertAsync(added);
                results.Add($"{added.Count} payee matching rules");
            }

            return "Added " + string.Join(", ", results);
        }

        /// <summary>
        /// Whether the given seed <paramref name="rules"/> are allowed given the current
        /// state of the database
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        private bool RulesOK(IEnumerable<string> rules)
        {
            foreach(var rule in rules)
            {
                if (nameof(BudgetTx) == rule)
                {
                    if (_context.Get<BudgetTx>().Any())
                        return false;
                }
                if (nameof(Payee) == rule)
                {
                    if (_context.Get<Payee>().Any())
                        return false;
                }
                if (nameof(Transaction) == rule)
                {
                    if (_context.Get<Transaction>().Any())
                        return false;
                }
                if ("Today" == rule)
                {
                    if (_context.Get<Transaction>().Any(x=>x.Timestamp >= _clock.Now))
                        return false;
                }
            }

            return true;

        }
    }

    internal class DownloadOffering : ISampleDataDownloadOffering
    {
        public string ID { get; set; }

        public SampleDataDownloadFileType FileType { get; set; }

        public string Description { get; set; }

        public SampleDataDownloadOfferingKind Kind { get; set; }
    }

    internal class SeedOffering : ISampleDataSeedOffering
    {
        public string ID { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public bool IsAvailable { get; set; }

        public SampleDataSeedOfferingCondition Condition { get; set; }

        public IEnumerable<string> Rules { get; set; }
    }
}
