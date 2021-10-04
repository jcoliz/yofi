﻿using jcoliz.OfficeOpenXml.Easy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YoFi.AspNet.Models;

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
            Definitions.AddRange(ssr.Read<SampleDataLineItem>());
        }

        public void GenerateTransactions()
        {
            var groupings = Definitions.ToLookup(x => x.Group);

            // Generate the solos
            if (groupings.Contains(null))
            {
                Transactions.AddRange(groupings[null].SelectMany(x => x.GetTransactions()));
            }

            // Generate the groups
            int nextid = 1;
            foreach(var group in groupings.Where(x => !string.IsNullOrEmpty(x.Key)))
            {
                var main = group.Where(x => !string.IsNullOrEmpty(x.Payee));

                if (!main.Any())
                    throw new ApplicationException($"Group {group.Key} has no definition with a payee");

                if (main.Skip(1).Any())
                    throw new ApplicationException($"Group {group.Key} has multiple definitions with a payee. Expecting only one");

                var txs = main.Single().GetTransactions(group).ToList();
                
                // Add ID matchers so we can later get the tranactions and splits matched up
                foreach(var tx in txs)
                {
                    var id = nextid++;
                    tx.ID = id;
                    foreach (var split in tx.Splits)
                        split.TransactionID = id;
                }

                Transactions.AddRange(txs);
            }

            Transactions.Sort((x, y) => x.Timestamp.CompareTo(y.Timestamp));
        }

        public void GeneratePayees()
        {
            Payees = Definitions.Where(x => x.Payee != null).ToLookup(x=>x.Payee).SelectMany(x=>x.Key.Split(',').Select(y=>new AspNet.Models.Payee() { Name = y, Category = x.First().Category })).ToList();
        }

        public void GenerateBudget()
        {
            BudgetTxs = Definitions
                            .Where(x => x.Category != null)
                            .ToLookup(x => x.Category)
                            .Select(x => new AspNet.Models.BudgetTx { Category = x.Key, Amount = x.Sum(y => y.AmountYearly), Timestamp = new DateTime(SampleDataLineItem.Year,1,1) })
                            .ToList();
        }

        /// <summary>
        /// Save all generated data to spreadsheet at <paramref name="stream"/>
        /// </summary>
        /// <param name="stream"></param>
        public void Save(Stream stream)
        {
            using var ssr = new OpenXmlSpreadsheetWriter();
            ssr.Open(stream);
            ssr.Write(Transactions);
            ssr.Write(Transactions.Where(x=>x.Splits?.Count > 1).SelectMany(x => x.Splits),"Split");
            ssr.Write(Payees);
            ssr.Write(BudgetTxs);
        }

        public List<SampleDataLineItem> Definitions { get; } = new List<SampleDataLineItem>();

        public List<Transaction> Transactions { get; } = new List<Transaction>();

        public List<Payee> Payees { get; private set; } = new List<Payee>();

        public List<BudgetTx> BudgetTxs { get; private set; } = new List<BudgetTx>();
    }
}