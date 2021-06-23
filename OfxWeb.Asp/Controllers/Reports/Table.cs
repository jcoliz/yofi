using System;
using System.Collections.Generic;
using System.Linq;

namespace OfxWeb.Asp.Controllers.Reports
{
    /// <summary>
    /// This is a standardized way for reports to be formulated. It is simply a
    /// 2D dictionary of C,R to V.
    /// </summary>
    /// <remarks>
    /// The idea is that the "pivot" view can render many different kinds of reports
    /// without knowing the details, because the details are all in here.
    /// </remarks>
    /// <typeparam name="TColumn">Class to represent each column</typeparam>
    /// <typeparam name="TRow">Class to represent each roww</typeparam>
    /// <typeparam name="TValue">Class to represent each cell value</typeparam>
    public class Table<TColumn, TRow, TValue>
    {
        class Key
        {
            public TColumn col { get; }
            public TRow row { get; }

            public Key(TColumn _col, TRow _row)
            {
                col = _col;
                row = _row;
            }

            public override bool Equals(object obj)
            {
                return obj is Key key &&
                       EqualityComparer<TColumn>.Default.Equals(col, key.col) &&
                       EqualityComparer<TRow>.Default.Equals(row, key.row);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(col, row);
            }
        }

        /// <summary>
        /// Primary representation of data.
        /// </summary>
        /// <remarks>
        /// This is essentially a 2D dictionary, and could perhaps be improved to simply be a
        /// SparseDictionary of (Row,Col) Tuple to Values.
        /// </remarks>
        Dictionary<Key,TValue> DataSet = new Dictionary<Key,TValue>();

        public IEnumerable<TColumn> ColumnLabels
        {
            get
            {
                return _ColumnLabels.OrderBy(x => x);
            }
            set
            {
                _ColumnLabels.Clear();
                foreach (var label in value)
                    _ColumnLabels.Add(label);
            }
        }
        protected HashSet<TColumn> _ColumnLabels = new HashSet<TColumn>();

        public IEnumerable<TRow> RowLabels
        {
            get
            {
                return _RowLabels.OrderBy(x => x);
            }
            set
            {
                _RowLabels.Clear();
                foreach (var label in value)
                    _RowLabels.Add(label);
            }
        }
        protected HashSet<TRow> _RowLabels = new HashSet<TRow>();

        public TValue this[TColumn collabel, TRow rowlabel]
        {
            get
            {
                var key = new Key(_col: collabel, _row: rowlabel);

                if (DataSet.ContainsKey(key))
                {
                    return DataSet[key];
                }
                else
                {
                    return default(TValue);
                }
            }
            set
            {
                var key = new Key(_col: collabel, _row: rowlabel);

                DataSet[key] = value;
                _ColumnLabels.Add(collabel);
                _RowLabels.Add(rowlabel);
           }
        }

        public IEnumerable<TValue> RowValues(TRow row)
        {
            return _ColumnLabels.Select(x => this[x, row]);
        }

        protected void RemoveColumnsWhere(Predicate<TColumn> predicate)
        {
            _ColumnLabels.RemoveWhere(predicate);
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

}
