using System;
using System.Collections.Generic;
using System.Numerics;
using System.Drawing;

using NewWidgets.UI;
using NewWidgets.Utility;

#if RUNMOBILE
using RunMobile.Utility;
#endif

namespace NewWidgets.Widgets
{
    public class WidgetScrollView : Widget, IWindowContainer
    {
        private static readonly float s_zoomDeltaScale = 10.0f;
        private static readonly float s_borderPadding = 100.0f;
        private static readonly float s_dragMultiplier = 3.0f;
        private static readonly float s_dragEpsilon = 100.0f;
        private static readonly int s_autoScrollSpeed = 50;
        
        private static readonly float ScrollEpsilon = 0.01f;

        private readonly WidgetPanel m_contentView;

        private Vector2 m_dragShift;
        private Vector2 m_dragStart;
        private bool m_dragging;
        private bool m_dragVScroller;
        private bool m_dragHScroller;

        private WidgetScrollType m_horizontalScroll;
        private WidgetScrollType m_verticalScroll;

        private readonly Widget m_horizontalScrollBar;
        private readonly Widget m_verticalScrollBar;
        private readonly Widget m_horizontalScrollBarIndicator;
        private readonly Widget m_verticalScrollBarIndicator;

        private bool m_horizontalBarVisible = false;
        private bool m_verticalBarVisible = false;
        
        private bool m_animating;
        
        private bool m_needLayout;

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
                bool relayout = (m_contentView.Size - value).LengthSquared() > 0;
                m_contentView.Size = value;
                SetScroll(m_contentView.Position.X, m_contentView.Position.Y, true);
                if (relayout)
                    m_needLayout = true;
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
                    m_needLayout = true;
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
                    m_needLayout = true;
            }
        }

        ICollection<WindowObject> IWindowContainer.Children
        {
            get
            {
                return ((IWindowContainer)m_contentView).Children;
            }
        }
        
        public IList<Widget> Children
        {
            get { return m_contentView.Children; }
        }

        public WidgetScrollView()
            : this(WidgetManager.DefaultPanelStyle)
        {            
        }

        public WidgetScrollView(WidgetStyleSheet style)
            : base(style)
        {
            m_contentView = new WidgetPanel(WidgetManager.DefaultWidgetStyle);
            m_contentView.Parent = this;
            ClipContents = true;
            m_horizontalScroll = WidgetScrollType.Normal | WidgetScrollType.Visible | WidgetScrollType.AutoHide;
            m_verticalScroll = WidgetScrollType.Normal | WidgetScrollType.Visible | WidgetScrollType.AutoHide;

            m_horizontalScrollBar = new Widget(WidgetManager.GetStyle("scroll_horizontal"));
            m_horizontalScrollBar.Parent = this;
            m_horizontalScrollBarIndicator = new Widget(WidgetManager.GetStyle("scroll_indicator_horizontal"));
            m_horizontalScrollBarIndicator.Parent = this;
            
            m_verticalScrollBar = new Widget(WidgetManager.GetStyle("scroll_vertical"));
            m_verticalScrollBar.Parent = this;
            m_verticalScrollBarIndicator = new Widget(WidgetManager.GetStyle("scroll_indicator_vertical"));
            m_verticalScrollBarIndicator.Parent = this;

            m_needLayout = true;
        }
        
        protected override void Resize(Vector2 size)
        {
            base.Resize(size);

            if (m_contentView != null)
            {
                Vector2 contentSize = new Vector2(Math.Max(size.X, m_contentView.Size.X), Math.Max(size.Y, m_contentView.Size.Y));
                m_contentView.Size = contentSize;

                m_needLayout = true;
            }
        }

        public void Relayout()
        {
            m_horizontalBarVisible = (m_horizontalScroll & WidgetScrollType.Visible) != 0;
            m_verticalBarVisible = (m_verticalScroll & WidgetScrollType.Visible) != 0;

            if (m_horizontalBarVisible)
            {
                // TODO: load padding from WidgetManager
                Margin horizontalScrollBarPadding = m_verticalBarVisible ? new Margin(2, 0, 18, 1) : new Margin(2, 0, 2, 1);
                Margin horizontalScrollIndicatorPadding = new Margin(3, 3, 3, 3);
                float horizontalScrollBarHeight = 21;
                float horizontalScrollIndicatorHeight = 15;
                float horizontalScrollIndicatorWidth = 60;

                m_horizontalScrollBar.Size = new Vector2(Size.X - horizontalScrollBarPadding.Right - horizontalScrollBarPadding.Left, horizontalScrollBarHeight);
                m_horizontalScrollBar.Position = new Vector2(horizontalScrollBarPadding.Left, Size.Y - m_horizontalScrollBar.Size.Y - horizontalScrollBarPadding.Bottom);

                float maxLength = m_horizontalScrollBar.Size.X - horizontalScrollIndicatorPadding.Left - horizontalScrollIndicatorPadding.Right;
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
                Margin verticalScrollIndicatorPadding = new Margin(3, 3, 3, 3);
                float verticalScrollBarWidth = 21;
                float verticalScrollIndicatorWidth = 15;
                float verticalScrollIndicatorHeight = 60;

                m_verticalScrollBar.Size = new Vector2(verticalScrollBarWidth, Size.Y - verticalScrollBarPadding.Top - verticalScrollBarPadding.Bottom);
                m_verticalScrollBar.Position = new Vector2(Size.X - m_verticalScrollBar.Size.X - verticalScrollBarPadding.Right, verticalScrollBarPadding.Top);

                float maxLength = m_verticalScrollBar.Size.Y - verticalScrollIndicatorPadding.Left - verticalScrollIndicatorPadding.Right;
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

            m_needLayout = false;
        }

        public override bool Update()
        {
            if (!base.Update())
                return false;

            if (m_needLayout)
                Relayout();

            m_contentView.Update();

            if (m_animating)
                UpdateScroll();

            if (m_horizontalBarVisible)
            {
                m_horizontalScrollBar.Alpha = Alpha;
                m_horizontalScrollBar.Update();
                m_horizontalScrollBarIndicator.Alpha = Alpha;
                m_horizontalScrollBarIndicator.Update();
            }

            if (m_verticalBarVisible)
            {
                m_verticalScrollBar.Alpha = Alpha;
                m_verticalScrollBar.Update();
                m_verticalScrollBarIndicator.Alpha = Alpha;
                m_verticalScrollBarIndicator.Update();
            }

            return true;
        }

        protected override void DrawContents(object canvas)
        {
            base.DrawContents(canvas);
            
            RectangleF visibleRect = new RectangleF(-m_contentView.Position.X, -m_contentView.Position.Y, Size.X, Size.Y); // TODO: margins and scale?

            foreach (Widget widget in m_contentView.Children)
            {
                RectangleF widgetRect = new RectangleF(widget.Position.X, widget.Position.Y, widget.Size.X, widget.Size.Y);
                
                if (visibleRect.IntersectsWith(widgetRect))
                    widget.Draw(canvas);
                
//                if (widget.Position > m_contentView.Size
            }
                       
            //m_contentView.Draw(canvas);

            if (m_horizontalBarVisible)
            {
                m_horizontalScrollBar.Draw(canvas);
                m_horizontalScrollBarIndicator.Draw(canvas);
            }

            if (m_verticalBarVisible)
            {
                m_verticalScrollBar.Draw(canvas);
                m_verticalScrollBarIndicator.Draw(canvas);
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
                    multiplier = m_contentView.Size.Y / Size.Y;
                }
                else
                if (m_dragHScroller)
                {
                    move = new Vector2(-move.X, move.Y);
                    multiplier = m_contentView.Size.X / Size.X;
                }

                float targetX = m_contentView.Position.X + move.X * multiplier;// * 2;
                if (targetX < -m_contentView.Size.X + Size.X)
                    targetX = -m_contentView.Size.X + Size.X;
                if (targetX > 0)
                    targetX = 0;

                float targetY = m_contentView.Position.Y + move.Y * multiplier;// * 2;
                if (targetY < -m_contentView.Size.Y + Size.Y)
                    targetY = -m_contentView.Size.Y + Size.Y;
                if (targetY > 0)
                    targetY = 0;

                Vector2 target = new Vector2(targetX, targetY);
                if ((target - m_contentView.Position).LengthSquared() > 0)
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

            if (!m_dragging && press && /*hit && */pointer == 0)
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

            if (m_dragging && ((unpress && pointer == 0)/* || !hit*/))
            {
                StopDrag(point);

                if (/*hit && */(m_dragStart - point).LengthSquared() < s_dragEpsilon)
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
                    multiplier = m_contentView.Size.Y / Size.Y;
                }
                else
                if (m_dragHScroller)
                {
                    move = new Vector2(-move.X, move.Y);
                    multiplier = m_contentView.Size.X / Size.X;
                }

                if (move.LengthSquared() > 0)
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
                    if ((target - m_contentView.Position).LengthSquared() > 0)
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
            bool processed = base.Zoom(x, y, value);

            if (processed)
                return true;

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

        public void AddChild(Widget child)
        {
            m_contentView.AddChild(child);
        }

        void IWindowContainer.AddChild(WindowObject child)
        {
            if (child is Widget)
                AddChild((Widget)child);
            else
                throw new ArgumentException("child");
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
            float distance = (newPos - m_contentView.Position).Length(); // TODO: distance is no longer used, may be switch to DistanceSquared?
            if (distance < ScrollEpsilon)
                return;

            m_animating = true;
            m_contentView.Move(newPos, time/*(int)(distance * 1000 / s_autoScrollSpeed)*/, delegate { m_animating = false; UpdateScroll(); });
        }

        private void UpdateScroll()
        {
            if (m_horizontalBarVisible)
            {
                Margin horizontalScrollIndicatorPadding = new Margin(3, 3, 3, 3);
                
                float max = m_horizontalScrollBar.Size.X - horizontalScrollIndicatorPadding.Left - horizontalScrollIndicatorPadding.Right - m_horizontalScrollBarIndicator.Size.X;
                
                float percent = MathHelper.Clamp(-m_contentView.Position.X / (m_contentView.Size.X - Size.X), 0, 1);
                m_horizontalScrollBarIndicator.Position = new Vector2(max * percent + m_horizontalScrollBar.Position.X + horizontalScrollIndicatorPadding.Left, m_horizontalScrollBar.Position.Y + horizontalScrollIndicatorPadding.Top);
            }

            if (m_verticalBarVisible)
            {
                Margin verticalScrollIndicatorPadding = new Margin(3, 3, 3, 3);

                float max = m_verticalScrollBar.Size.Y - verticalScrollIndicatorPadding.Top - verticalScrollIndicatorPadding.Bottom - m_verticalScrollBarIndicator.Size.Y;

                float percent = MathHelper.Clamp(-m_contentView.Position.Y / (m_contentView.Size.Y - Size.Y), 0, 1);
                m_verticalScrollBarIndicator.Position = new Vector2(m_verticalScrollBar.Position.X + verticalScrollIndicatorPadding.Left, max * percent + m_verticalScrollBar.Position.Y + verticalScrollIndicatorPadding.Top);
            }
        }
    }
}

