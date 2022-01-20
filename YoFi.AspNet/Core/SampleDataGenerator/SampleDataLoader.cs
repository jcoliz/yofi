using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Core.SampleGen
{
    public class SampleDataLoader : ISampleDataLoader
    {
        private readonly IDataContext _context;
        private readonly string _directory;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">Application data context</param>
        /// <param name="directory">Location of sample data file</param>
        public SampleDataLoader(IDataContext context, string directory)
        {
            _context = context;
            _directory = directory;
        }

        public Task<Stream> DownloadSampleDataAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ISampleDataDownloadOffering>> GetDownloadOfferings()
        {
            throw new NotImplementedException();
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
}
