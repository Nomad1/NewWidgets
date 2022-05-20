using System;
using System.Collections.Generic;
using System.Numerics;

using NewWidgets.UI;
using NewWidgets.Utility;

using RunMobile.Utility;

namespace NewWidgets.Widgets
{
    /// <summary>
    /// Panel element with scrolling support
    /// </summary>
    public class WidgetScrollView : WidgetBackground, IWindowContainer
    {
        public new const string ElementType = "scrollview";

        private class WidgetScrollContent : WidgetPanel
        {
            public new const string ElementType = "scrollviewcontent";

            public WidgetScrollContent(WidgetStyle style = default(WidgetStyle))
                : base(ElementType, style)
            {
            }
        }


        //
        public const string HorizontalScrollId = "scrollview_hscroll";
        public const string HorizontalTrackerId = "scrollview_htrack";
        public const string VerticalScrollId = "scrollview_vscroll";
        public const string VerticalScrollTrackerId = "scrollview_vtrack";
        //

        private static readonly float s_zoomDeltaScale = 10.0f;
        private static readonly float s_borderPadding = 100.0f;
        private static readonly float s_dragMultiplier = 3.0f;
        private static readonly float s_dragEpsilon = 2.0f;
        private static readonly int s_autoScrollSpeed = 50;

        private static readonly float s_scrollEpsilon = 0.0001f;

        private static readonly Margin s_verticalIndicatorPadding = new Margin(3.0f);
        private static readonly Margin s_horizontalIndicatorPadding = new Margin(3.0f);

        private WidgetScrollType m_horizontalScroll;
        private WidgetScrollType m_verticalScroll;

        private readonly WidgetScrollContent m_contentView;
        private readonly Widget m_horizontalScrollBar;
        private readonly Widget m_verticalScrollBar;
        private readonly Widget m_horizontalScrollBarIndicator;
        private readonly Widget m_verticalScrollBarIndicator;

        private Vector2 m_dragShift;
        private Vector2 m_dragStart;
        private bool m_dragging;
        private bool m_dragVScroller;
        private bool m_dragHScroller;
        private bool m_horizontalBarVisible;
        private bool m_verticalBarVisible;

        private bool m_animating;

        public override bool Enabled
        {
            get { return m_contentView.Enabled; }
            set { m_contentView.Enabled = value; }
        }

        public Vector2 ContentSize
        {
            get
            {
                return m_contentView.Size;
            }
            set
            {
                bool relayout = Vector2.DistanceSquared(m_contentView.Size, value) > float.Epsilon;
                m_contentView.Size = value;
                SetScroll(m_contentView.Position.X, m_contentView.Position.Y, true);
                if (relayout)
                    InvalidateLayout();
            }
        }

        public WidgetScrollType HorizontalScroll
        {
            get { return m_horizontalScroll; }
            set
            {
                bool relayout = (value & WidgetScrollType.Visible) != (m_horizontalScroll & WidgetScrollType.Visible);
                m_horizontalScroll = value;
                if (relayout)
                    InvalidateLayout();
            }
        }

        public WidgetScrollType VerticalScroll
        {
            get { return m_horizontalScroll; }
            set
            {
                bool relayout = (value & WidgetScrollType.Visible) != (m_verticalScroll & WidgetScrollType.Visible);
                m_verticalScroll = value;
                if (relayout)
                    InvalidateLayout();
            }
        }

        ICollection<WindowObject> IWindowContainer.Children
        {
            get { return ((IWindowContainer)m_contentView).Children; }
        }

        public int MaximumZIndex
        {
            get { return ((IWindowContainer)m_contentView).MaximumZIndex; }
        }

        public IList<Widget> Children
        {
            get { return m_contentView.Children; }
        }

        protected WidgetPanel ContentView
        {
            get { return m_contentView; }
        }

        /// <summary>
        /// Creates a scroll view
        /// </summary>
        /// <param name="style"></param>
        public WidgetScrollView(WidgetStyle style = default(WidgetStyle))
            : this(ElementType, style)
        {
        }

        /// <summary>
        /// Creates a scroll view
        /// </summary>
        /// <param name="style"></param>
        protected WidgetScrollView(string elementType, WidgetStyle style)
            : base(elementType, style)
        {
            m_contentView = new WidgetScrollContent();
            m_contentView.Parent = this;
            m_horizontalScroll = WidgetScrollType.Normal | WidgetScrollType.Visible | WidgetScrollType.AutoHide;
            m_verticalScroll = WidgetScrollType.Normal | WidgetScrollType.Visible | WidgetScrollType.AutoHide;

            m_horizontalScrollBar = new WidgetBackground(new WidgetStyle(HorizontalScrollId));
            m_horizontalScrollBar.Parent = this;

            m_horizontalScrollBarIndicator = new WidgetBackground(new WidgetStyle(HorizontalTrackerId));
            m_horizontalScrollBarIndicator.Parent = this;

            m_verticalScrollBar = new WidgetBackground(new WidgetStyle(VerticalScrollId));
            m_verticalScrollBar.Parent = this;

            m_verticalScrollBarIndicator = new WidgetBackground(new WidgetStyle(VerticalScrollTrackerId));
            m_verticalScrollBarIndicator.Parent = this;

            Overflow = WidgetOverflow.Hidden;
        }

        protected override void Resize(Vector2 size)
        {
            base.Resize(size);

            if (m_contentView != null)
            {
                Vector2 contentSize = new Vector2(Math.Max(size.X, m_contentView.Size.X), Math.Max(size.Y, m_contentView.Size.Y));
                m_contentView.Size = contentSize;

                InvalidateLayout();
            }
        }

        public override void UpdateLayout()
        {
            m_horizontalBarVisible = (m_horizontalScroll & WidgetScrollType.Visible) != 0;
            m_verticalBarVisible = (m_verticalScroll & WidgetScrollType.Visible) != 0;

            if (m_horizontalBarVisible)
            {
                // TODO: load padding from WidgetManager
                Margin horizontalScrollBarPadding = m_verticalBarVisible ? new Margin(2, 0, 18, 1) : new Margin(2, 0, 2, 1);
                float horizontalScrollBarHeight = 21;
                float horizontalScrollIndicatorHeight = 15;
                float horizontalScrollIndicatorWidth = 60;

                m_horizontalScrollBar.Size = new Vector2(Size.X - horizontalScrollBarPadding.Right - horizontalScrollBarPadding.Left, horizontalScrollBarHeight);
                m_horizontalScrollBar.Position = new Vector2(horizontalScrollBarPadding.Left, Size.Y - m_horizontalScrollBar.Size.Y - horizontalScrollBarPadding.Bottom);

                float maxLength = m_horizontalScrollBar.Size.X - s_horizontalIndicatorPadding.Left - s_horizontalIndicatorPadding.Right;
                float indicatorSize = (maxLength * Size.X / m_contentView.Size.X);

                if (indicatorSize < horizontalScrollIndicatorWidth)
                    indicatorSize = horizontalScrollIndicatorWidth;
                if (indicatorSize > maxLength)
                    indicatorSize = maxLength;

                m_horizontalScrollBarIndicator.Size = new Vector2(indicatorSize, horizontalScrollIndicatorHeight);

                if ((m_horizontalScroll & WidgetScrollType.AutoHide) != 0)
                    m_horizontalBarVisible = indicatorSize < maxLength;
            }

            if (m_verticalBarVisible)
            {
                // TODO: load padding from WidgetManager
                Margin verticalScrollBarPadding = m_horizontalBarVisible ? new Margin(0, 2, 1, 18) : new Margin(0, 2, 1, 2);
                float verticalScrollBarWidth = 21;
                float verticalScrollIndicatorWidth = 15;
                float verticalScrollIndicatorHeight = 60;

                m_verticalScrollBar.Size = new Vector2(verticalScrollBarWidth, Size.Y - verticalScrollBarPadding.Top - verticalScrollBarPadding.Bottom);
                m_verticalScrollBar.Position = new Vector2(Size.X - m_verticalScrollBar.Size.X - verticalScrollBarPadding.Right, verticalScrollBarPadding.Top);

                float maxLength = m_verticalScrollBar.Size.Y - s_verticalIndicatorPadding.Left - s_verticalIndicatorPadding.Right;
                float indicatorSize = (maxLength * Size.Y / m_contentView.Size.Y);

                if (indicatorSize < verticalScrollIndicatorHeight)
                    indicatorSize = verticalScrollIndicatorHeight;
                if (indicatorSize > maxLength)
                    indicatorSize = maxLength;

                m_verticalScrollBarIndicator.Size = new Vector2(verticalScrollIndicatorWidth, indicatorSize);

                if ((m_verticalScroll & WidgetScrollType.AutoHide) != 0)
                    m_verticalBarVisible = indicatorSize < maxLength;
            }

            UpdateScroll();

            base.UpdateLayout();
        }

        public override bool Update()
        {
            if (!base.Update())
                return false;

            m_contentView.Update();

            if (m_animating)
                UpdateScroll();

            if (m_horizontalBarVisible)
            {
                m_horizontalScrollBar.Opacity = Opacity;
                m_horizontalScrollBar.Update();
                m_horizontalScrollBarIndicator.Opacity = Opacity;
                m_horizontalScrollBarIndicator.Update();
            }

            if (m_verticalBarVisible)
            {
                m_verticalScrollBar.Opacity = Opacity;
                m_verticalScrollBar.Update();
                m_verticalScrollBarIndicator.Opacity = Opacity;
                m_verticalScrollBarIndicator.Update();
            }

            return true;
        }

        protected override void DrawContents()
        {
            base.DrawContents();

            RectangleF visibleRect = new RectangleF(-m_contentView.Position.X, -m_contentView.Position.Y, Size.X, Size.Y); // TODO: margins and scale?

            foreach (Widget widget in m_contentView.Children)
            {
                RectangleF widgetRect = new RectangleF(widget.Position.X, widget.Position.Y, widget.Size.X, widget.Size.Y);

                if (visibleRect.IntersectsWith(widgetRect))
                    widget.Draw();

                //                if (widget.Position > m_contentView.Size
            }

            //m_contentView.Draw(canvas);

            if (m_horizontalBarVisible)
            {
                m_horizontalScrollBar.Draw();
                m_horizontalScrollBarIndicator.Draw();
            }

            if (m_verticalBarVisible)
            {
                m_verticalScrollBar.Draw();
                m_verticalScrollBarIndicator.Draw();
            }
        }

        private bool StopDragTouch(float x, float y, bool press, bool unpress, int pointer)
        {
            return Touch(x, y, press, unpress, pointer);
        }

        private void StopDrag(Vector2 point)
        {
            WindowController.Instance.OnTouch -= StopDragTouch;

            if (m_dragging)
            {
                m_dragging = false;

                Vector2 local = this.Transform.GetClientPoint(point);

                Vector2 move = local - m_dragShift;

                float multiplier = s_dragMultiplier;

                if (m_dragVScroller)
                {
                    move = new Vector2(move.X, -move.Y);
                    multiplier = m_contentView.Size.Y / (m_verticalScrollBar.Size.Y - m_verticalScrollBarIndicator.Size.Y);
                }
                else
                if (m_dragHScroller)
                {
                    move = new Vector2(-move.X, move.Y);
                    multiplier = m_contentView.Size.X / (m_horizontalScrollBar.Size.X - m_horizontalScrollBarIndicator.Size.X);
                }

                float targetX = m_contentView.Position.X + move.X * multiplier;
                if (targetX < -m_contentView.Size.X + Size.X)
                    targetX = -m_contentView.Size.X + Size.X;
                if (targetX > 0)
                    targetX = 0;

                float targetY = m_contentView.Position.Y + move.Y * multiplier;
                if (targetY < -m_contentView.Size.Y + Size.Y)
                    targetY = -m_contentView.Size.Y + Size.Y;
                if (targetY > 0)
                    targetY = 0;

                Vector2 target = new Vector2(targetX, targetY);
                if (Vector2.DistanceSquared(target, m_contentView.Position) > float.Epsilon)
                    m_contentView.Move(target, 120, null);
            }
        }

        public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            /*if (!Enabled)
                return true;
            */

            if (!m_dragging)
            {

                if (m_contentView.Touch(x, y, press, unpress, pointer))
                    return true;

                if (base.Touch(x, y, press, unpress, pointer))
                    return true;
            }

            //bool hit = this.Sprite.HitTest(x, y);
            Vector2 point = new Vector2(x, y);
            Vector2 local = this.Transform.GetClientPoint(point);

            if (!m_dragging && press && /*hit && */(pointer == 0 || WindowController.Instance.IsTouchScreen))
            {
                m_dragVScroller = m_verticalScrollBar.HitTest(x, y);
                m_dragHScroller = m_horizontalScrollBar.HitTest(x, y);

                if (!WindowController.Instance.IsTouchScreen && !m_dragVScroller && !m_dragHScroller)
                    return true; // on large screens allow only scroller scroll, no body scroll


                m_dragShift = local;
                m_dragStart = m_dragShift;
                m_dragging = true;

                WindowController.Instance.OnTouch += StopDragTouch;

                return true;
            }

            if (m_dragging && ((unpress && (pointer == 0 || WindowController.Instance.IsTouchScreen))/* || !hit*/))
            {
                StopDrag(point);

                if (/*hit && */ Vector2.DistanceSquared(m_dragStart, point) < s_dragEpsilon)
                    return false;

                return true;
            }

            if (m_dragging)
            {
                Vector2 move = local - m_dragShift;

                float multiplier = s_dragMultiplier;

                if (m_dragVScroller)
                {
                    move = new Vector2(move.X, -move.Y);
                    multiplier = (m_contentView.Size.Y + Size.Y + s_borderPadding) / (m_verticalScrollBar.Size.Y - m_verticalScrollBarIndicator.Size.Y);
                }
                else
                if (m_dragHScroller)
                {
                    move = new Vector2(-move.X, move.Y);

                    multiplier = (m_contentView.Size.X + Size.X + s_borderPadding) / (m_horizontalScrollBar.Size.X - m_horizontalScrollBarIndicator.Size.X);
                }

                if (Vector2.DistanceSquared(move, Vector2.Zero) > float.Epsilon)
                {
                    float targetX = m_contentView.Position.X;

                    if (m_horizontalScroll != 0)
                    {
                        float border = (m_horizontalScroll & WidgetScrollType.Inertial) != 0 ? s_borderPadding : 0;
                        targetX = m_contentView.Position.X + move.X * multiplier;

                        if (targetX < -m_contentView.Size.X + Size.X - border)
                            targetX = -m_contentView.Size.X + Size.X - border;
                        if (targetX > border)
                            targetX = border;
                    }

                    float targetY = m_contentView.Position.Y;

                    if (m_verticalScroll != 0)
                    {
                        float border = (m_verticalScroll & WidgetScrollType.Inertial) != 0 ? s_borderPadding : 0;
                        targetY = m_contentView.Position.Y + move.Y * multiplier;
                        if (targetY < -m_contentView.Size.Y + Size.Y - border)
                            targetY = -m_contentView.Size.Y + Size.Y - border;
                        if (targetY > border)
                            targetY = border;
                    }
                    Vector2 target = new Vector2(targetX, targetY);

                    if (Vector2.DistanceSquared(target, m_contentView.Position) > float.Epsilon)
                        m_contentView.Move(target, 1, null);

                    m_dragShift = local;

                    UpdateScroll();
                }
                return true;
            }

            return false;
        }

        public override bool Zoom(float x, float y, float value)
        {
            //While it's not required for Widget descendants, all WidgetScrollView descendants should use
            //the following lines:

            //bool processed = base.Zoom(x, y, value);

            //if (processed)
            //    return true;

            if (!Enabled)
                return true;

            if (!m_verticalBarVisible && !m_horizontalBarVisible)
                return true;

            bool isX = (m_horizontalBarVisible) && (!m_verticalBarVisible || Math.Abs(m_contentView.Size.Y - Size.Y) < Math.Abs(m_contentView.Size.X - Size.X));

            if (isX)
                SetScroll(m_contentView.Position.X + value * s_zoomDeltaScale, m_contentView.Position.Y);
            else
                SetScroll(m_contentView.Position.X, m_contentView.Position.Y + value * s_zoomDeltaScale);

            return true;
        }

        public override bool Key(SpecialKey key, bool up, string keyString)
        {
            if (!Enabled)
                return true;

            if (m_contentView.Key(key, up, keyString))
                return true;

            return false;
        }

        public void AddChild(Widget child)
        {
            m_contentView.AddChild(child);
        }

        public bool RemoveChild(WindowObject child)
        {
            return m_contentView.RemoveChild(child);
        }

        void IWindowContainer.AddChild(WindowObject child)
        {
            if (child is Widget)
                AddChild((Widget)child);
            else
                throw new ArgumentException(nameof(child));
        }

        public void Clear(bool scroolToZero = false)
        {
            m_contentView.Clear();

            if (scroolToZero)
                SetScroll(0, 0, true);
        }

        public Vector2 GetScroll()
        {
            return m_contentView.Position;
        }

        public void SetScroll(float x, float y, bool instant = false)
        {
            SetScroll(x, y, instant ? 1 : s_autoScrollSpeed);
        }

        public void SetScroll(float x, float y, int time)
        {
            if (x < -m_contentView.Size.X + Size.X)
                x = -m_contentView.Size.X + Size.X;
            if (x > 0)
                x = 0;

            if (y < -m_contentView.Size.Y + Size.Y)
                y = -m_contentView.Size.Y + Size.Y;
            if (y > 0)
                y = 0;

            Vector2 newPos = new Vector2(x, y);
            float distance = (newPos - m_contentView.Position).LengthSquared();
            if (distance < s_scrollEpsilon)
                return;

            m_animating = true;
            m_contentView.Move(newPos, time, delegate { m_animating = false; UpdateScroll(); });
        }

        private void UpdateScroll()
        {
            if (m_horizontalBarVisible)
            {
                float max = m_horizontalScrollBar.Size.X - s_horizontalIndicatorPadding.Left - s_horizontalIndicatorPadding.Right - m_horizontalScrollBarIndicator.Size.X;

                float percent = MathHelper.Clamp(-m_contentView.Position.X / (m_contentView.Size.X - Size.X), 0, 1);
                m_horizontalScrollBarIndicator.Position = new Vector2(max * percent + m_horizontalScrollBar.Position.X + s_horizontalIndicatorPadding.Left, m_horizontalScrollBar.Position.Y + s_horizontalIndicatorPadding.Top);
            }

            if (m_verticalBarVisible)
            {
                float max = m_verticalScrollBar.Size.Y - s_verticalIndicatorPadding.Top - s_verticalIndicatorPadding.Bottom - m_verticalScrollBarIndicator.Size.Y;

                float percent = MathHelper.Clamp(-m_contentView.Position.Y / (m_contentView.Size.Y - Size.Y), 0, 1);
                m_verticalScrollBarIndicator.Position = new Vector2(m_verticalScrollBar.Position.X + s_verticalIndicatorPadding.Left, max * percent + m_verticalScrollBar.Position.Y + s_verticalIndicatorPadding.Top);
            }
        }
    }
}
