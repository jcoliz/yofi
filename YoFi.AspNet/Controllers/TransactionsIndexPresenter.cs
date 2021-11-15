using Common.AspNet;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core;
using YoFi.Core.Models;

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
    public class TransactionsIndexPresenter : IViewParameters
    {
        public TransactionsIndexPresenter(IAsyncQueryExecution queryExecution)
        {
            _queryExecution = queryExecution;
        }

        #region Public Properties -- Used by View to display

        /// <summary>
        /// Model items to be displayed
        /// </summary>
        public IEnumerable<TransactionIndexDto> Items { get; set; }

        /// <summary>
        /// Page divider containing pagination properties
        /// </summary>
        public PageDivider Divider { get; set; }

        /// <summary>
        /// "Q" parameter used to build this page
        /// </summary>
        /// <remarks>
        /// Describes the subset of items to be included
        /// </remarks>
        public string QueryParameter { get; set; }

        /// <summary>
        /// "V" parameter used to build this page
        /// </summary>
        /// <remarks>
        /// Describes what items should be included in the view
        /// </remarks>
        public string ViewParameter
        {
            get
            {
                return _View;
            }
            set
            {
                _View = value;
                ShowHidden = ViewParameter?.ToLowerInvariant().Contains("h") == true;
                ShowSelected = ViewParameter?.ToLowerInvariant().Contains("s") == true;
            }
        }
        private string _View;

        /// <summary>
        /// "O" parameter used to build this page
        /// </summary>
        /// <remarks>
        /// Describes what order should the items be displayed
        /// </remarks>
        public string OrderParameter
        {
            get
            {
                return (_Order == default_order) ? null : _Order;
            }
            set
            {
                _Order = string.IsNullOrEmpty(value) ? default_order : value;
            }
        }
        private string _Order;
        const string default_order = "dd";

        /// <summary>
        /// "P" parameter used to build this page
        /// </summary>
        /// <remarks>
        /// Describes which page this is
        /// </remarks>
        public int? PageParameter
        {
            get
            {
                return (_PageParameter == default_page) ? null : (int?)_PageParameter;
            }
            set
            {
                _PageParameter = value ?? default_page;
            }
        }
        private int _PageParameter = default_page;
        const int default_page = 1;
        private readonly IAsyncQueryExecution _queryExecution;

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
        public string NextDateOrder => (_Order == "dd") ? "da" : null; /* not "dd", which is default */

        /// <summary>
        /// What "O" parameter to send in if user chooses to order by Payee
        /// </summary>
        public string NextPayeeOrder => (_Order == "pa") ? "pd" : "pa";

        /// <summary>
        /// What "O" parameter to send in if user chooses to order by Category
        /// </summary>
        public string NextCategoryOrder => (_Order == "ca") ? "cd" : "ca";

        /// <summary>
        /// What "O" parameter to send in if user chooses to order by Amount
        /// </summary>
        public string NextAmountOrder => (_Order == "aa") ? "as" : "aa";

        /// <summary>
        /// What "O" parameter to send in if user chooses to order by BankReference
        /// </summary>
        public string NextBankReferenceOrder => (_Order == "ra") ? "rd" : "ra";

        /// <summary>
        /// What "V" parameter to send in if user chooses to toggle the hidden state
        /// </summary>
        public string ToggleHidden => (ShowHidden ? string.Empty : "h") + (ShowSelected ? "s" : string.Empty);

        /// <summary>
        /// What "V" parameter to send in if user chooses to toggle the selected state
        /// </summary>
        public string ToggleSelected => (ShowHidden ? "h" : string.Empty) + (ShowSelected ? string.Empty : "s");
        #endregion

        /// <summary>
        /// The in-progress query we are building up
        /// </summary>
        internal IQueryable<Transaction> Query { get; set; }

        /// <summary>
        /// Interprets the "o" (Order) parameter on a transactions search
        /// </summary>
        /// <remarks>
        /// Public so can be used by other controllers.
        /// </remarks>
        /// <param name="result">Initial query to further refine</param>
        /// <param name="p">Order parameter</param>
        /// <returns>Resulting query refined by <paramref name="o"/></returns>
        internal void ApplyOrderParameter()
        {
            Query = _Order switch
            {
                // Coverlet finds cyclomatic complexity of 42 in this function!!?? No clue why it's not just 10.
                "aa" => Query.OrderBy(s => s.Amount),
                "ad" => Query.OrderByDescending(s => s.Amount),
                "ra" => Query.OrderBy(s => s.BankReference),
                "rd" => Query.OrderByDescending(s => s.BankReference),
                "pa" => Query.OrderBy(s => s.Payee),
                "pd" => Query.OrderByDescending(s => s.Payee),
                "ca" => Query.OrderBy(s => s.Category),
                "cd" => Query.OrderByDescending(s => s.Category),
                "da" => Query.OrderBy(s => s.Timestamp).ThenBy(s => s.Payee),
                "dd" => Query.OrderByDescending(s => s.Timestamp).ThenBy(s => s.Payee),
                _ => Query
            };
        }

        /// <summary>
        /// Apply the contents of the Page parameter to the presenter
        /// </summary>
        /// <returns></returns>
        internal async Task ApplyPageParameterAsync()
        {
            Query = await Divider.ItemsForPage(Query, _PageParameter);
            Divider.ViewParameters = this;
        }

        /// <summary>
        /// Apply the view parameter to the presenter
        /// </summary>
        internal void ApplyViewParameter()
        {
            if (!ShowHidden)
                Query = Query.Where(x => x.Hidden != true);
        }

        /// <summary>
        /// Build the <see cref="TransactionsIndexPresenter.Items"/> list out of the current query
        /// </summary>
        internal async Task ExecuteQueryAsync()
        {
            if (ShowHidden || ShowSelected)
            {
                // Get the long form
                var dtoquery = Query.Select(t => new TransactionIndexDto()
                {
                    ID = t.ID,
                    Timestamp = t.Timestamp,
                    Payee = t.Payee,
                    Amount = t.Amount,
                    Category = t.Category,
                    Memo = t.Memo,
                    HasReceipt = t.ReceiptUrl != null,
                    HasSplits = t.Splits.Any(),
                    BankReference = t.BankReference,
                    Hidden = t.Hidden ?? false,
                    Selected = t.Selected ?? false
                });

                Items = await _queryExecution.ToListNoTrackingAsync(dtoquery);
            }
            else
            {
                // Get the shorter form
                var dtoquery = Query.Select(t => new TransactionIndexDto()
                {
                    ID = t.ID,
                    Timestamp = t.Timestamp,
                    Payee = t.Payee,
                    Amount = t.Amount,
                    Category = t.Category,
                    Memo = t.Memo,
                    HasReceipt = t.ReceiptUrl != null,
                    HasSplits = t.Splits.Any(),
                });

                Items = await _queryExecution.ToListNoTrackingAsync(dtoquery);
            }
        }

        /// <summary>
        /// The transaction data for Index page
        /// </summary>
        public class TransactionIndexDto
        {
            public int ID { get; set; }
            [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
            [Display(Name = "Date")]
            public DateTime Timestamp { get; set; }
            public string Payee { get; set; }
            [DisplayFormat(DataFormatString = "{0:C2}")]
            public decimal Amount { get; set; }
            public string Category { get; set; }
            public string Memo { get; set; }
            public bool HasReceipt { get; set; }
            public bool HasSplits { get; set; }

            // Only needed in some cases

            public string BankReference { get; set; }
            public bool Hidden { get; set; }
            public bool Selected { get; set; }

            // This is just for test cases, so it's a limited transaltion, just what we need for
            // certain cases.
            public static explicit operator Transaction(TransactionIndexDto o) => new Transaction()
            {
                Category = o.Category,
                Memo = o.Memo,
                Payee = o.Payee
            };

            public bool Equals(Transaction other)
            {
                return string.Equals(Payee, other.Payee) && Amount == other.Amount && Timestamp.Date == other.Timestamp.Date;
            }
        }
    }
}
