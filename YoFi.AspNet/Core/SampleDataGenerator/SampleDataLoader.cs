using Common.DotNet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
            Stream result;

            var instream = System.IO.File.OpenRead($"{_directory}/SampleData-Full.xlsx");

            if ("full" == id)
            {
                // Just return it!
                result = instream;
            }
            else
                throw new ApplicationException($"Not found sample data ID {id}");

            return Task.FromResult(result);
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
                                        ID = m.ToString()
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

        public string FileType { get; set; }

        public string Description { get; set; }

        public SampleDataDownloadOfferingKind Kind { get; set; }
    }
}
