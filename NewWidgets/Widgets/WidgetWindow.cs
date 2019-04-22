using System.Numerics;
using NewWidgets.UI;
using NewWidgets.Widgets.Styles;

namespace NewWidgets.Widgets
{
    /// <summary>
    /// Draggable window class. Brings to front on click or visibility change
    /// </summary>
    public class WidgetWindow : WidgetPanel
    {
        public static readonly new WidgetStyleReference<WidgetBackgroundStyleSheet> DefaultStyle = WidgetManager.RegisterDefaultStyle<WidgetBackgroundStyleSheet>("default_window");

        private static readonly float s_dragEpsilonSquared = 10.0f*10.0f;

        private Vector2 m_dragShift;
        private Vector2 m_dragStart;
        private bool m_dragging;

        public override bool Visible
        {
            get { return base.Visible; }
            set
            {
                if (value && !base.Visible)
                    BringToFront();
                base.Visible = value;
            }
        }

        public WidgetWindow()
            : this(null)
        {
        }

        public WidgetWindow(WidgetStyleSheet style)
            : base(style as WidgetBackgroundStyleSheet ?? DefaultStyle)
        {
            if (Style != style)
                WindowController.Instance.LogMessage("WARNING: Initing {0} with style {1}. Falling back to default style.", GetType(), style == null ? "(null)" : style.ToString());
        }

        public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            if (press || unpress)
                BringToFront();

            if (base.Touch(x, y, press, unpress, pointer))
                return true;

            if (!m_dragging && press && pointer == 0)
            {
                Vector2 local = this.Parent.Transform.GetClientPoint(new Vector2(x, y));
                m_dragShift = local;
                m_dragStart = Position;
                m_dragging = true;

                WindowController.Instance.OnTouch += HandleGlobalTouch;

                return true;
            }

            return false;
        }

        private bool HandleGlobalTouch(float x, float y, bool press, bool unpress, int pointer)
        {
            if (m_dragging)
            {
                Vector2 local = this.Parent.Transform.GetClientPoint(new Vector2(x, y));
                Vector2 move = local - m_dragShift;

                if (move.LengthSquared() > 0)
                    Position = m_dragStart + move;
            }

            if (unpress)
            {
                m_dragging = false;

                WindowController.Instance.OnTouch -= HandleGlobalTouch;

                if (Vector2.DistanceSquared(m_dragStart, m_dragShift) > s_dragEpsilonSquared)
                {
                    return true;
                }
            }

            return true;
        }
    }
}
