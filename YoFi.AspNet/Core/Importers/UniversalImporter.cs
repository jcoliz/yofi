﻿using jcoliz.OfficeOpenXml.Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;

namespace YoFi.Core.Importers
{
    /// <summary>
    /// Handles importing any and all data types in a single upload file
    /// </summary>
    public class UniversalImporter
    {
        private readonly TransactionImporter _transactionImporter;
        private readonly IImporter<Payee> _payeeImporter;
        private readonly IImporter<BudgetTx> _budgettxImporter;

        public UniversalImporter(TransactionImporter transactionImporter, IImporter<Payee> payeeImporter, IImporter<BudgetTx> budgettxImporter)
        {
            _transactionImporter = transactionImporter;
            _payeeImporter = payeeImporter;
            _budgettxImporter = budgettxImporter;
        }

        public void QueueImportFromXlsx(Stream stream)
        {
            // Universal importer supports extracting data types from spreadsheets which are
            // explicitly named.

            using var ssr = new SpreadsheetReader();
            ssr.Open(stream);
            if (ssr.SheetNames.Any())
            {
                if (ssr.SheetNames.Contains(nameof(BudgetTx)))
                {
                    _budgettxImporter.QueueImportFromXlsx(ssr);
                }
                if (ssr.SheetNames.Contains(nameof(Payee)))
                {
                    _payeeImporter.QueueImportFromXlsx(ssr);
                }
                if (ssr.SheetNames.Contains(nameof(Transaction)))
                {
                    _transactionImporter.QueueImportFromXlsx(ssr);
                }

                // Also, it will extract try to extract transactions from spreadsheets with ANY
                // name, as long as they're not claimed by something else
                var firstname = ssr.SheetNames.First();
                var others = new string[] { nameof(BudgetTx), nameof(Payee), nameof(Transaction), nameof(Split) };
                if (!others.Contains(firstname))
                {
                    _transactionImporter.QueueImportFromXlsx(ssr);
                }
            }
        }

        public void QueueImportFromXlsx<T>(Stream stream)
        {
            if (typeof(T) == typeof(BudgetTx))
            {
                _budgettxImporter.QueueImportFromXlsx(stream);
            }
            else
            if (typeof(T) == typeof(Payee))
            {
                _payeeImporter.QueueImportFromXlsx(stream);
            }
            else
                throw new NotImplementedException();
        }

        public async Task ProcessImportAsync()
        {
            await _budgettxImporter.ProcessImportAsync();
            await _payeeImporter.ProcessImportAsync();
            await _transactionImporter.ProcessImportAsync();
        }
    }
}
