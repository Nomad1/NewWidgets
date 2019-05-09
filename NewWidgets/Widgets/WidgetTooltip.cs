using System.Numerics;
using System.Drawing;

using NewWidgets.UI;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    public class WidgetTooltip : WidgetPanel
    {
        private Vector2 m_shift;
        private RectangleF m_region;

        public Vector2 Shift
        {
            get { return m_shift; }
            set { m_shift = value; }
        }

        public RectangleF Region
        {
            get { return m_region; }
            set { m_region = value; }
        }

        public WidgetTooltip(WidgetStyleSheet style)
            : base(style)
        {
        }

        public virtual void UpdatePosition(Vector2 position)
        {
            Vector2 tooltipSize = Transform.ActualScale * Size;

            position += m_shift;

            /*RectangleF rect = new Rectangle(position, tooltipSize);

            if (rect.IntersectsWith(Region))
            {

            }*/


            if (position.X < 0)
                position.X = 0;

            if (position.Y < 0)
                position.Y = 0;

            if (position.X + tooltipSize.X > WindowController.Instance.ScreenWidth)
                position.X = WindowController.Instance.ScreenWidth - tooltipSize.X;
                //position.X -= m_shift.X + tooltipSize.X;

            if (position.Y + tooltipSize.Y> WindowController.Instance.ScreenHeight)
                //position.Y = WindowController.Instance.ScreenHeight - tooltipSize.Y;
                position.Y -= m_shift.Y + tooltipSize.Y;

            Vector2 pos = Parent.Transform.GetClientPoint(position);

            Position = new Vector2((int)pos.X, (int)pos.Y);
        }
              
        public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            return false;
        }

        private bool UnHoverTouch(float x, float y, bool press, bool unpress, int pointer)
        {
            if (Visible && !Region.Contains(x,y))
            {
                Hide();
            }
            return false;
        }

        #region Static tooltip manager

        private static WidgetTooltip s_currentTooltip = null;

        public static WidgetTooltip CurrentTooltip
        {
            get { return s_currentTooltip; }
        }

        public static void Show(WidgetTooltip tooltip, Vector2 position, RectangleF region)
        {
            Hide();

            tooltip.ZIndex = int.MaxValue;
            WidgetManager.GetTopmostWindow().AddChild(tooltip);
            tooltip.Update(); // make sure all sizes are settled up

            tooltip.Region = region;
            tooltip.UpdatePosition(position);

            WindowController.Instance.OnTouch += tooltip.UnHoverTouch;

            s_currentTooltip = tooltip;
        }
              
        public static void Hide()
        {
            if (s_currentTooltip != null)
            {
                WindowController.Instance.OnTouch -= s_currentTooltip.UnHoverTouch;
                s_currentTooltip.Remove();
                s_currentTooltip = null;
            }
        }

        #endregion
    }
}

