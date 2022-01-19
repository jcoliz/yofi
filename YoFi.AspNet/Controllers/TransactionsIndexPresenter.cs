using Common.AspNet;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core;
using YoFi.Core.Models;
using YoFi.Core.Repositories.Wire;

namespace YoFi.AspNet.Controllers
{
    /// <summary>
    /// Everything the Transactions Index view needs to display the index page
    /// </summary>
    /// <remarks>
    /// Given that this page is the main page of the application, it makes sense
    /// for it to have the most UI complexity, and ergo the most need for a
    /// dedicated presenter to build the arrange the data nicely for the view
    /// 
    /// 
    /// This also could be thought of as a viewmodel.
    /// </remarks>
    public class TransactionsIndexPresenter : IWireQueryResult<Transaction>
    {
        public TransactionsIndexPresenter(IWireQueryResult<Transaction> qresult)
        {
            _qresult = qresult;
            ShowHidden = qresult.Parameters.View?.ToLowerInvariant().Contains("h") == true;
            ShowSelected = qresult.Parameters.View?.ToLowerInvariant().Contains("s") == true;
        }

        private readonly IWireQueryResult<Transaction> _qresult;

        #region IWireQueryParameters
        public IWireQueryParameters Parameters => _qresult.Parameters;

        public IEnumerable<Transaction> Items => _qresult.Items;

        public IWirePageInfo PageInfo => _qresult.PageInfo;

        #endregion

        #region Public Properties -- Used by View to display

        /// <summary>
        /// Whether to show the 'hidden' checkbox
        /// </summary>
        public bool ShowHidden { get; set; }

        /// <summary>
        /// Whether to show the 'select' checkbox
        /// </summary>
        public bool ShowSelected { get; set; }

        /// <summary>
        /// What "O" parameter to send in if user chooses to order by Date
        /// </summary>
        public string NextDateOrder => (Parameters.Order == null) ? "da" : null; /* not "dd", which is default */

        /// <summary>
        /// What "O" parameter to send in if user chooses to order by Payee
        /// </summary>
        public string NextPayeeOrder => (Parameters.Order == "pa") ? "pd" : "pa";

        /// <summary>
        /// What "O" parameter to send in if user chooses to order by Category
        /// </summary>
        public string NextCategoryOrder => (Parameters.Order == "ca") ? "cd" : "ca";

        /// <summary>
        /// What "O" parameter to send in if user chooses to order by Amount
        /// </summary>
        public string NextAmountOrder => (Parameters.Order == "aa") ? "as" : "aa";

        /// <summary>
        /// What "O" parameter to send in if user chooses to order by BankReference
        /// </summary>
        public string NextBankReferenceOrder => (Parameters.Order == "ra") ? "rd" : "ra";

        /// <summary>
        /// What "V" parameter to send in if user chooses to toggle the hidden state
        /// </summary>
        public string ToggleHidden => (ShowHidden ? string.Empty : "h") + (ShowSelected ? "s" : string.Empty);

        /// <summary>
        /// What "V" parameter to send in if user chooses to toggle the selected state
        /// </summary>
        public string ToggleSelected => (ShowHidden ? "h" : string.Empty) + (ShowSelected ? string.Empty : "s");
        #endregion

    }
}
