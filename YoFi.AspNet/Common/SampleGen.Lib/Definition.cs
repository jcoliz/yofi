using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YoFi.AspNet.Models;

namespace YoFi.SampleGen
{
    public class Definition
    {
        public string Payee { get; set; }
        public SchemeEnum Scheme { get; set; }
        public JitterEnum DateJitter { get; set; }

        public string Category { get; set; }
        public decimal YearlyAmount { get; set; }
        public JitterEnum AmountJitter { get; set; }

        public string Group { get; set; }

        public static int Year { get; set; } = DateTime.Now.Year;

        public const int HowManyPerWeek = 3;

        public static Dictionary<JitterEnum, double> AmountJitterValues = new Dictionary<JitterEnum, double>()
        {
            { JitterEnum.None, 0 },
            { JitterEnum.Low, 0.1 },
            { JitterEnum.Moderate, 0.4 },
            { JitterEnum.High, 0.9 }
        };

        public static Dictionary<JitterEnum, double> DateJitterValues = new Dictionary<JitterEnum, double>()
        {
            { JitterEnum.None, 0 },
            { JitterEnum.Low, 0.25 },
            { JitterEnum.Moderate, 0.4 },
            { JitterEnum.High, 1.0 }
        };

        public static Dictionary<SchemeEnum, TimeSpan> SchemeTimespans = new Dictionary<SchemeEnum, TimeSpan>()
        {
            { SchemeEnum.Weekly, TimeSpan.FromDays(7) },
            { SchemeEnum.ManyPerWeek, TimeSpan.FromDays(7) },
            { SchemeEnum.Monthly, TimeSpan.FromDays(28) },
            { SchemeEnum.Quarterly, TimeSpan.FromDays(90) },
            { SchemeEnum.Yearly, TimeSpan.FromDays(365) },
        };

        public static Dictionary<SchemeEnum, int> SchemeNumPeriods = new Dictionary<SchemeEnum, int>()
        {
            { SchemeEnum.ManyPerWeek, 52 * HowManyPerWeek },
            { SchemeEnum.Weekly, 52 },
            { SchemeEnum.SemiMonthly, 24 },
            { SchemeEnum.Monthly, 12 },
            { SchemeEnum.Quarterly, 4 },
            { SchemeEnum.Yearly, 1 },
        };

        private readonly int[] SemiWeeklyDays = new int[] { 1, 15 };

        public IEnumerable<Transaction> GetTransactions(IEnumerable<Definition> insplits = null)
        {
            // Many Per Week overrides the date jitter to high
            if (Scheme == SchemeEnum.ManyPerWeek)
                DateJitter = JitterEnum.High;

            // Check for invalid parameter combinations
            if (Scheme == SchemeEnum.SemiMonthly && DateJitter != JitterEnum.None && DateJitter != JitterEnum.Invalid)
                throw new NotImplementedException("SemiMonthly with date jitter is not implemented");

            // Randomly choose a window. The Window must be entirely within the Scheme Timespan, but chosen at random.
            // The size of the window is given by the Date Jitter.
            if (Scheme != SchemeEnum.SemiMonthly)
            {
                DateWindowLength = (DateJitter == JitterEnum.None) ? TimeSpan.FromDays(1) : SchemeTimespans[Scheme] * DateJitterValues[DateJitter];
                DateWindowStarts = TimeSpan.FromDays(random.Next(0, SchemeTimespans[Scheme].Days - DateWindowLength.Days));
            }

            Payees = Payee.Split(",").ToList();

            var splits = insplits ?? new List<Definition> { this };

            return Scheme switch
            {
                SchemeEnum.Invalid => throw new ApplicationException("Invalid scheme"),
                SchemeEnum.Yearly => GenerateTypical(splits),
                SchemeEnum.Monthly => GenerateTypical(splits),
                SchemeEnum.Quarterly => GenerateTypical(splits),
                SchemeEnum.Weekly => GenerateTypical(splits),
                SchemeEnum.SemiMonthly => GenerateSemiMonthly(splits),
                SchemeEnum.ManyPerWeek => GenerateManyPerWeek(splits),
                _ => throw new NotImplementedException()
            };
        }

        private static Random random = new Random();
        private TimeSpan DateWindowStarts;
        private TimeSpan DateWindowLength;

        private IEnumerable<Transaction> GenerateTypical(IEnumerable<Definition> splits) =>
            Enumerable.Range(1, SchemeNumPeriods[Scheme]).Select(x => GenerateTypicalTransaction(x, splits));

        private IEnumerable<Transaction> GenerateManyPerWeek(IEnumerable<Definition> splits) =>
            Enumerable.Range(1, HowManyPerWeek).SelectMany(x => Enumerable.Range(1, 52).Select(w => GenerateTypicalTransaction(w, splits))).OrderBy(x => x.Timestamp);

        private IEnumerable<Transaction> GenerateSemiMonthly(IEnumerable<Definition> splits) =>
            Enumerable.Range(1, 12).SelectMany(month => SemiWeeklyDays.Select(day => GenerateBaseTransaction(splits, new DateTime(Year, month, day))));

        private Transaction GenerateTypicalTransaction(int index, IEnumerable<Definition> splits) =>
            GenerateBaseTransaction(splits,
                Scheme switch
                {
                    SchemeEnum.Monthly => new DateTime(Year, index, 1),
                    SchemeEnum.Yearly => new DateTime(Year, 1, 1),
                    SchemeEnum.Quarterly => new DateTime(Year, index * 3 - 2, 1),
                    SchemeEnum.ManyPerWeek => new DateTime(Year, 1, 1) + TimeSpan.FromDays(7 * (index - 1)),
                    SchemeEnum.Weekly => new DateTime(Year, 1, 1) + TimeSpan.FromDays(7 * (index - 1)),
                    _ => throw new NotImplementedException()
                } + JitterizedDate
            );

        private Transaction GenerateBaseTransaction(IEnumerable<Definition> splits, DateTime timestamp)
        {
            var generatedsplits = splits.Select(s => new Split()
            {
                Category = s.Category,
                Amount = s.JitterizeAmount(s.YearlyAmount / SchemeNumPeriods[Scheme])
            }).ToList();

            return new Transaction()
            {
                Payee = JitterizedPayee,
                Splits = generatedsplits.Count > 1 ? generatedsplits : null,
                Timestamp = timestamp,
                Category = generatedsplits.Count == 1 ? generatedsplits.Single().Category : null,
                Amount = generatedsplits.Sum(x => x.Amount)
            };
        }

        private List<string> Payees;

        private decimal JitterizeAmount(decimal amount) =>
            (AmountJitter == JitterEnum.None) ? amount :
                (decimal)((double)amount * (1.0 + 2.0 * (random.NextDouble() - 0.5) * AmountJitterValues[AmountJitter]));

        private TimeSpan JitterizedDate => 
            DateWindowStarts + ((DateJitter != JitterEnum.None) ? TimeSpan.FromDays(random.Next(0, DateWindowLength.Days)) : TimeSpan.Zero);

        private string JitterizedPayee => Payees[random.Next(0, Payees.Count)];
    }

    public enum SchemeEnum { Invalid = 0, ManyPerWeek, Weekly, SemiMonthly, Monthly, Quarterly, Yearly };
    public enum JitterEnum { Invalid = 0, None, Low, Moderate, High };
}
