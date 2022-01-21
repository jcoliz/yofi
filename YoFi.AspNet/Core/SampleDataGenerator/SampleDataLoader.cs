using Common.DotNet;
using Common.NET.Data;
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

        public async Task<IEnumerable<ISampleDataDownloadOffering>> GetDownloadOfferingsAsync()
        {
            using var stream = Common.NET.Data.SampleData.Open("SampleDataDownloadOfferings.json");

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

        public async Task<IEnumerable<ISampleDataSeedOffering>> GetSeedOfferingsAsync()
        {
            using var stream = Common.NET.Data.SampleData.Open("SampleDataSeedOfferings.json");

            var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new JsonStringEnumConverter());
            var result = await JsonSerializer.DeserializeAsync<List<SeedOffering>>(stream, options);

            foreach (var offering in result)
                offering.IsAvailable = RulesOK(offering.Rules);

            return result;
        }

        public async Task<string> SeedAsync(string id)
        {
            var results = new List<string>();

            // First, get info about the offering
            var offerings = await GetSeedOfferingsAsync();
            var found = offerings.Where(x => x.ID == id);
            if (!found.Any())
                throw new ApplicationException($"Not found seed ID {id}");
            var offering = found.Single();

            // Ensure this is available
            if (!offering.IsAvailable)
                throw new ApplicationException($"This data type is unavailable: {offering.Description}");

            // Load sample data
            var instream = File.OpenRead($"{_config.Directory}/SampleData-Full.xlsx");
            using var ssr = new SpreadsheetReader();
            ssr.Open(instream);

            // Add the kinds of data based on the rules
            if (offering.Rules.Contains(nameof(Transaction)))
            {
                var added = ssr.Deserialize<Transaction>();
                _context.AddRange(added);
                results.Add($"{added.Count()} transactions");
            }
            if (offering.Rules.Contains("Today"))
            {
                var txs = ssr.Deserialize<Transaction>();

                DateTime last = DateTime.MinValue;
                var lastq = _context.Transactions.OrderByDescending(x => x.Timestamp).Select(x => x.Timestamp);
                if (lastq.Any())
                    last = lastq.First();

                var added = txs.Where(x => x.Timestamp > last && x.Timestamp <= _clock.Now);
                _context.AddRange(added);

                results.Add($"{added.Count()} transactions");
            }
            if (offering.Rules.Contains(nameof(BudgetTx)))
            {
                var added = ssr.Deserialize<BudgetTx>();
                _context.AddRange(added);
                results.Add($"{added.Count()} budget line items");
            }
            if (offering.Rules.Contains(nameof(Payee)))
            {
                var added = ssr.Deserialize<Payee>();
                _context.AddRange(added);
                results.Add($"{added.Count()} payee matching rules");
            }

            await _context.SaveChangesAsync();
            return "OK: Added " + string.Join(", ", results);
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
                    if (_context.BudgetTxs.Any())
                        return false;
                }
                if (nameof(Payee) == rule)
                {
                    if (_context.Payees.Any())
                        return false;
                }
                if (nameof(Transaction) == rule)
                {
                    if (_context.Transactions.Any())
                        return false;
                }
                if ("Today" == rule)
                {
                    // We'll come back to this
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

        public bool IsRecommended { get; set; }

        public bool IsAvailable { get; set; }

        public SampleDataSeedOfferingCondition Condition { get; set; }

        public IEnumerable<string> Rules { get; set; }
    }
}
