using Common.DotNet;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using YoFi.Core.Models;

namespace YoFi.Core.Repositories
{
    /// <summary>
    /// Build transactions query from textual 'q' parameter
    /// </summary>
    /// <remarks>
    /// This is complex enough that I thought it deserved it own class.
    /// </remarks>
    public class TransactionsQueryBuilder
    {
        #region Properties

        /// <summary>
        /// The resulting query
        /// </summary>
        /// <remarks>
        /// This is what we build over time
        /// </remarks>
        public IQueryable<Transaction> Query { get; private set; }

        #endregion

        #region Fields

        private readonly IClock _clock;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="initial">The starting large set of items which we will further winnow down </param>
        public TransactionsQueryBuilder(IQueryable<Transaction> initial, IClock clock)
        {
            Query = initial;
            _clock = clock;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Interprets the "q" (Query) parameter on a transactions search
        /// </summary>
        /// <remarks>
        /// Public so can be used by other controllers
        /// </remarks>
        /// <param name="q">Query parameter</param>
        /// <returns>Resulting query refined by <paramref name="q"/></returns>
        public void BuildForQ(string q)
        {
            if (!string.IsNullOrEmpty(q))
            {
                var terms = q.Split(',');

                // User Story 1385: Transactions search defaults to last 12 months

                // If there is any "y" term, then we will NOT apply the last-12 rule
                if (!terms.Any(x=>x.ToLowerInvariant().StartsWith("y")))
                {
                    Query = Query.Where(x => x.Timestamp > _clock.Now - TimeSpan.FromDays(366));
                }

                foreach (var term in terms)
                {
                    // Look for "{key}={value}" terms
                    if (term.Length > 2 && term[1] == '=')
                    {
                        var key = term.ToLowerInvariant().First();
                        var value = term[2..];

                        Query = key switch
                        {
                            'p' => TransactionsForQuery_Payee(Query, value),
                            'c' => TransactionsForQuery_Category(Query, value),
                            'y' => TransactionsForQuery_Year(Query, value),
                            'm' => TransactionsForQuery_Memo(Query, value),
                            'r' => TransactionsForQuery_HasReceipt(Query, value),
                            'a' => TransactionsForQuery_Amount(Query, value),
                            'd' => TransactionsForQuery_Date(Query, value),
                            'i' => TransactionsForQuery_Imported(Query, value),
                            _ => throw new ArgumentException($"Unknown query parameter {key}", nameof(q))
                        };

                    }
                    else if (Int32.TryParse(term, out Int32 intval))
                    {
                        // If this is an integer search term, there's a lot of places it can be. It will
                        // the the SAME as the text search, below, plus amount or date.

                        // One tricky thing is figuring out of it's a valid date
                        DateTime? dtval = null;
                        try
                        {
                            dtval = new DateTime(_clock.Now.Year, intval / 100, intval % 100);
                        }
                        catch
                        {
                            // Any issues, we'll leave dtval as null
                        }

                        if (dtval.HasValue)
                        {
                            Query = Query.Where(x =>
                                x.Category.Contains(term) ||
                                x.Memo.Contains(term) ||
                                x.Payee.Contains(term) ||
                                x.Amount == (decimal)intval ||
                                x.Amount == ((decimal)intval) / 100 ||
                                x.Amount == -(decimal)intval ||
                                x.Amount == -((decimal)intval) / 100 ||
                                (x.Timestamp >= dtval && x.Timestamp <= dtval.Value.AddDays(7)) ||
                                x.Splits.Any(s =>
                                    s.Category.Contains(term) ||
                                    s.Memo.Contains(term) ||
                                    s.Amount == (decimal)intval ||
                                    s.Amount == ((decimal)intval) / 100 ||
                                    s.Amount == -(decimal)intval ||
                                    s.Amount == -((decimal)intval) / 100
                                )
                            );
                        }
                        else
                        {
                            Query = Query.Where(x =>
                                x.Category.Contains(term) ||
                                x.Memo.Contains(term) ||
                                x.Payee.Contains(term) ||
                                x.Amount == (decimal)intval ||
                                x.Amount == ((decimal)intval) / 100 ||
                                x.Amount == -(decimal)intval ||
                                x.Amount == -((decimal)intval) / 100 ||
                                x.Splits.Any(s =>
                                    s.Category.Contains(term) ||
                                    s.Memo.Contains(term) ||
                                    s.Amount == (decimal)intval ||
                                    s.Amount == ((decimal)intval) / 100 ||
                                    s.Amount == -(decimal)intval ||
                                    s.Amount == -((decimal)intval) / 100
                                )
                            );
                        }

                    }
                    else
                    {
                        // Look for term anywhere
                        Query = Query.Where(x =>
                            x.Category.Contains(term) ||
                            x.Memo.Contains(term) ||
                            x.Payee.Contains(term) ||
                            x.Splits.Any(s =>
                                s.Category.Contains(term) ||
                                s.Memo.Contains(term)
                            )
                        );

                    }
                }
            }
        }

        /// <summary>
        /// Interprets the "o" (Order) parameter on a transactions search
        /// </summary>
        /// <remarks>
        /// Public so can be used by other controllers.
        /// </remarks>
        /// <param name="result">Initial query to further refine</param>
        /// <param name="p">Order parameter</param>
        /// <returns>Resulting query refined by <paramref name="o"/></returns>
        internal void ApplyOrderParameter(string o)
        {
            Query = o switch
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
                null => Query.OrderByDescending(s => s.Timestamp).ThenBy(s => s.Payee),
                _ => throw new ArgumentException($"Unexpected order parameter {o}", nameof(o))
            };
        }

        /// <summary>
        /// Apply the view parameter to the presenter
        /// </summary>
        internal void ApplyViewParameter(string v)
        {
            if (!(v?.ToLowerInvariant().Contains('h') == true))
                Query = Query.Where(x => x.Hidden != true);
        }

#if false
//
// Leaving out the DTOs for now, to simplify. I'll come back to this.
//
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
#endif

        #endregion

        #region Internals

        /// <summary>
        /// Transalte query on payee
        /// </summary>
        private static IQueryable<Transaction> TransactionsForQuery_Payee(IQueryable<Transaction> result, string value) =>
            result.Where(x => x.Payee.Contains(value));

        /// <summary>
        /// Translate query on category, including splits
        /// </summary>
        private static IQueryable<Transaction> TransactionsForQuery_Category(IQueryable<Transaction> result, string value) =>
            (value.ToLowerInvariant() == "[blank]")
                ? result.Where(x => string.IsNullOrEmpty(x.Category) && !x.Splits.Any())
                : result.Where(x =>
                        x.Category.Contains(value)
                        ||
                        x.Splits.Any(s => s.Category.Contains(value)
                    )
                );

        /// <summary>
        /// Transalte query on year
        /// </summary>
        private static IQueryable<Transaction> TransactionsForQuery_Year(IQueryable<Transaction> result, string value) =>
            (Int32.TryParse(value, out int year))
                ? result.Where(x => x.Timestamp.Year == year)
                : result;

        /// <summary>
        /// Translate query on memo
        /// </summary>
        private static IQueryable<Transaction> TransactionsForQuery_Memo(IQueryable<Transaction> result, string value) =>
            result.Where(x => x.Memo.Contains(value) || x.Splits.Any(s=>s.Memo.Contains(value)));

        /// <summary>
        /// Transalte query on having receipt
        /// </summary>
        private static IQueryable<Transaction> TransactionsForQuery_HasReceipt(IQueryable<Transaction> result, string value) =>
            value switch
            {
                "0" => result.Where(x => x.ReceiptUrl == null),
                "1" => result.Where(x => x.ReceiptUrl != null),
                _ => throw new ArgumentException($"Unexpected query parameter {value}", nameof(value))
            };
        /// <summary>
        /// Transalte query on having the import flag set
        /// </summary>
        private static IQueryable<Transaction> TransactionsForQuery_Imported(IQueryable<Transaction> result, string value) =>
            value switch
            {
                "0" => result.Where(x => x.Imported != true),
                "1" => result.Where(x => x.Imported == true),
                _ => throw new ArgumentException($"Unexpected query parameter {value}", nameof(value))
            };


        /// <summary>
        /// Translate query on amount
        /// </summary>
        private static IQueryable<Transaction> TransactionsForQuery_Amount(IQueryable<Transaction> result, string value)
        {
            if (Int32.TryParse(value, out Int32 ival))
            {
                var cents = ((decimal)ival) / 100;
                return result.Where(x => x.Amount == (decimal)ival || x.Amount == cents || x.Amount == -(decimal)ival || x.Amount == -cents);
            }
            else if (decimal.TryParse(value, out decimal dval))
            {
                return result.Where(x => x.Amount == dval || x.Amount == -dval);
            }
            else
                throw new ArgumentException($"Unexpected query parameter {value}", nameof(value));
        }

        // TODO: There is a bug here. Illicit use of "DateTime Now"!! Should consume an ICLOCK instead.

        /// <summary>
        /// Translate query on date
        /// </summary>
        private IQueryable<Transaction> TransactionsForQuery_Date(IQueryable<Transaction> result, string value)
        {
            DateTime? dtval = null;

            if (Int32.TryParse(value, out int ival) && ival >= 101 && ival <= 1231)
                dtval = new DateTime(_clock.Now.Year, ival / 100, ival % 100);
            else if (DateTime.TryParse(value, out DateTime dtvalout))
            {
                dtval = new DateTime(_clock.Now.Year, dtvalout.Month, dtvalout.Day);
            }

            if (dtval.HasValue)
                return result.Where(x => x.Timestamp >= dtval.Value && x.Timestamp < dtval.Value.AddDays(7));
            else
                throw new ArgumentException($"Unexpected query parameter {value}", nameof(value));
        }

#endregion

    }

#if false
    // Currently not using DTO's. Do need to bring it back though

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
#endif
}
