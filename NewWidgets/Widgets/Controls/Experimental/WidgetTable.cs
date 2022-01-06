using System.Numerics;
using System.Collections.Generic;
using System;

using NewWidgets.Utility;
using NewWidgets.UI;

namespace NewWidgets.Widgets
{
    /// <summary>
    /// Generic type to represent cell value that have different display text and sort value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SortWrapper<T> : IComparable
    {
        private readonly T Value; // sorting value
        private readonly string Text; // display name

        public SortWrapper(T value, string text)
        {
            Value = value;
            Text = text;
        }

        public int CompareTo(object obj)
        {
            if (obj == null || !(obj is SortWrapper<T>))
                return -1;

            object value = ((SortWrapper<T>)obj).Value;

            return Comparer<object>.Default.Compare(Value, value);
        }

        public override string ToString()
        {
            return Text;
        }
    }


    /// <summary>
    /// Generic specialization to use WidgetTable with string-based columns
    /// </summary>
    public class WidgetTable : WidgetTable<string>
    {
    }

    /// <summary>
    /// Container class capable of showing table with scroll and fixed size columns
    /// Rows are displayed as set of WidgetText cells with flexible height
    /// </summary>
    public class WidgetTable<TColumnIdentifier> : WidgetScrollView
    {
        private static readonly uint s_headerRowIndex = uint.MaxValue;

        private readonly List<WidgetTableRow> m_rows;
        private readonly TableLayout m_layout;

        private ITableDataReader m_dataReader = null;

        // styling

        private bool m_showHeader;
        private bool m_allowSort;

        // Simple styling. TODO: change to different styles

        private WidgetStyleSheet m_headerRowStyle;
        private WidgetStyleSheet m_rowStyle;
        private WidgetStyleSheet m_alternateRowStyle;

        private string m_upSortString;
        private string m_downSortString;
        private Margin m_margin;

        // Internal 
        private int m_currentSortDirection;
        private TColumnIdentifier m_currentSortField;

       
        public bool ShowHeader
        {
            get { return m_showHeader; }
            set { m_showHeader = value; }
        }

        public bool AllowSort
        {
            get { return m_allowSort; }
            set { m_allowSort = value; }
        }

        public WidgetStyleSheet RowStyle
        {
            get { return m_rowStyle; }
            set { m_rowStyle = value; }
        }

        public WidgetStyleSheet AlternateRowStyle
        {
            get { return m_alternateRowStyle; }
            set { m_alternateRowStyle = value; }
        }

        public WidgetStyleSheet HeaderRowStyle
        {
            get { return m_headerRowStyle; }
            set { m_headerRowStyle = value; }
        }

        public string UpSortString
        {
            get { return m_upSortString; }
            set { m_upSortString = value; }
        }

        public string DownSortString
        {
            get { return m_downSortString; }
            set { m_downSortString = value; }
        }

        public Margin Margin
        {
            get { return m_margin; }
            set { m_margin = value; }
        }

        public float RowHeight
        {
            get { return m_layout.RowHeight; }
            set { m_layout.RowHeight = value; }
        }

        public Margin RowPadding
        {
            get { return m_layout.RowPadding; }
            set { m_layout.RowPadding = value; }
        }

        public uint[] SelectedIndices
        {
            get
            {
                List<uint> result = new List<uint>();

                foreach (WidgetTableRow row in m_rows)
                    if (row.Selected)
                        result.Add(row.Id);

                return result.ToArray();
            }
            set
            {
                Dictionary<uint, bool> toSelect = new Dictionary<uint, bool>();

                foreach (uint index in value)
                    toSelect[index] = true;

                toSelect.Remove(s_headerRowIndex); // do not select header

                foreach (WidgetTableRow row in m_rows)
                    row.Selected = toSelect.ContainsKey(row.Id);
            }
        }

        protected bool Inited
        {
            get { return m_layout.Columns.Count > 0; }
        }

        public event Func<uint, TColumnIdentifier, bool> OnRowClick;

        public WidgetTable(WidgetStyleSheet style = default(WidgetStyleSheet))
            : base(style.IsEmpty ? DefaultStyle : style)
        {
            m_layout = new TableLayout();
            m_rows = new List<WidgetTableRow>();

            // styling
            m_showHeader = true;
            m_allowSort = false;
            m_upSortString = "{0} >";
            m_downSortString = "{0} <";
            m_margin = new Margin(1); // TODO: read from style?

            // misc
            AddHeaderRow();
        }

        public ColumnLayout AddColumn(TColumnIdentifier columnId, string header, string tooltip, float maxWidth,
                            WidgetAlign align = WidgetAlign.Top | WidgetAlign.Left, bool noWrap = false, bool noSort = false, bool custom = false)
        {
            ColumnLayout column = new ColumnLayout(columnId, maxWidth, align, noWrap, noSort, custom, header, tooltip);
            m_layout.AddColumn(column);

            InvalidateLayout();

            return column;
        }

        public ColumnLayout GetColumn(TColumnIdentifier columnId)
        {
            return m_layout.GetColumn(columnId);
        }

        public void RemoveColumn(TColumnIdentifier columnId)
        {
            m_layout.RemoveColumn(columnId);

            InvalidateLayout();
        }

        #region Data binding

        public void SetDataReader(ITableDataReader reader)
        {
            Clear();

            AddHeaderRow(false);

            m_dataReader = reader;

            InvalidateLayout();
        }

        public void SetData(System.Collections.IList data)
        {
            SetDataReader(new ListDataReader(data));
        }

        [Obsolete]
        public void AddRow(ITableDataRow data)
        {
            if (m_dataReader != null && !(m_dataReader is RowDataReader))
                throw new WidgetException("Asked to add ITableDataRow when ITableDataReader is already created!");

            if (m_dataReader == null)
                SetDataReader(new RowDataReader());

            ((RowDataReader)m_dataReader).AddRow(data);
            DoAddRow(data.Id, m_dataReader);
        }

        #endregion

        private WidgetTableRow AddHeaderRow(bool invalidate = true)
        {
            return DoAddRow(s_headerRowIndex, new HeaderDataReader(m_layout), invalidate);
        }

        /// <summary>
        /// Sorts the table for specified column and direction
        /// </summary>
        /// <param name="columnId">Id of the column</param>
        /// <param name="direction">-1, 0 or 1</param>
        public void Sort(TColumnIdentifier columnId, int direction)
        {
            if (m_currentSortField != null)
                GetColumn(m_currentSortField).HeaderSymbol = string.Empty;

            m_currentSortField = columnId;
            m_currentSortDirection = direction;

            if (m_currentSortDirection != 0)
                GetColumn(m_currentSortField).HeaderSymbol = m_currentSortDirection == -1 ? m_upSortString : m_downSortString;

            InvalidateLayout();
        }

        protected virtual bool HandleRowClick(uint rowId, TColumnIdentifier columnId)
        {
            if (rowId == s_headerRowIndex)
            {
                if (!GetColumn(columnId).NoSort)
                {
                    int direction = !columnId.Equals(m_currentSortField) ? 1 // sort by click. let it be 1 by default
                        : (m_currentSortDirection + 2) % 3 - 1; // direction values are -1, 0, 1, that formula changes it to 0, 1, 2, increments the index and then shifts back

                    Sort(columnId, direction);
                }
                return true;
            }

            if (OnRowClick != null)
                return OnRowClick(rowId, columnId);

            return false;
        }

        public void SetSort(TColumnIdentifier columnId, int direction)
        {
            if (m_currentSortField != null)
                GetColumn(m_currentSortField).HeaderSymbol = string.Empty;

            ColumnLayout targetColumn = GetColumn(columnId);

            // no need to check - GetColumn will raise an exception if column not found

            m_currentSortField = columnId;
            m_currentSortDirection = direction;

            if (m_currentSortDirection != 0 && targetColumn != null)
                targetColumn.HeaderSymbol = m_currentSortDirection == -1 ? m_upSortString : m_downSortString;

            InvalidateLayout();
        }

        public void UpdateRow(uint id)
        {
            // TODO: update particular row without redrawing everything!

            InvalidateLayout();
        }

        [Obsolete("Don't access row data directly!")]
        public ITableDataRow GetRow(uint id)
        {
            //foreach (WidgetTableRow row in m_rows)
            //    if (row.Id == id)
            //    {
            //        return row.Data;
            //    }

            return null;
        }

        public void Clear()
        {
            foreach (WidgetTableRow row in m_rows)
                row.Remove();

            m_rows.Clear();

            InvalidateLayout();
        }

        [Obsolete("Don't remove a row without removing it from from DataReader")]
        protected void RemoveRow(uint id)
        {
            if (id == s_headerRowIndex)
                throw new WidgetException("Header row should not be removed");

            for (int i = 0; i < m_rows.Count; i++)
                if (m_rows[i].Id == id)
                {
                    m_rows[i].Remove();
                    m_rows.RemoveAt(i);

                    InvalidateLayout();
                    return;
                }
        }

        /// <summary>
        /// This method marks the widget for full relayout including adding
        /// new rows from DataReader and removing absent ones.
        /// </summary>
        //public void InvalidateLayout()

        private WidgetTableRow DoAddRow(uint rowId, ITableDataReader reader, bool invalidate = true)
        {
            WidgetTableRow row = new WidgetTableRow(rowId, reader);

            if (rowId == s_headerRowIndex)
                m_rows.Insert(0, row);
            else
                m_rows.Add(row);

            row.OnCellClick += (cell) => HandleRowClick(rowId, cell);

            AddChild(row);

            if (invalidate)
                InvalidateLayout();

            return row;
        }

        public override void UpdateLayout()
        {
            int index = -1;

            // We do a search for removed or added rows here with Dictionary as a helper
            Dictionary<uint, WidgetTableRow> toRemove = new Dictionary<uint, WidgetTableRow>();

            // Add all old values
            foreach (WidgetTableRow row in m_rows)
                toRemove.Add(row.Id, row);

            if (m_dataReader != null)
            {
                foreach (uint rowId in m_dataReader.Rows)
                {
                    // Remove from collection everything that's already present
                    if (toRemove.ContainsKey(rowId))
                    {
                        toRemove.Remove(rowId);
                        continue;
                    }

                    // Add new rows
                    DoAddRow(rowId, m_dataReader);
                }
            }

            if (m_showHeader)
                toRemove.Remove(s_headerRowIndex);

            // Remove everything that left
            foreach (WidgetTableRow row in toRemove.Values)
            {
                row.Remove();
                m_rows.Remove(row);
            }

            // Sort the collection. Doesn't affect the header
            if (m_currentSortDirection != 0)
                m_rows.Sort(SortComparison);
            

            float position = m_margin.Top;

            foreach (WidgetTableRow row in m_rows)
            {
                WidgetStyleSheet style;

                if (row.Id == s_headerRowIndex)
                {
                    style = m_headerRowStyle;

                    if (!m_showHeader)
                    {
                        row.Visible = false;
                        continue;
                    }
                }
                else
                {
                    style = index % 2 == 0 ? m_rowStyle : m_alternateRowStyle;
                }

                row.Visible = true;
                row.UpdateStyle(m_layout, style);
                row.Position = new Vector2(m_margin.Left, position);

                position += row.Size.Y;
                index++;
            }

            this.ContentSize = new Vector2(m_layout.RowWidth + m_margin.Width, position + m_margin.Bottom);

            base.UpdateLayout();
        }

        private int SortComparison(WidgetTableRow x, WidgetTableRow y)
        {
            return y.CompareTo(x, m_currentSortField) * m_currentSortDirection;
        }

        #region Layouts

        /// <summary>
        /// This class stores basic data for the column - id, align, hedear, width
        /// </summary>
        public class ColumnLayout
        {
            private readonly TColumnIdentifier m_id;

            private readonly WidgetAlign m_textAlign;
            private readonly bool m_noWrap;
            private readonly bool m_noSort;
            private readonly bool m_custom;

            private float m_width;

            private string m_header;
            private string m_headerSymbol;
            private string m_tooltip;

            public TColumnIdentifier Id
            {
                get { return m_id; }
            }

            public float Width
            {
                get { return m_width; }
                set { m_width = value; }
            }

            public WidgetAlign TextAlign
            {
                get { return m_textAlign; }
            }

            public bool NoWrap
            {
                get { return m_noWrap; }
            }

            public bool NoSort
            {
                get { return m_noSort; }
            }

            public bool Custom
            {
                get { return m_custom; }
            }

            public string Header
            {
                get { return m_header; }
                set { m_header = value; }
            }

            public string HeaderSymbol
            {
                get { return m_headerSymbol; }
                set { m_headerSymbol = value; }
            }

            public string FullName
            {
                get { return m_header + m_headerSymbol; }
            }

            public string Tooltip
            {
                get { return m_tooltip; }
                set { m_tooltip = value; }
            }

            public ColumnLayout(TColumnIdentifier id, float width, WidgetAlign align, bool noWrap, bool noSort, bool custom, string header, string tooltip, string headerSymbol = "")
            {
                m_id = id;
                m_width = width;
                m_textAlign = align;
                m_noWrap = noWrap;
                m_noSort = noSort;
                m_custom = custom;
                m_header = header;
                m_tooltip = tooltip;
                m_headerSymbol = headerSymbol;
            }
        }

        /// <summary>
        /// This class stores data for all the columns and some generic data
        /// alike row height and padding
        /// </summary>
        public class TableLayout
        {
            private readonly List<ColumnLayout> m_columns;

            private float m_minRowHeight;
            private Margin m_rowPadding;

            public IList<ColumnLayout> Columns
            {
                get { return m_columns; }
            }

            public float RowHeight
            {
                get { return m_minRowHeight; }
                set { m_minRowHeight = value; }
            }

            public Margin RowPadding
            {
                get { return m_rowPadding; }
                set { m_rowPadding = value; }
            }

            /// <summary>
            /// Row disth is calculated from all the column widths
            /// </summary>
            public float RowWidth
            {
                get
                {
                    float totalWidth = 0;

                    for (int i = 0; i < m_columns.Count; i++)
                        totalWidth += m_columns[i].Width;

                    // TODO: do we need to add row padding here?

                    return totalWidth + m_rowPadding.Width;
                }
            }

            public TableLayout()
            {
                m_columns = new List<ColumnLayout>();
            }

            public void AddColumn(ColumnLayout column)
            {
                m_columns.Add(column);
            }

            public void RemoveColumn(TColumnIdentifier id)
            {
                for (int i = 0; i < m_columns.Count; i++)
                    if (m_columns[i].Id.Equals(id))
                    {
                        m_columns.RemoveAt(i);
                        return;
                    }
            }

            public ColumnLayout GetColumn(TColumnIdentifier id)
            {
                for (int i = 0; i < m_columns.Count; i++)
                    if (m_columns[i].Id.Equals(id))
                        return m_columns[i];

                throw new WidgetException("Column " + id + " not found!");
            }
        }

        #endregion

        #region Rows

        private class WidgetTableRow : WidgetPanel
        {
            private readonly uint m_id;
            private readonly ITableDataReader m_reader;

            private readonly IDictionary<TColumnIdentifier, object> m_lastData;
            private readonly IDictionary<TColumnIdentifier, string> m_lastTooltips;
            private readonly IDictionary<TColumnIdentifier, Widget> m_cells;

            public uint Id
            {
                get { return m_id; }
            }

            public event Func<TColumnIdentifier, bool> OnCellClick;

            public WidgetTableRow(uint id, ITableDataReader reader)
                : base(Widget.DefaultStyle)
            {
                m_id = id;
                m_reader = reader;
                m_cells = new Dictionary<TColumnIdentifier, Widget>();
                m_lastData = new Dictionary<TColumnIdentifier, object>();
                m_lastTooltips = new Dictionary<TColumnIdentifier, string>();
            }

            private IDictionary<TColumnIdentifier, object> EnsureData(TableLayout layout)
            {
                m_lastData.Clear();
                m_lastTooltips.Clear();

                foreach (ColumnLayout column in layout.Columns)
                {
                    object value;
                    string tooltip;
                    m_reader.GetCell(m_id, column.Id, out value, out tooltip);

                    m_lastData[column.Id] = value;
                    m_lastTooltips[column.Id] = tooltip;
                }

                return m_lastData;
            }

            public void UpdateStyle(TableLayout layout, WidgetStyleSheet style)
            {
                this.Size = new Vector2(layout.RowWidth + layout.RowPadding.Width, layout.RowHeight + layout.RowPadding.Height);
                //this.LoadStyle(WidgetState.Normal, style);

                var data = EnsureData(layout);

                float position = layout.RowPadding.Left;

                // in some rare cases column structure could change so we need to make sure that only cells
                // corresponding to layout.Columns and with non-zero width are displayed
                Dictionary<TColumnIdentifier, Widget> toRemove = new Dictionary<TColumnIdentifier, Widget>(m_cells);

                foreach (ColumnLayout column in layout.Columns)
                {
                    if (column.Width <= 0) // 0 width means invisible
                        continue;

                    object cellValue;
                    data.TryGetValue(column.Id, out cellValue);

                    Widget cell;

                    if (!m_cells.TryGetValue(column.Id, out cell))
                        cell = CreateCell(column, layout.RowHeight, cellValue);
                    else
                        UpdateCell(cell, column, layout.RowHeight, cellValue);

                    cell.Position = new Vector2(position, layout.RowPadding.Top);

                    string tooltip = null;
                    if (m_lastTooltips != null)
                        m_lastTooltips.TryGetValue(column.Id, out tooltip);

                    cell.Tooltip = tooltip;

                    if (cell.Size.Y > Size.Y)
                        Size = new Vector2(Size.X, cell.Size.Y + layout.RowPadding.Height);

                    position += column.Width;// + layout.RowPadding.Right;

                    toRemove.Remove(column.Id);
                }

                // remove all cells that wasn't updated
                foreach (KeyValuePair<TColumnIdentifier, Widget> pair in toRemove)
                {
                    Widget cell = pair.Value;
                    if (cell != null)
                        cell.Remove();

                    m_cells.Remove(pair.Key);
                }
            }

            protected virtual void UpdateCell(Widget cell, ColumnLayout column, float height, object value)
            {
                if (cell is WidgetText)
                {
                    if (column.NoWrap)
                    {
                        cell.Size = new Vector2(column.Width, height);
                        if (cell is WidgetText)
                            ((WidgetText)cell).MaxWidth = 0;
                    }
                    else
                    {
                        cell.Size = new Vector2(0, height);
                        if (cell is WidgetText)
                            ((WidgetText)cell).MaxWidth = column.Width;
                    }

                    //cell.LoadStyle(WidgetStyleType.Normal, style);

                    ((WidgetText)cell).Text = value == null ? "" : value.ToString();
                    ((WidgetText)cell).TextAlign = column.TextAlign;
                    ((WidgetText)cell).UpdateLayout();
                    return;
                }

                WidgetPanel panel = cell as WidgetPanel;
                if (panel == null) // should never happen
                    return;

                if (panel.Children[0] != value)
                {
                    panel.Clear();

                    if (value != null)
                        panel.AddChild((Widget)value);
                }
                else
                {
                    if (value != null && ((Widget)value).Size.Y > height)
                        panel.Size = new Vector2(column.Width, ((Widget)value).Size.Y);
                    else
                        panel.Size = new Vector2(column.Width, height);
                }

                ((Widget)value).Size = panel.Size;
            }

            protected virtual Widget CreateCell(ColumnLayout column, float height, object value)
            {
                Widget result;

                if (column.Custom && value is Widget)
                {
                    WidgetPanel panel = new WidgetPanel(Widget.DefaultStyle);
                    panel.Size = new Vector2(column.Width, height);
                    ((Widget)value).Size = panel.Size;
                    panel.AddChild((Widget)value);

                    result = panel;
                }
                else
                {
                    WidgetText textWidget = new WidgetText(value == null ? "" : value.ToString());
                    if (column.NoWrap)
                    {
                        textWidget.Size = new Vector2(column.Width, height);
                        textWidget.MaxWidth = 0;
                    }
                    else
                    {
                        textWidget.Size = new Vector2(0, height);
                        textWidget.MaxWidth = column.Width;
                    }

                    textWidget.TextAlign = column.TextAlign;

                    result = textWidget;
                }

                m_cells[column.Id] = result;

                result.OnTouch += delegate (float x, float y, bool press, bool unpress, int pointer)
                {
                    if (unpress || (press && WindowController.Instance.IsTouchScreen))
                    {
                        if (OnCellClick != null)
                            return OnCellClick(column.Id);
                    }
                    return false;
                };

                AddChild(result);
                return result;
            }

            public int CompareTo(WidgetTableRow another, TColumnIdentifier sortField)
            {
                object thisValue;
                string tooltip;
                m_reader.GetCell(m_id, sortField, out thisValue, out tooltip);

                if (thisValue == null)
                    return 0;

                object anotherValue;
                another.m_reader.GetCell(m_id, sortField, out anotherValue, out tooltip);

                if (anotherValue == null)
                    return 1;

                if (thisValue is IComparable && anotherValue.GetType() == thisValue.GetType())
                    return ((IComparable)thisValue).CompareTo(anotherValue);

                return string.Compare(thisValue.ToString(), anotherValue.ToString());
            }
        }
        #endregion

        #region Inner classes

        /// <summary>
        /// Primary interface for submitting data to WidgetTable
        /// </summary>
        public interface ITableDataReader
        {
            IEnumerable<uint> Rows { get; }

            bool GetCell(uint rowId, TColumnIdentifier column, out object value, out string tooltip);
        }

        /// <summary>
        /// Interface providing custom data and tooltip for the object
        /// </summary>
        public interface ITableDataSource
        {
            bool GetData(TColumnIdentifier column, out object value, out string tooltip);
        }

        public class ListDataReader : ITableDataReader
        {
            private readonly System.Collections.IList m_data;

            public IEnumerable<uint> Rows
            {
                get
                {
                    System.Collections.IEnumerator e = m_data.GetEnumerator();
                    uint i = 0;

                    while (e.MoveNext())
                        yield return i++;
                }
            }

            public ListDataReader(System.Collections.IList data)
            {
                m_data = data;
            }

            public bool GetCell(uint rowId, TColumnIdentifier column, out object value, out string tooltip)
            {
                if (rowId >= m_data.Count)
                    throw new WidgetException(string.Format("ListDataReader for {0} asked to retrieve data for row {1} of {2}!", m_data.GetType(), rowId, m_data.Count));

                object data = m_data[(int)rowId];

                if (data is ITableDataSource)
                {
                    ((ITableDataSource)data).GetData(column, out value, out tooltip);
                }
                else
                {
                    value = data;
                    tooltip = data == null ? null : data.ToString();
                }
                return true;
            }
        }

        public class NameValueDataReader : ITableDataReader
        {
            private readonly System.Collections.Specialized.NameValueCollection m_data;

            public IEnumerable<uint> Rows
            {
                get
                {
                    System.Collections.IEnumerator e = m_data.GetEnumerator();
                    uint i = 0;

                    while (e.MoveNext())
                        yield return i++;
                }
            }

            public NameValueDataReader(System.Collections.Specialized.NameValueCollection data)
            {
                m_data = data;
            }

            public bool GetCell(uint rowId, TColumnIdentifier column, out object value, out string tooltip)
            {
                if (rowId >= m_data.Count)
                    throw new WidgetException(string.Format("NameValueDataReader was asked to retrieve data for row {0} of {1}!", rowId, m_data.Count));

                if (!default(TColumnIdentifier).Equals(column))
                    throw new WidgetException(string.Format("NameValueDataReader was asked to retrieve unknown column {0}!", column));

                string text = m_data.Get((int)rowId);
                string key = m_data.GetKey((int)rowId);

                object data = new SortWrapper<string>(key, text);

                value = data;
                tooltip = data == null ? null : data.ToString();

                return true;
            }
        }
        /*
        public class DictionaryDataReader<T> : ITableDataReader
        {
            private readonly IDictionary<TColumnIdentifier, T> m_data;

            public IEnumerable<uint> Rows
            {
                get { return new uint[] { 0 }; }
            }

            public DictionaryDataReader(IDictionary<TColumnIdentifier, T> data)
            {
                m_data = data;
            }

            public bool GetCell(uint rowId, TColumnIdentifier column, out object value, out string tooltip)
            {
                if (rowId != 0)
                    throw new WidgetException(string.Format("DictionaryDataReader for {0} asked to retrieve data for row {1} while only 0 is supported!", m_data.GetType(), rowId));

                T data;

                if (m_data.TryGetValue(column, out data))
                    throw new WidgetException(string.Format("DictionaryDataReader for {0} asked to retrieve unknown column {1}!", m_data.GetType(), column));

                value = data;
                tooltip = data == null ? null : data.ToString();

                return true;
            }
        }*/

        public class HeaderDataReader : ITableDataReader
        {
            private readonly TableLayout m_layout;

            public IEnumerable<uint> Rows
            {
                get { return new uint[] { 0 }; }
            }

            public HeaderDataReader(TableLayout layout)
            {
                m_layout = layout;
            }

            public bool GetCell(uint rowId, TColumnIdentifier column, out object value, out string tooltip)
            {
                ColumnLayout columnLayout = m_layout.GetColumn(column);

                value = columnLayout.FullName;
                tooltip = columnLayout.Tooltip;

                return true;
            }
        }

        [Obsolete("WidgetTable now uses ITableDataReader instead of ITableDataRow")]
        public interface ITableDataRow
        {
            /// <summary>
            /// Unique row id
            /// </summary>
            uint Id { get; }

            /// <summary>
            /// Retrieves data for this row
            /// </summary>
            /// <returns></returns>
            void GetRowData(IDictionary<TColumnIdentifier, object> data, IDictionary<TColumnIdentifier, string> tooltips, out bool selected);
        }

#pragma warning disable CS0618 // Type or member is obsolete

        /// <summary>
        /// Special class for reading data from obsolete ITableDataRow
        /// </summary>
        private class RowDataReader : ITableDataReader
        {
            private readonly IList<ITableDataRow> m_data;

            public IEnumerable<uint> Rows
            {
                get
                {
                    IEnumerator<ITableDataRow> e = m_data.GetEnumerator();

                    while (e.MoveNext())
                        yield return e.Current.Id;
                }
            }

            public RowDataReader(params ITableDataRow [] data)
            {
                m_data = new List<ITableDataRow>(data);
            }

            public ICollection<TColumnIdentifier> GetColumns()
            {
                throw new NotImplementedException();
            }

            public bool GetCell(uint rowId, TColumnIdentifier column, out object value, out string tooltip)
            {
                ITableDataRow row = null;

                for (int i = 0; i < m_data.Count; i++)
                    if (m_data[i].Id == rowId)
                    {
                        row = m_data[i];
                        break;
                    }

                if (row == null)
                    throw new WidgetException("Invalid row " + rowId + " for RowDataReader");

                Dictionary<TColumnIdentifier, object> values = new Dictionary<TColumnIdentifier, object>();
                Dictionary<TColumnIdentifier, string> tooltips = new Dictionary<TColumnIdentifier, string>();

                bool selected;

                row.GetRowData(values, tooltips, out selected);

                if(!values.TryGetValue(column, out value))
                    throw new WidgetException("Invalid column " + column + " for RowDataReader");

                tooltips.TryGetValue(column, out tooltip);

                return selected;
            }

            public void AddRow(ITableDataRow row)
            {
                m_data.Add(row);
            }
        }

#pragma warning restore CS0618 // Type or member is obsolete

        #endregion
    }
}
