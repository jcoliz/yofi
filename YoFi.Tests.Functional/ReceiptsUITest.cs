using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YoFi.Tests.Functional
{
    /// <summary>
    /// Feature 1189: Receipt Matching: Upload multiple receipts in one place, auto match them to transactions by file name
    /// </summary>
    [TestClass]
    public class ReceiptsUITest : FunctionalUITest
    {
        #region User Story 1190: [User Can] Upload receipts independently of transactions

        public async Task NavigateToReceiptsPage()
        {
            /*
            Given: On Transaction Page
            And: Clicking on Actions
            When: Clicking on Match Receipts
            Then: On Receipts Page
            */
        }

        public async Task UploadReceipts()
        {
            /*
            Given: On receipts page
            When: Uploading many files with differing name compositions
            Then: Results displayed match number and composition of expected receipts
            */
        }

        public async Task UploadMatching()
        {
            /*
            Given: A set of transactions
            And: On receipts page
            When: Uploading many files with differing name compositions, some of which will exactly match the transactions
            Then: Matching receipts are found and shown, as expected, with matches shown as expected
            */
        }

        #endregion

        #region User Story 1191: [User Can] Accept automatically matched receipts to transactions 

        public async Task AcceptAll()
        {
            /*
            Given: Many receipts with differing name compositions, some of which will exactly match the transactions
            When: Accept All
            Then: The receipts which have a single match are all assigned to that transaction
            And: The receipts are removed from the display
            */
        }

        #endregion

        #region User Story 1310: [User Can] Manually choose among possible matches for uploaded receipts when no direct match is found

        public async Task ReviewMatches()
        {
            /*
            Given: Many receipts with differing name compositions, some of which will exactly match the transactions
            When: Tapping “review”
            Then: The receipts which match the selected transaction are shown
            */
        }

        public async Task AcceptChosenMatch()
        {
            /*
            Given: Reviewing multiple matches for a receipt
            When: Tapping “Match”
            Then: The receipt is added to the selected transaction
            And: The receipt is removed from the display
            */
        }

        public async Task MemoInTransaction()
        {
            /*
            Given: A receipt which matches an existing transaction and contains a memo
            When: Accept All
            And: Navigating to Transactions Page
            And: Searching for the affected Transaction
            Then: The memo of the transaction matches the memo from the receipt
            */
        }

        #endregion

        #region User Story 1192: [User Can] Delete uploaded receipts before matching them to a transaction

        public async Task Delete()
        {
            /*
            Given: On Receipts page, with an uploaded unmatched receipt
            When: Deleting it
            Then: No receipts shown on the page
            */
        }

        #endregion
    }
}
