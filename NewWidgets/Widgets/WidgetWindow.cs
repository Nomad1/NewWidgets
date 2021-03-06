﻿using System.Numerics;
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

        private bool m_draggable = true;
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

        public bool Draggable
        {
            get { return m_draggable; }
            set { m_draggable = value; }
        }

        public WidgetWindow(WidgetStyleSheet style = default(WidgetStyleSheet))
            : base(style.IsEmpty ? DefaultStyle : style)
        {

        }

        public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            if (Visible && (press || unpress))
                BringToFront();

            if (base.Touch(x, y, press, unpress, pointer))
                return true;

            if (m_draggable && !m_dragging && press && (pointer == 0 || WindowController.Instance.IsTouchScreen))
            {
                Vector2 local = this.Parent.Transform.GetClientPoint(new Vector2(x, y));
                m_dragShift = local;
                m_dragStart = Position;
                m_dragging = true;

                WindowController.Instance.OnTouch += HandleGlobalTouch;

                return true;
            }

            return true; // Windows should always return true in the end!
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


            // we're still returning true here to avoid clicks on non-active windows
            return true;
        }
    }
}
