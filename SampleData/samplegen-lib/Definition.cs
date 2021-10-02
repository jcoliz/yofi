﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public IEnumerable<Transaction> GetTransactions(IEnumerable<Definition> splits = null)
        {
            // Many Per Week overrides the date jitter to high
            if (Scheme == SchemeEnum.ManyPerWeek)
                DateJitter = JitterEnum.High;

            SetDateWindow();

            return Scheme switch
            {
                SchemeEnum.Invalid => throw new ApplicationException("Invalid scheme"),
                SchemeEnum.Yearly => GenerateTypical(),
                SchemeEnum.Monthly => GenerateTypical(),
                SchemeEnum.Quarterly => GenerateTypical(),
                SchemeEnum.Weekly => GenerateTypical(),
                SchemeEnum.SemiMonthly => GenerateSemiMonthly(splits ?? new List<Definition> { this }),
                SchemeEnum.ManyPerWeek => GenerateManyPerWeek(),
                _ => throw new NotImplementedException()
            };
        }
        
        public static int Year { get; set; } = DateTime.Now.Year;

        private static Random random = new Random();

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

        private TimeSpan DateWindowStarts;
        private TimeSpan DateWindowLength;

        private Transaction GenerateOneTransaction(int index, int numperiods)
        {
            var result = new Transaction() { Amount = JitterizeAmount(YearlyAmount / numperiods), Category = Category, Payee = Payee };

            result.Timestamp = Scheme switch
            {
                SchemeEnum.Monthly => new DateTime(Year, index, 1) + JitterizedDate,
                SchemeEnum.Yearly => new DateTime(Year, 1, 1) + JitterizedDate,
                SchemeEnum.Quarterly => new DateTime(Year, index * 3 - 2, 1) + JitterizedDate,
                SchemeEnum.ManyPerWeek => new DateTime(Year, 1, 1) + TimeSpan.FromDays(7 * (index-1)) + JitterizedDate,
                SchemeEnum.Weekly => new DateTime(Year, 1, 1) + TimeSpan.FromDays(7 * (index-1)) + JitterizedDate,
                _ => throw new NotImplementedException()
            };

            return result;
        }

        private IEnumerable<Transaction> GenerateTypical()
        {
            var periods = SchemeNumPeriods[Scheme];
            return Enumerable.Range(1, periods).Select(x => GenerateOneTransaction(x, periods));
        }

        private IEnumerable<Transaction> GenerateManyPerWeek() =>
            Enumerable.Repeat(0, HowManyPerWeek).SelectMany(x => Enumerable.Range(1, 52).Select(x => GenerateOneTransaction(x, 52 * HowManyPerWeek))).OrderBy(x => x.Timestamp);

        private IEnumerable<Transaction> GenerateSemiMonthly(IEnumerable<Definition> splits)
        {
            // The "Splits" give us category and amount

            if (DateJitter != JitterEnum.None && DateJitter != JitterEnum.Invalid)
                throw new NotImplementedException("SemiMonthly with date jitter is not implemented");

            var days = new int[] { 1, 15 };

            return Enumerable.Range(1, 12).SelectMany
            (
                month => 
                days.Select
                ( 
                    day =>                
                    new Transaction() 
                    { 
                        Payee = Payee, 
                        Timestamp = new DateTime(Year, month, day),
                        Splits = splits.Select(s=>new CategoryAmount() 
                        { 
                            Category = s.Category, 
                            Amount = s.JitterizeAmount(s.YearlyAmount/24) 
                        }).ToList()
                    }
                )
            );
        }

        private decimal JitterizeAmount(decimal amount)
        {
            if (AmountJitter != JitterEnum.None)
            {
                var amountjittervalue = AmountJitterValues[AmountJitter];
                amount = (decimal)((double)amount * (1.0 + 2.0 * (random.NextDouble() - 0.5) * amountjittervalue));
            }

            return amount;
        }

        private void SetDateWindow()
        {
            // No date windows for semimonthly
            if (Scheme == SchemeEnum.SemiMonthly)
                return;

            // Randomly choose a window. The Window must be entirely within the Scheme Timespan, but chosen at random.
            // The size of the window is given by the Date Jitter.

            DateWindowLength = TimeSpan.FromDays(1);
            if (DateJitter != JitterEnum.None)
            {
                DateWindowLength = SchemeTimespans[Scheme] * DateJitterValues[DateJitter];
            }
            DateWindowStarts = TimeSpan.FromDays(random.Next(0, SchemeTimespans[Scheme].Days - DateWindowLength.Days));
        }

        private TimeSpan JitterizedDate => DateWindowStarts + ((DateJitter != JitterEnum.None) ? TimeSpan.FromDays(random.Next(0, DateWindowLength.Days)) : TimeSpan.Zero);

    }

    public enum SchemeEnum { Invalid = 0, ManyPerWeek, Weekly, SemiMonthly, Monthly, Quarterly, Yearly };
    public enum JitterEnum { Invalid = 0, None, Low, Moderate, High };
}
