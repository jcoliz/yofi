using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using YoFi.Core.Models;

namespace YoFi.SampleGen
{
    /// <summary>
    /// Defines a single pattern of yearly spending
    /// </summary>
    /// <remarks>
    /// The sample data generator will use this to generate a series of transactions to
    /// match this spending pattern in a year
    /// </remarks>
    public class SampleDataPattern: ISplitPattern
    {
        /// <summary>
        /// Comma-separated list of possible transaction payees
        /// </summary>
        public string Payee { get; set; }

        /// <summary>
        /// How frequently this spending happens
        /// </summary>
        public FrequencyEnum DateFrequency { get; set; }

        /// <summary>
        /// How much variability (jitter) is there between the dates of multiple transactions of
        /// the same pattern
        /// </summary>
        public JitterEnum DateJitter { get; set; }

        /// <summary>
        /// How many times this spending happens within the DateFrequency
        /// </summary>
        public int DateRepeats { get; set; } = 1;

        /// <summary>
        /// Transaction category
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// The target amount to spend yearly in this pattern
        /// </summary>
        public decimal AmountYearly { get; set; }

        /// <summary>
        /// How much variability (jitter) is there between the amounts of multiple transactions of
        /// the same pattern
        /// </summary>
        public JitterEnum AmountJitter { get; set; }

        /// <summary>
        /// User-defined grouping for multiple patterns
        /// </summary>
        /// <remarks>
        /// Generator will combine these all into a single transaction, with the multiple
        /// patterns as splits
        /// </remarks>
        public string Group { get; set; }

        /// <summary>
        /// Json loan definition, if this is a loan payment
        /// </summary>
        public string Loan { get; set; }

        public Loan LoanObject
        {
            get
            {
                if (!string.IsNullOrEmpty(Loan))
                {
                    var loan = JsonSerializer.Deserialize<Loan>(Loan, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    loan.Principal = Category;
                    return loan;
                }
                else
                    return null;
            }
        }

        /// <summary>
        /// What is the year we are operating on?
        /// </summary>
        public static int Year 
        { 
            get
            {
                return _Year ?? throw new ApplicationException("Must set a year first");
            }
            set
            {
                _Year = value;
            }
        }
        private static int? _Year;

        /// <summary>
        /// How much jitter exactly is there in a given kind of amount jitter?
        /// </summary>
        /// <remarks>
        /// This is a +/- mutiplier on the transaction. e,g, for Moderate jitter, the
        /// actual amount will be between 60% and 140% of the target amount
        /// </remarks>
        public static Dictionary<JitterEnum, double> AmountJitterValues = new Dictionary<JitterEnum, double>()
        {
            { JitterEnum.None, 0 },
            { JitterEnum.Low, 0.1 },
            { JitterEnum.Moderate, 0.4 },
            { JitterEnum.High, 0.9 }
        };

        /// <summary>
        /// How much jitter exactly is there in a given kind of date jitter?
        /// </summary>
        /// <remarks>
        /// This expressed how large of range relative to the target period in
        /// which all the transactions should appear. e.g. for Low jitter,
        /// a "Monthly" pattern would generate transactions all within the same 7 days.
        /// </remarks>
        public static Dictionary<JitterEnum, double> DateJitterValues = new Dictionary<JitterEnum, double>()
        {
            { JitterEnum.None, 0 },
            { JitterEnum.Low, 0.25 },
            { JitterEnum.Moderate, 0.4 },
            { JitterEnum.High, 1.0 }
        };

        /// <summary>
        /// How many days are there in a given frequency?
        /// </summary>
        /// <remarks>
        /// This is public so the unit tests can access them
        /// </remarks>
        public static Dictionary<FrequencyEnum, TimeSpan> SchemeTimespans = new Dictionary<FrequencyEnum, TimeSpan>()
        {
            { FrequencyEnum.Weekly, TimeSpan.FromDays(7) },
            { FrequencyEnum.Monthly, TimeSpan.FromDays(28) },
            { FrequencyEnum.Quarterly, TimeSpan.FromDays(90) },
            { FrequencyEnum.Yearly, TimeSpan.FromDays(365) },
        };

        /// <summary>
        /// In case you forgot
        /// </summary>
        const int MonthsPerYear = 12;
        const int WeeksPerYear = 52;
        const int DaysPerWeek = 7;
        const int MonthsPerQuarter = 3;

        /// <summary>
        /// For a given frequency, how many transactions will be in a year?
        /// </summary>
        /// <remarks>
        /// This is public so the unit tests can access them
        /// </remarks>
        public static Dictionary<FrequencyEnum, int> FrequencyPerYear = new Dictionary<FrequencyEnum, int>()
        {
            { FrequencyEnum.Weekly, WeeksPerYear },
            { FrequencyEnum.SemiMonthly, 2 * MonthsPerYear },
            { FrequencyEnum.Monthly, MonthsPerYear },
            { FrequencyEnum.Quarterly, MonthsPerYear / MonthsPerQuarter },
            { FrequencyEnum.Yearly, 1 },
        };

        /// <summary>
        /// On what days exactly do the SemiWeekly transactions occur?
        /// </summary>
        private readonly int[] SemiWeeklyDays = new int[] { 1, 15 };

        decimal ISplitPattern.Amount => MakeAmount(AmountYearly / DateRepeats / FrequencyPerYear[DateFrequency]);

        /// <summary>
        /// Generate transactions for a given pattern (or group of patterns)
        /// </summary>
        /// <remarks>
        /// For a group of patterns, you'll need to pick a "main" pattern which gives the payee
        /// and date parameters. The group patterns will be used for amount and category.
        /// </remarks>
        /// <param name="group">Optional grouping of patterns to be turned into single transactions</param>
        /// <returns>The transactions generated</returns>
        public IEnumerable<Transaction> GetTransactions(IEnumerable<ISplitPattern> group = null)
        {
            //
            // Check for invalid parameter combinations
            //

            if (DateFrequency == FrequencyEnum.SemiMonthly && ((DateJitter != JitterEnum.None && DateJitter != JitterEnum.Invalid) || DateRepeats != 1))
                throw new NotImplementedException("SemiMonthly with date jitter or date repeats is not implemented");
            if (DateFrequency == FrequencyEnum.Invalid)
                throw new ApplicationException("Invalid date frequency");

            //
            // Choose a time window.
            //
            // The Window must be entirely within the Scheme Timespan, but chosen at random.
            // The size of the window is given by the Date Jitter.
            //

            if (DateFrequency != FrequencyEnum.SemiMonthly)
            {
                DateWindowLength = (DateJitter == JitterEnum.None) ? TimeSpan.FromDays(1) : SchemeTimespans[DateFrequency] * DateJitterValues[DateJitter];
                DateWindowStarts = TimeSpan.FromDays(random.Next(0, SchemeTimespans[DateFrequency].Days - DateWindowLength.Days));
            }

            //
            // Determine where we will get transaction split details from
            //

            ITransactionDetailsFactory detailsfactory;

            var loanobject = LoanObject;
            if (loanobject != null)
            {
                // Option #1: From our Loan if we have one
                detailsfactory = new LoanDetails(loanobject);
            }
            else if (group?.Any() == true)
            {
                // Option #2: From the supplied details group, if supplied
                detailsfactory = new GroupDetails(group);
            }
            else
            {
                // Option #3: From ourselves if nothing else applies
                detailsfactory = new SingleDetails(this);
            }

            //
            // Now generate transactions
            //

            if (DateFrequency == FrequencyEnum.SemiMonthly)
                return Enumerable.Range(1, MonthsPerYear).SelectMany(month => SemiWeeklyDays.Select(day => GenerateTransaction(detailsfactory.ForDate(new DateTime(Year, month, day)))));
            else
                return Enumerable.Repeat(1, DateRepeats).SelectMany(i=>Enumerable.Range(1, FrequencyPerYear[DateFrequency]).Select(x => GenerateTransaction(detailsfactory.ForDate(MakeDate(x)))));
        }

        /// <summary>
        /// Our own random number generator
        /// </summary>
        private static readonly Random random = new Random();

        /// <summary>
        /// For transactions generated in this pattern, what is the earliest day they can fall?
        /// </summary>
        private TimeSpan DateWindowStarts;

        /// <summary>
        /// For transactions generated in this pattern, how large of a possible dates is there?
        /// </summary>
        private TimeSpan DateWindowLength;

        /// <summary>
        /// Foundational generator. Actually generates the transaction
        /// </summary>
        /// <remarks>
        /// The thing that actually varies between different frequencies is the generation of the date.
        /// So, you figure that out and tell us.
        /// </remarks>
        /// <param name="timestamp">What exact timestamp to assign to this transaction</param>
        /// <param name="group">Optional grouping of patterns to be turned into single transactions</param>
        /// <returns>The transactions generated</returns>
        private Transaction GenerateTransaction(ITransactionDetails details) // IEnumerable<ISplitPattern> group, DateTime timestamp)
        {
            // I was tempted to refactor this by moving up to GetTransactions(). However,
            // it needs to be refigured every time to accomplish amount jitter
            var generatedsplits = details.Splits.Select(s => new Split()
            {
                Category = s.Category,
                Amount = s.Amount
            }).ToList();

            return new Transaction()
            {
                Payee = MakePayee,
                Splits = generatedsplits.Count > 1 ? generatedsplits : null,
                Timestamp = details.Date,
                Category = generatedsplits.Count == 1 ? generatedsplits.Single().Category : null,
                Amount = generatedsplits.Sum(x => x.Amount)
            };
        }

        /// <summary>
        /// The indivual payees which can be used in this transaction
        /// </summary>
        private IEnumerable<string> Payees => Payee.Split(',');

        /// <summary>
        /// Create a varied amount, based on the target amount specified, such that the
        /// jitter values are respected.
        /// </summary>
        /// <param name="amount">Target amount</param>
        /// <returns>Randomized amount within the desired ditter</returns>
        private decimal MakeAmount(decimal amount) =>
            Math.Round(
                (AmountJitter == JitterEnum.None) ? amount :
                    (decimal)((double)amount * (1.0 + 2.0 * (random.NextDouble() - 0.5) * AmountJitterValues[AmountJitter]))
                , 2);

        /// <summary>
        /// Create a date that fits within the frequency and date jitter parameters
        /// </summary>
        private DateTime MakeDate(int periodindex) => DateFrequency switch
            {
                FrequencyEnum.Monthly => new DateTime(Year, periodindex, 1),
                FrequencyEnum.Yearly => new DateTime(Year, 1, 1),
                FrequencyEnum.Quarterly => new DateTime(Year, periodindex * MonthsPerQuarter - 2, 1),
                FrequencyEnum.Weekly => new DateTime(Year, 1, 1) + TimeSpan.FromDays(DaysPerWeek * (periodindex - 1)),
                _ => throw new NotImplementedException()
            } 
            + DateWindowStarts 
            + ((DateJitter != JitterEnum.None) ? TimeSpan.FromDays(random.Next(0, DateWindowLength.Days)) : TimeSpan.Zero);

        /// <summary>
        /// Create a payee within the set specified
        /// </summary>
        private string MakePayee => Payees.Skip(random.Next(0, Payees.Count())).First();

        #region Helper Classes

        /// <summary>
        /// Explicitly provide the relatively small amount of information that a transaction split needs
        /// </summary>
        class SplitPattern : ISplitPattern
        {
            public decimal Amount { get; set; }

            public string Category { get; set; }
        }

        /// <summary>
        /// Defines the information needed to fill in the variable details of
        /// transaction creation
        /// </summary>
        interface ITransactionDetails
        {
            IEnumerable<ISplitPattern> Splits { get; }

            DateTime Date { get; }
        }

        class TransactionDetails : ITransactionDetails
        {
            public IEnumerable<ISplitPattern> Splits { get; set; }

            public DateTime Date { get; set; }
        }

        /// <summary>
        /// Defines the a source-agnostic approach to creating a source of transaction details
        /// for a given date.
        /// </summary>

        interface ITransactionDetailsFactory
        {
            /// <summary>
            /// Build a single set of transaction details for the given <paramref name="date"/>
            /// </summary>
            ITransactionDetails ForDate(DateTime date);
        }

        /// <summary>
        /// A transaction details factory which uses a single pattern as its basis
        /// </summary>
        class SingleDetails : ITransactionDetailsFactory
        {
            private readonly SampleDataPattern _single;

            public SingleDetails(SampleDataPattern single)
            {
                _single = single;
            }

            public ITransactionDetails ForDate(DateTime date)
            {
                return new TransactionDetails() { Date = date, Splits = Enumerable.Repeat(_single, 1) };
            }
        }

        /// <summary>
        /// A transaction details factory which uses a group of subordinate patterns as its basis
        /// </summary>
        class GroupDetails : ITransactionDetailsFactory
        {
            private readonly IEnumerable<ISplitPattern> _group;

            public GroupDetails(IEnumerable<ISplitPattern> group)
            {
                _group = group;
            }

            public ITransactionDetails ForDate(DateTime date)
            {
                return new TransactionDetails() { Date = date, Splits = _group };
            }
        }

        /// <summary>
        /// A transaction details factory which uses a loan as its basis
        /// </summary>

        class LoanDetails : ITransactionDetailsFactory
        {
            private readonly Loan _loan;

            public LoanDetails(Loan loan)
            {
                _loan = loan;
            }

            public ITransactionDetails ForDate(DateTime date)
            {
                return new TransactionDetails() { Date = date, Splits = _loan.PaymentSplitsForDate(date).Select(x => new SplitPattern() { Category = x.Key, Amount = x.Value }) };
            }
        }
        #endregion
    }

    /// <summary>
    /// Defines how frequently a pattern may occur
    /// </summary>
    public enum FrequencyEnum { Invalid = 0, Weekly, SemiMonthly, Monthly, Quarterly, Yearly };

    /// <summary>
    /// Describes the severity of jitter which may be applied to dates or amounts
    /// </summary>
    public enum JitterEnum { Invalid = 0, None, Low, Moderate, High };

    /// <summary>
    /// Defines the relatively small amount of information that a transaction split needs
    /// </summary>
    public interface ISplitPattern
    {
        decimal Amount { get; }

        string Category { get; }
    }
}
