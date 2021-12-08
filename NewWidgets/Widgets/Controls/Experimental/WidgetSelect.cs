using System;
using System.Numerics;
using System.Collections.Generic;
using NewWidgets.Widgets;
using RunMobile.Utility;

namespace NewWidgets.Widgets
{
    /// <summary>
    /// List-type widget consisting of left and right buttons and label
    /// </summary>
    public class WidgetSelect : WidgetPanel
    {
        private readonly SimpleListDictionary<string, object> m_items;
        private readonly WidgetLabel m_label;
        private readonly WidgetButton m_leftButton;
        private readonly WidgetButton m_rightButton;

        private int m_selectedIndex = -1;
        private bool m_animated;

        public Action<int> OnSelectionChange;
        public Action<int, object> OnSelectionValueChange;

        /// <summary>
        /// Use animation effect for inner label
        /// </summary>
        /// <value><c>true</c> if animated; otherwise, <c>false</c>.</value>
        public bool IsAnimated
        {
            get { return m_animated; }
            set { m_animated = value; }
        }

        // TODO: special class with this functionality?
        public override bool Enabled
        {
            get { return base.Enabled; }
            set
            {
                base.Enabled = value;
                foreach (var widget in Children)
                    widget.Enabled = value;
            }
        }

        public int SelectedIndex
        {
            get { return m_selectedIndex; }
            set
            {
                m_selectedIndex = m_items.Count == 0 ? -1 : value % m_items.Count;

                m_label.Text = SelectedItem.Key;
                m_label.Relayout();
                if (m_animated)
                    m_label.AnimateAppear(false);
            }
        }

        public object SelectedValue
        {
            get { return SelectedItem.Value; }
            set
            {
                m_selectedIndex = m_items.Count == 0 ? -1 : 0;

                if (value == null)
                    return;

                for (int i = 0; i < m_items.Count; i++)
                {
                    if (value.Equals(m_items[i].Value))
                    {
                        m_selectedIndex = i;
                        break;
                    }
                }

                m_label.Text = SelectedItem.Key;
                m_label.Relayout();
                if (m_animated)
                    m_label.AnimateAppear(false);
            }
        }

        public KeyValuePair<string, object> SelectedItem
        {
            get { return m_selectedIndex < 0 || m_items.Count == 0 ? new KeyValuePair<string, object>(string.Empty, null) : m_items[m_selectedIndex]; }
        }


        public IList<KeyValuePair<string, object>> Items
        {
            get { return m_items.List; }
        }

        public WidgetSelect(
            string[] items = null,
            object[] values = null)
            : this(WidgetManager.GetStyle("left_button"),
                  WidgetManager.GetStyle("default_select"),
                  WidgetManager.GetStyle("right_button"),
                  new SimpleListDictionary<string,object>(items, values))
        {
        }

        public WidgetSelect(
            IList<KeyValuePair<string, object>> items)
            : this(WidgetManager.GetStyle("left_button"),
                  WidgetManager.GetStyle("default_select"),
                  WidgetManager.GetStyle("right_button"),
                  new SimpleListDictionary<string, object>(items))
        {
        }

        public WidgetSelect(WidgetStyleSheet leftButtonStyle,
            WidgetStyleSheet labelStyle,
            WidgetStyleSheet rightButtonStyle,
            SimpleListDictionary<string, object> items
            )
            : base(Widget.DefaultStyle)
        {
            m_items = items;

            m_leftButton = new WidgetButton(leftButtonStyle);
            AddChild(m_leftButton);
            m_leftButton.OnPress += (obj) => HandleChange(-1);

            m_label = new WidgetLabel(labelStyle, items.Count > 0 ? items[0].Key : string.Empty);
            //m_label.FontSize = WidgetManager.FontScale * 1.25f;
            AddChild(m_label);
            m_label.Relayout();

            m_rightButton = new WidgetButton(rightButtonStyle);
            AddChild(m_rightButton);
            m_rightButton.OnPress += (obj) => HandleChange(+1);

            m_animated = true;

            Size = new Vector2(m_label.Size.X + m_leftButton.Size.X + m_rightButton.Size.X, m_label.Size.Y);
        }

        protected override void Resize(Vector2 size)
        {
            base.Resize(size);

            m_leftButton.Position = new Vector2(0, (size.Y - m_leftButton.Size.Y) / 2);
            m_label.Position = new Vector2(m_leftButton.Size.X, (size.Y - m_label.Size.Y) / 2);
            m_label.Size = new Vector2(size.X - m_leftButton.Size.X - m_rightButton.Size.X, m_label.Size.Y);
            m_rightButton.Position = new Vector2(size.X - m_rightButton.Size.X, (size.Y - m_rightButton.Size.Y) / 2);
        }

        public void AddItem(string item, object value = null)
        {
            m_items.Add(item, value);
        }

        public void ClearItems()
        {
            m_items.Clear();
            m_selectedIndex = 0;
            m_label.Text = string.Empty;
        }

        private void HandleChange(int delta)
        {
            // Nomad: I'm putting cycling logic here to avoid excessive code in SelectedIndex setter

           if (SelectedIndex + delta < 0)
                SelectedIndex = (SelectedIndex + delta) % m_items.Count + m_items.Count;
            else
                SelectedIndex += delta;

            if (OnSelectionChange != null)
                OnSelectionChange(SelectedIndex);

            if (OnSelectionValueChange != null)
                OnSelectionValueChange(SelectedIndex, SelectedValue);
        }

        // Force play animation if it's enabled
        public void Animate()
        {
            if (m_animated)
                m_label.AnimateAppear(false);
        }
    }
}
