using System.Collections.Generic;
using System.Numerics;
using NewWidgets.UI;
using NewWidgets.Widgets;

namespace NewWidgets.Widgets
{
    /// <summary>
    /// Toolbar panel widget. Allows adding any kind of controls but prefers WidgetButtons
    /// Can be horizontal (by default) or vertical
    /// </summary>
    public class WidgetToolbar : Widget
    {
        public static readonly new WidgetStyleSheet DefaultStyle = WidgetManager.GetStyle("toolbar_panel", true);

        private const float s_offsetX = 8f;
        private const float s_offsetY = 8f;
        private const float s_buttonSpacing = 4f;

        private readonly WidgetPanel m_buttons;
        private readonly bool m_vertical;

        public IList<Widget> Buttons
        {
            get { return m_buttons.Children; }
        }

        public WidgetToolbar(bool vertical = false)
            : base(Widget.DefaultStyle)
        {
            m_buttons = new WidgetPanel(DefaultStyle);
            m_buttons.Parent = this;
            m_vertical = vertical;

        }

        public void AddChild(WidgetButton obj)
        {
            m_buttons.AddChild(obj);
            InvalidateLayout();
        }

        public void RemoveChild(WidgetButton obj)
        {
            if (m_buttons.RemoveChild(obj)) // we're doing RemoveChild instead of Remove to make sure buttons are refreshed
                InvalidateLayout();
        }

        public void Clear()
        {
            m_buttons.Clear();
            InvalidateLayout();
        }

        public override void UpdateLayout()
        {
            if (m_buttons.Children.Count > 0)
            {
                float x = s_offsetX;
                float y = s_offsetY;
                float max = 0;

                foreach (var button in m_buttons.Children)
                {
                    if (button.Visible)
                    {
                        button.Position = new Vector2(x, y);

                        if (m_vertical)
                        {
                            y += button.Size.Y + s_buttonSpacing;

                            if (button.Size.X + x > max)
                                max = x + button.Size.X;
                        }
                        else
                        {
                            x += button.Size.X + s_buttonSpacing;

                            if (button.Size.Y + y > max)
                                max = y + button.Size.Y;
                        }
                    }
                }

                if (m_vertical)
                    x = max;
                else
                    y = max;

                // Update global size
                Vector2 size = new Vector2(x + s_offsetX, y + s_offsetY);
                m_buttons.Size = Size = size;

                Visible = true;
            }
            else
            {
                Visible = false;
            }

            base.UpdateLayout();
        }

        public override bool Update()
        {
            if (m_buttons != null)
                m_buttons.Update();

            return base.Update();
        }

        protected override void DrawContents()
        {
            if (m_buttons != null)
                m_buttons.Draw();

            base.DrawContents();
        }

        public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            if (m_buttons != null)
            {
                if (m_buttons.Touch(x, y, press, unpress, pointer))
                    return true;
            }

            return base.Touch(x, y, press, unpress, pointer);
        }

        public override bool Zoom(float x, float y, float value)
        {
            if (m_buttons != null)
            {
                if (m_buttons.Zoom(x, y, value))
                    return true;
            }

            return base.Zoom(x, y, value);
        }

        public override bool Key(SpecialKey key, bool up, string keyString)
        {
            if (m_buttons != null)
            {
                if (m_buttons.Key(key, up, keyString))
                    return true;
            }

            return base.Key(key, up, keyString);
        }
    }

    public class WidgetToolbarSeparator : WidgetButton
    {
        public static readonly new WidgetStyleSheet DefaultStyle = WidgetManager.GetStyle("toolbar_separator", true);
        public static readonly WidgetStyleSheet DefaultHorizontalStyle = WidgetManager.GetStyle("toolbar_separator_horizontal", true);

        public WidgetToolbarSeparator(bool vertical = true)
            : base(vertical ? DefaultStyle : DefaultHorizontalStyle)
        {
            Enabled = false;
        }
    }
}
