using System;
using System.Numerics;
using NewWidgets.Utility;
using NewWidgets.Widgets.Styles;

namespace NewWidgets.Widgets
{
    public class WidgetContextMenu : WidgetPanel
    {
        private Vector2 m_itemSize;
        private WidgetStyleSheet m_itemStyle;
        private Margin m_padding;

        private bool m_autohide;
        
        public Vector2 ItemSize
        {
            get { return m_itemSize; }
            set { m_itemSize = value; }
        }

        public WidgetContextMenu()
            : this(WidgetManager.GetStyle("default_context_menu"))
        {
        }

        public WidgetContextMenu(WidgetStyleSheet style)
            : base(style)
        {
            Visible = false;
            m_itemStyle = WidgetManager.GetStyle(style.GetParameter("item_style"));
            if (m_itemStyle == null)
                m_itemStyle = WidgetManager.DefaultButtonStyle;
            
            m_itemSize = m_itemStyle.Size;
            m_padding = style.Padding;
        }

        public void Show(Vector2 position, bool autohide = true)
        {
			Hide();

			WidgetManager.GetTopmostWindow().AddChild(this);

            this.Position = position - m_padding.TopLeft;
            
            Visible = true;

            if (autohide)
                WidgetManager.SetExclusive(this);
                
//                WidgetManager.WindowController.OnTouch += UnHoverTouch;

            m_autohide = autohide;
        }

        public WidgetButton AddItem(string title, Action<WidgetButton> action)
        {
            WidgetButton item = new WidgetButton(m_itemStyle, title);
            item.Position = m_padding.TopLeft + m_itemSize * new Vector2(0, Children.Count);
            if (action != null)
                item.OnPress += delegate { Hide(item); action(item); };
            else
                item.OnPress += Hide;
            
            item.Enabled = !string.IsNullOrEmpty(title);
            
            AddChild(item);

            item.Relayout();
            m_itemSize = new Vector2(Math.Max(m_itemSize.X, item.Size.X), Math.Max(m_itemSize.Y, item.Size.Y));

            for (int i = 0; i < Children.Count; i++)
            {
                ((WidgetButton)Children[i]).Size = m_itemSize;
                ((WidgetButton)Children[i]).Position = m_padding.TopLeft + m_itemSize * new Vector2(0, i);
            }

            Size = new Vector2(m_padding.Width + m_itemSize.X, m_padding.Height + m_itemSize.Y * Children.Count);
            
            return item;
        }

        public void Hide()
        {
            Hide(null);
        }

        private void Hide(WidgetButton caller)
        {
            if (!Visible)
                return;
            
            Visible = false;
            
            if (m_autohide)
                WidgetManager.RemoveExclusive(this);


//                WidgetManager.WindowController.OnTouch -= UnHoverTouch;
        }
        
       /* private bool UnHoverTouch(float x, float y, bool press, bool unpress, int pointer)
        {
            if (Visible && (press || unpress) && !HitTest(x, y))
            {
                Hide();
                return true;
            }
            
            if (Visible && (press || unpress) && HitTest(x, y))
            {
                return this.Touch(x, y, press, unpress, pointer);
            }
            
            return false;
        }*/

        public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            bool hit = HitTest(x, y);
            
            if (unpress && !hit)
                Hide();
            
            return base.Touch(x, y, press, unpress, pointer);
        }

    }
}

