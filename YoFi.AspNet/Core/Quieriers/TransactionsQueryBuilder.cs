using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;

namespace YoFi.Core.Quieriers
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

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="initial">The starting large set of items which we will further winnow down </param>
        public TransactionsQueryBuilder(IQueryable<Transaction> initial)
        {
            Query = initial;
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
        public void Build(string q)
        {
            if (!string.IsNullOrEmpty(q))
            {
                var terms = q.Split(',');

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
                            _ => throw new ArgumentException($"Unknown query parameter {key}", nameof(key))
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
                            dtval = new DateTime(DateTime.Now.Year, intval / 100, intval % 100);
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
            result.Where(x => x.Memo.Contains(value));

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

        /// <summary>
        /// Translate query on date
        /// </summary>
        private static IQueryable<Transaction> TransactionsForQuery_Date(IQueryable<Transaction> result, string value)
        {
            DateTime? dtval = null;

            if (Int32.TryParse(value, out int ival) && ival >= 101 && ival <= 1231)
                dtval = new DateTime(DateTime.Now.Year, ival / 100, ival % 100);
            else if (DateTime.TryParse(value, out DateTime dtvalout))
                dtval = dtvalout;

            if (dtval.HasValue)
                return result.Where(x => x.Timestamp >= dtval.Value && x.Timestamp < dtval.Value.AddDays(7));
            else
                throw new ArgumentException($"Unexpected query parameter {value}", nameof(value));
        }

        #endregion

    }
}
