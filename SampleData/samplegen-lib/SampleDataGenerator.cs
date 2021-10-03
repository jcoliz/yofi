using jcoliz.OfficeOpenXml.Easy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace YoFi.SampleGen
{
    public class SampleDataGenerator
    {
        /// <summary>
        /// Load defintions from spreadsheet at <paramref name="stream"/>
        /// </summary>
        /// <param name="stream"></param>
        public void LoadDefinitions(Stream stream)
        {
            using var ssr = new OpenXmlSpreadsheetReader();
            ssr.Open(stream);
            Definitions.AddRange(ssr.Read<Definition>());
        }

        public void GenerateTransactions()
        {
            var groupings = Definitions.ToLookup(x => x.Group);

            // Handle the solos
            if (groupings.Contains(null))
            {
                Transactions.AddRange(groupings[null].SelectMany(x => x.GetTransactions()));
            }

            foreach(var group in groupings.Where(x => !string.IsNullOrEmpty(x.Key)))
            {
                var main = group.Where(x => !string.IsNullOrEmpty(x.Payee));

                if (!main.Any())
                    throw new ApplicationException($"Group {group.Key} has no definition with a payee");

                if (main.Skip(1).Any())
                    throw new ApplicationException($"Group {group.Key} has multiple definitions with a payee. Expecting only one");

                Transactions.AddRange(main.Single().GetTransactions(group));
            }

            Transactions.Sort((x, y) => x.Timestamp.CompareTo(y.Timestamp));
        }

        /// <summary>
        /// Save all generated data to spreadsheet at <paramref name="stream"/>
        /// </summary>
        /// <param name="stream"></param>
        public void Save(Stream stream)
        {
            throw new NotImplementedException();
        }

        public List<Definition> Definitions { get; } = new List<Definition>();

        public List<Transaction> Transactions { get; } = new List<Transaction>();
    }
}
