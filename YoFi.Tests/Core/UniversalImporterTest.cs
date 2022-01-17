using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using YoFi.Core.Importers;
using YoFi.Core.Models;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Core
{
    [TestClass]
    public class UniversalImporterTest
    {
        UniversalImporter importer;

        [TestInitialize]
        public void SetUp()
        {
            var budgetrepo = new MockBudgetTxRepository();
            var budgetimport = new BaseImporter<BudgetTx>(budgetrepo);
            var payeerepo = new MockPayeeRepository();
            var payeeimport = new BaseImporter<Payee>(payeerepo);
            var txrepo = new MockTransactionRepository();
            var tximport = new TransactionImporter(txrepo, payeerepo);
            importer = new UniversalImporter(tximport, payeeimport, budgetimport);
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(importer);
        }

        public void BudgetTx()
        {
            // Given: A spreadsheet with budget transactions in a sheet named "BudgetTx", and all other kinds of data too
            // When: Importing it
            // Then: All items imported
        }
        public void BudgetTxNoName()
        {
            // Given: A spreadsheet with budget transactions in a sheet named {Something Random}
            // When: Importing it
            // Then: All items imported
        }
        public void Payee()
        {
            // Given: A spreadsheet with payees in a sheet named "Payee", and all other kinds of data too
            // When: Importing it
            // Then: All items imported
        }
        public void PayeeNoName()
        {
            // Given: A spreadsheet with payees in a sheet named {Something Random}
            // When: Importing it
            // Then: All items imported
        }
        public void Transactions()
        {
            // Given: A spreadsheet with transactions in a sheet named "Transaction", and all other kinds of data too
            // When: Importing it
            // Then: All items imported
        }
        public void TransactionsNoName()
        {
            // Given: A spreadsheet with transactions in a sheet named {Something Random} and splits in a sheet named {something random}
            // When: Importing it
            // Then: All items imported
        }
        public void TransactionsAndSplitsNoName()
        {
            // Given: A spreadsheet with transactions and splits in sheets each named {Something Random}
            // When: Importing it
            // Then: All items imported
        }
        public void TransactionsAndSplits()
        {
            // Given: A spreadsheet with transactions and splits in sheets each named "Transaction", and "Split"
            // When: Importing it
            // Then: All items imported
        }
        public void TransactionsOfx()
        {
            // Given: An OFX file with transactions
            // When: Importing it
            // Then: All items imported
        }
    }
}
