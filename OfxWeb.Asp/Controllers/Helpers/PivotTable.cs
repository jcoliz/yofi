using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Controllers.Helpers
{
    /// <summary>
    /// This is a standardized way for reports to be formulated
    /// </summary>
    /// <remarks>
    /// The idea is that the "pivot" view can render many different kinds of reports
    /// without knowing the details, because the details are all in here.
    /// </remarks>
    /// <typeparam name="C">Class to represent each column</typeparam>
    /// <typeparam name="R">Class to represent each roww</typeparam>
    /// <typeparam name="V">Class to represent each cell value</typeparam>
    public class PivotTable<C, R, V>
    {
        /// <summary>
        /// Primary representation of data.
        /// </summary>
        /// <remarks>
        /// This is essentially a 2D dictionary, and could perhaps be improved to simply be a
        /// SparseDictionary of (Row,Col) Tuple to Values.
        /// </remarks>
        Dictionary<R, SparseDictionary<C, V>> Table = new Dictionary<R, SparseDictionary<C, V>>();


        public IEnumerable<C> ColumnLabels
        {
            get
            {
                return _ColumnLabels;
            }
            set
            {
                _ColumnLabels.Clear();
                foreach (var label in value)
                    _ColumnLabels.Add(label);
            }
        }
        HashSet<C> _ColumnLabels = new HashSet<C>();

        public IEnumerable<R> RowLabels => Table.Keys.OrderBy(x => x);

        public V this[C collabel, R rowlabel]
        {
            get
            {
                if (Table.ContainsKey(rowlabel))
                {
                    return Table[rowlabel][collabel];
                }
                else
                {
                    return default(V);
                }
            }
            set
            {
                if (!Table.ContainsKey(rowlabel))
                {
                    Table[rowlabel] = new SparseDictionary<C, V>();
                }
                var row = Table[rowlabel];

                row[collabel] = value;

                _ColumnLabels.Add(collabel);
            }
        }

        public IEnumerable<V> Row(R row)
        {
            return Table[row].Values;
        }
    }

    /// <summary>
    /// Label value used in rows and columns in a report
    /// </summary>
    /// <remarks>
    /// This should likely be separated to have a RowLabel class and a ColLabel class
    /// because there is a lot in here which is specific to rows
    /// </remarks>
    public class Label : IComparable<Label>
    {
        public int Order { get; set; }
        public string Value { get; set; } = string.Empty;
        public string SubValue { get; set; } = string.Empty;
        public string Key1 { get; set; } = string.Empty;
        public string Key2 { get; set; } = string.Empty;
        public string Key3 { get; set; } = string.Empty;
        public string Key4 { get; set; } = string.Empty;
        public bool Emphasis { get; set; } = false;
        public bool SuperHeading { get; set; } = false;
        public string Format { get; set; } = null;

        public int CompareTo(Label other)
        {
            if (Order == 0 && other.Order == 0)
            {
                if (Value == other.Value)
                {
                    if (SubValue == other.SubValue)
                        return (Key3 ?? String.Empty).CompareTo(other.Key3 ?? string.Empty);
                    else
                        return (SubValue ?? String.Empty).CompareTo(other.SubValue ?? string.Empty);
                }
                else
                    return Value.CompareTo(other.Value);
            }
            else
                return Order.CompareTo(other.Order);
        }

        public override bool Equals(object obj)
        {
            var other = obj as Label;

            if (other.Order == 0 && Order == 0)
                return Value.Equals(other.Value) && (SubValue ?? String.Empty).Equals(other.SubValue ?? String.Empty) && (Key3 ?? String.Empty).Equals(other.Key3 ?? String.Empty) == true;
            else
                return Order.Equals(other.Order);
        }

        public override int GetHashCode()
        {
            return Order == 0 ? Value.GetHashCode() : Order;
        }
    }

    /// <summary>
    /// A dictionary which will return a value for every key without having
    /// to HOLD values for every day.
    /// </summary>
    /// <typeparam name="K">Key type</typeparam>
    /// <typeparam name="V">Value type</typeparam>
    public class SparseDictionary<K, V> : Dictionary<K, V>
    {
        public new V this[K key]
        {
            get
            {
                return base.ContainsKey(key) ? base[key] : default(V);
            }
            set
            {
                base[key] = value;
            }
        }
    }
}
