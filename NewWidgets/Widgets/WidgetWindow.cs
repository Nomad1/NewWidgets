using System.Numerics;
using NewWidgets.UI;

namespace NewWidgets.Widgets
{
    /// <summary>
    /// Draggable window class. Brings to front on click or visibility change
    /// </summary>
    public class WidgetWindow : WidgetPanel
    {
        public static readonly new WidgetStyleSheet DefaultStyle = WidgetManager.GetStyle("default_window", true);

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

        public WidgetWindow(WidgetStyleSheet style = default(WidgetStyleSheet))
            : base(style.IsEmpty ? DefaultStyle : style)
        {
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
