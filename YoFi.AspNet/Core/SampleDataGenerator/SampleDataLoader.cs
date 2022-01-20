using Common.DotNet;
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
        private readonly string _directory;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">Application data context</param>
        /// <param name="directory">Location of sample data file</param>
        public SampleDataLoader(IDataContext context, IClock clock, string directory)
        {
            _context = context;
            _clock = clock;
            _directory = directory;
        }

        public Task<Stream> DownloadSampleDataAsync(string id)
        {
            Stream stream;

            var instream = System.IO.File.OpenRead($"{_directory}/SampleData-Full.xlsx");

            if ("full" == id)
            {
                // Just return it!
                stream = instream;
            }
            else if ("budgettx" == id)
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
            else if ("payee" == id)
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

        public Task<IEnumerable<ISampleDataSeedOffering>> GetSeedOfferingsAsync()
        {
            throw new NotImplementedException();
        }

        public Task SeedAsync(string id)
        {
            throw new NotImplementedException();
        }
    }

    internal class DownloadOffering : ISampleDataDownloadOffering
    {
        public string ID { get; set; }

        public SampleDataDownloadFileType FileType { get; set; }

        public string Description { get; set; }

        public SampleDataDownloadOfferingKind Kind { get; set; }
    }
}
