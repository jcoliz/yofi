using System;
using System.Collections.Generic;
using System.Linq;

namespace YoFi.AspNet.Controllers.Reports
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
#if false
            // It seems we do not actually SET these.
            set
            {
                _ColumnLabels.Clear();
                foreach (var label in value)
                    _ColumnLabels.Add(label);
            }
#endif
        }
        protected HashSet<TColumn> _ColumnLabels = new HashSet<TColumn>();

        public IEnumerable<TRow> RowLabels
        {
            get
            {
                return _RowLabels.OrderBy(x => x);
            }
#if false
            // It seems we do not actually SET these.
            {
                _RowLabels.Clear();
                foreach (var label in value)
                    _RowLabels.Add(label);
            }
#endif
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

#if false
        // It seems we do not actually use these

        public IEnumerable<TValue> RowValues(TRow row)
        {
            return _ColumnLabels.Select(x => this[x, row]);
        }

        protected void RemoveColumnsWhere(Predicate<TColumn> predicate)
        {
            _ColumnLabels.RemoveWhere(predicate);
        }

#endif
    }

}
