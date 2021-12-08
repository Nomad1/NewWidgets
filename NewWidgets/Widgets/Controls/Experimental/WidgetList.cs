using System.Numerics;
using System.Collections.Generic;
using System;

using NewWidgets.Widgets;

namespace NewWidgets.Widgets
{
    /// <summary>
    /// List box type control. Based on table
    /// </summary>
    public class WidgetList : WidgetTable<WidgetList.NameOnlyField>
    {
        #region Helpers

        public enum NameOnlyField
        {
            Name,
        }

        private class ListElement : ITableDataSource
        {
            public readonly uint Id;
            public readonly string Name;
            public readonly object Key;
            public readonly string Tooltip;

            public ListElement(uint id, string name, object key, string tooltip)
            {
                Id = id;
                Key = key;
                Name = name;
                Tooltip = tooltip;
            }

            public bool GetData(NameOnlyField column, out object value, out string tooltip)
            {
                value = new SortWrapper<object>(Key ?? Name, Name);
                tooltip = Tooltip;
                return false;
            }
        }

        #endregion

        private readonly List<ListElement> m_data;

        private bool m_allowMultiSelect;
        private uint m_ids;

        public bool MultiSelect
        {
            get { return m_allowMultiSelect; }
            set { m_allowMultiSelect = value; }
        }

        /// <summary>
        /// Index of selected object. It will be -1 if no object is selected
        /// </summary>
        public int SelectedIndex
        {
            get
            {
                uint[] selectedRows = SelectedIndices;

                if (selectedRows.Length == 0)
                    return -1;

                return (int)selectedRows[0];
            }
            set
            {
                HandleRowClick((uint)value, NameOnlyField.Name);
            }
        }

        /// <summary>
        /// List of selected objects in case of multi-selection
        /// </summary>
        public object [] SelectedObjects
        {
            get
            {
                uint[] selectedRows = SelectedIndices;

                if (selectedRows.Length == 0)
                    return new object[0];

                List<object> result = new List<object>();

                foreach (ListElement element in m_data)
                    foreach (uint id in selectedRows)
                    {
                        if (element.Id == id)
                            result.Add(id);
                    }

                return result.ToArray();
            }
        }

        /// <summary>
        /// First selected object or null if none is selected
        /// </summary>
        public object SelectedObject
        {
            get
            {
                object[] selectedObjects = SelectedObjects;

                if (selectedObjects.Length == 0)
                    return null;

                return selectedObjects[0];
            }
        }

        public event Func<int, object, bool> OnSelectedIndexChanged;

        public WidgetList(WidgetStyleSheet styleSheet = default(WidgetStyleSheet), string header = "")
            : base(styleSheet.IsEmpty ? DefaultStyle : styleSheet)
        {
            m_data = new List<ListElement>();

            AddColumn(NameOnlyField.Name, string.IsNullOrEmpty(header) ? "" : header, string.IsNullOrEmpty(header) ? "" : header, 255, WidgetAlign.Left, false, false);

            if (string.IsNullOrEmpty(header))
                ShowHeader = false;

            this.HorizontalScroll = WidgetScrollType.None;
            this.VerticalScroll = WidgetScrollType.AutoHide | WidgetScrollType.Visible;

            this.SetDataReader(new ListDataReader(m_data));
        }

        protected override bool HandleRowClick(uint id, NameOnlyField column)
        {
            if (base.HandleRowClick(id, column))
                return true;

            ListElement data = null;
            foreach (ListElement element in m_data)
                if (element.Id == id)
                {
                    data = element;
                    break;
                }

            if (data == null) // misplaced click
                return false;

            List<uint> selectedIndices = new List<uint>(SelectedIndices);

            if (selectedIndices.Contains(id))
            {
                if (m_allowMultiSelect)
                {
                    selectedIndices.Remove(id);
                }
                else
                    return false; // in single select mode clicking on selected element gives nothing at all
            }
            else
            {
                if (OnSelectedIndexChanged != null && !OnSelectedIndexChanged((int)id, data.Key)) // callback can prevent us from changing the selection
                    return false;

                if (!m_allowMultiSelect)
                    selectedIndices.Clear();  // in single select there should be no other selections

                selectedIndices.Add(id);
            }

            SelectedIndices = selectedIndices.ToArray();

            return false;
        }

        public uint AddItem(string name, object key = null, string tooltip = "")
        {
            uint id = ++m_ids;
            m_data.Add(new ListElement(id, name, key, tooltip));

            InvalidateLayout();
            return id;
        }

        public void RemoveItem(uint id)
        {
            for (int i = 0; i < m_data.Count; i++)
                if (m_data[i].Id == id)
                {
                    m_data.RemoveAt(i);

                    InvalidateLayout();
                    return;
                }
        }

        protected override void Resize(Vector2 size)
        {
            if (Inited)
            {
                GetColumn(NameOnlyField.Name).Width = size.X;

                InvalidateLayout();
            }

            base.Resize(size);
        }
    }
}
