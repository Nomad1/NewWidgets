using System;
using System.Numerics;

#if RUNMOBILE
using RunMobile.Utility;
#else
using NewWidgets.Utility;
#endif

namespace NewWidgets.Widgets
{
	public class WidgetLine : WidgetImage
	{
		private Vector2 m_from;
		private Vector2 m_to;
		private float m_gap;
		private float m_width;
		private int m_angleSnap;

		private bool m_needLayout;

		public Vector2 From
		{
			get { return m_from; }
			set { m_from = value; m_needLayout = true; }
		}

		public Vector2 To
		{
			get { return m_to; }
			set { m_to = value; m_needLayout = true; }
		}

		public int AngleSnap
		{
			get { return m_angleSnap; }
			set { m_angleSnap = value; m_needLayout = true; }
		}

		public float Gap
		{
			get { return m_gap; }
			set { m_gap = value; m_needLayout = true; }
		}

		public float Width
		{
			get { return m_width; }
			set { m_width = value; m_needLayout = true; }
		}

		/// <summary>
		/// Draws a white line
		/// </summary>
		/// <param name="from">From position</param>
		/// <param name="to">To position</param>
		/// <param name="gap">Gap from points to actual line</param>
		/// <param name="width">Width</param>
		/// <param name="angleSnap">Angle snap</param>
		public WidgetLine(Vector2 from, Vector2 to, float gap = 0, float width = 4, int angleSnap = 180)
		    : base(WidgetBackgroundStyle.ThreeImage, "line_3") // TODO: no constant images!
		{
			ImagePivot = new Vector2(0.5f, 0f);
			Alpha = 0.8f;
			m_from = from;
			m_to = to;
			m_gap = gap;
			m_width = width;	
			m_angleSnap = angleSnap;
			m_needLayout = true;
		}

		private new void Relayout()
		{
			Vector2 direction = m_from - m_to;

			float distance = direction.Length();

            direction /= distance;

            distance -= m_gap * 2;

            Size = new Vector2(distance, m_width);

			if (m_angleSnap != 0)
				Rotation = (float)(Math.Round(Math.Atan2(direction.Y, direction.X) * MathHelper.Rad2Deg / m_angleSnap) * m_angleSnap);
			else
				Rotation = (float)(Math.Atan2(direction.Y, direction.X) * MathHelper.Rad2Deg);

            double angle = MathHelper.Deg2Rad * Rotation;

            direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));

			Position = m_to + (direction) * m_gap;// - new Vector2(0, m_width / 2);

			m_needLayout = false;
		}

		public override bool Update()
		{
			if (m_needLayout)
				Relayout();
			return base.Update();
		}
	}
}
