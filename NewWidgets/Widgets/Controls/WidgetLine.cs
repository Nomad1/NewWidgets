using System;
using System.Numerics;

#if RUNMOBILE
using RunMobile.Utility;
#else
using NewWidgets.Utility;
#endif

namespace NewWidgets.Widgets
{
    public class WidgetLine : WidgetBackground
    {
        public new const string ElementType = "line";
        //

        private readonly bool m_simpleLine;
        private Vector2 m_from;
        private Vector2 m_to;
        private float m_gap;
        private float m_width;
        private int m_angleSnap;

        public Vector2 From
        {
            get { return m_from; }
            set { m_from = value; InvalidateLayout(); }
        }

        public Vector2 To
        {
            get { return m_to; }
            set { m_to = value; InvalidateLayout(); }
        }

        public int AngleSnap
        {
            get { return m_angleSnap; }
            set { m_angleSnap = value; InvalidateLayout(); }
        }

        public float Gap
        {
            get { return m_gap; }
            set { m_gap = value; InvalidateLayout(); }
        }

        public float Width
        {
            get { return m_width; }
            set { m_width = value; InvalidateLayout(); }
        }

        public uint Color
        {
            get { return BackgroundColor; }
            set { BackgroundColor = value; }
        }

        /// <summary>
        /// Draws a simple line
        /// </summary>
        /// <param name="style">Style.</param>
        public WidgetLine(WidgetStyle style = default(WidgetStyle))
           : this(ElementType, style)
        {
            
        }

        /// <summary>
        /// Inheritance constructor
        /// </summary>
        /// <param name="style">Style.</param>
        protected WidgetLine(string elementType, WidgetStyle style)
           : base(elementType, style)
        {
            m_simpleLine = true;
            m_from = Vector2.Zero;
            m_to = Vector2.Zero;
            m_gap = 0;
            m_width = 2;
            m_angleSnap = 0;
        }

        /// <summary>
        /// Draws a line
        /// </summary>
        /// <param name="from">From position</param>
        /// <param name="to">To position</param>
        /// <param name="gap">Gap from points to actual line</param>
        /// <param name="width">Width</param>
        /// <param name="angleSnap">Angle snap</param>
        public WidgetLine(Vector2 from, Vector2 to, float gap = 0, float width = 4, int angleSnap = 180)
            : this(ElementType, default(WidgetStyle), from, to, gap, width, angleSnap)
        {
        }

        /// <summary>
        /// Draws a line
        /// </summary>
        /// <param name="style">Style.</param>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="gap">Gap.</param>
        /// <param name="width">Width.</param>
        /// <param name="angleSnap">Angle snap.</param>
        public WidgetLine(WidgetStyle style, Vector2 from, Vector2 to, float gap = 0, float width = 4, int angleSnap = 180)
            : this(ElementType, style, from, to, gap, width, angleSnap)
        {
           
        }

        /// <summary>
        /// Inheritance constructor
        /// </summary>
        /// <param name="elementType"></param>
        /// <param name="style"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="gap"></param>
        /// <param name="width"></param>
        /// <param name="angleSnap"></param>
        protected WidgetLine(string elementType, WidgetStyle style, Vector2 from, Vector2 to, float gap, float width, int angleSnap)
           : base(elementType, style)
        {
            m_from = from;
            m_to = to;
            m_gap = gap;
            m_width = width;
            m_angleSnap = angleSnap;
            m_simpleLine = false;
        }

        protected override void Resize(Vector2 size)
        {
            base.Resize(size);

            if (m_simpleLine)
            {
                m_from = new Vector2(0, size.Y / 2);
                m_to = new Vector2(size.X, size.Y / 2);
                m_width = size.Y / 2;
                InvalidateLayout();
            }
        }

        public override void UpdateLayout()
        {
            if (!m_simpleLine)
            {
                Vector2 direction = m_from - m_to;

                float distance = direction.Length();

                direction /= distance;

                distance -= m_gap * 2;

                Size = new Vector2(distance, m_width);

                if (m_angleSnap != 0)
                    Rotation = (float)Math.Round(Math.Atan2(direction.Y, direction.X) * MathHelper.Rad2Deg / m_angleSnap, MidpointRounding.AwayFromZero) * m_angleSnap;
                else
                    Rotation = (float)(Math.Atan2(direction.Y, direction.X) * MathHelper.Rad2Deg);

                double angle = MathHelper.Deg2Rad * Rotation;

                direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));

                Position = m_to + (direction) * m_gap;// - new Vector2(0, m_width / 2);
            }

            base.UpdateLayout();
        }
    }
}
