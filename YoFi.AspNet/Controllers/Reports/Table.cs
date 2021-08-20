using System;
using System.Collections.Generic;
using System.Linq;

namespace YoFi.AspNet.Controllers.Reports
{
    /// <summary>
    /// Dictionary of (<typeparamref name="TColumn"/>,<typeparamref name="TRow"/>)
    /// to <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="TColumn">Class to represent each column</typeparam>
    /// <typeparam name="TRow">Class to represent each row</typeparam>
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

        /// <summary>
        /// Column labels
        /// </summary>
        /// <remarks>
        /// In sorted order
        /// </remarks>
        public IEnumerable<TColumn> ColumnLabels => _ColumnLabels.OrderBy(x => x);

        protected HashSet<TColumn> _ColumnLabels = new HashSet<TColumn>();

        /// <summary>
        /// Row labels
        /// </summary>
        /// <remarks>
        /// In sorted order
        /// </remarks>
        public IEnumerable<TRow> RowLabels => _RowLabels.OrderBy(x => x);

        protected HashSet<TRow> _RowLabels = new HashSet<TRow>();

        /// <summary>
        /// Value at this (C,R) position, or default
        /// </summary>
        /// <param name="collabel">Column label</param>
        /// <param name="rowlabel">Row label</param>
        /// <returns>Value at this (C,R) position, or default</returns>
        public TValue this[TColumn collabel, TRow rowlabel]
        {
            get
            {
                var key = new Key(_col: collabel, _row: rowlabel);

                return DataSet.GetValueOrDefault(key);
            }
            set
            {
                var key = new Key(_col: collabel, _row: rowlabel);

                DataSet[key] = value;
                _ColumnLabels.Add(collabel);
                _RowLabels.Add(rowlabel);
           }
        }
    }
}
