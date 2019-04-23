using System.Numerics;
using NewWidgets.Utility;

namespace NewWidgets.Widgets.Styles
{
    /// <summary>
    /// Base class for widgets with background
    /// </summary>
    public class WidgetBackgroundStyleSheet : WidgetStyleSheet
    {
        [WidgetStyleValue("back_style")]
        protected WidgetBackgroundStyle m_backgroundStyle = WidgetBackgroundStyle.None;

        [WidgetStyleValue("back_depth")]
        private WidgetBackgroundDepth m_backgroundDepth = WidgetBackgroundDepth.Back;

        [WidgetStyleValue("back_image")]
        private string m_backgroundTexture = "";

        [WidgetStyleValue("back_scale")]
        private float m_backgroundScale = 1.0f;

        [WidgetStyleValue("back_angle")]
        private float m_backgroundRotation = 0.0f;

        [WidgetStyleValue("back_pivot")]
        private Vector2 m_backgroundPivot = new Vector2(0.5f, 0.5f);

        [WidgetStyleValue("back_padding")]
        private Margin m_backgroundPadding = new Margin(0);

        [WidgetStyleValue("back_color")]
        private int m_backgroundColor = 0xffffff;

        [WidgetStyleValue("back_opacity")]
        private float m_backgroundOpacity = 1.0f;

        public WidgetBackgroundStyle BackgroundStyle
        {
            get { return m_backgroundStyle; }
            internal set { m_backgroundStyle = value; CheckReadonly(); }
        }

        public WidgetBackgroundDepth BackgroundDepth
        {
            get { return m_backgroundDepth; }
            internal set { m_backgroundDepth = value; CheckReadonly(); }
        }

        public Margin BackgroundPadding
        {
            get { return m_backgroundPadding; }
            internal set { m_backgroundPadding = value; CheckReadonly(); }
        }

        public string BackgroundTexture
        {
            get { return m_backgroundTexture; }
            internal set { m_backgroundTexture = value; CheckReadonly(); }
        }

        public Vector2 BackgroundPivot
        {
            get { return m_backgroundPivot; }
            internal set { m_backgroundPivot = value; CheckReadonly(); }
        }

        public float BackgroundScale
        {
            get { return m_backgroundScale; }
            internal set { m_backgroundScale = value; CheckReadonly(); }
        }

        public float BackgroundRotation
        {
            get { return m_backgroundRotation; }
            internal set { m_backgroundRotation = value; CheckReadonly(); }
        }

        public float BackgroundOpacity
        {
            get { return m_backgroundOpacity; }
            internal set { m_backgroundOpacity = value; CheckReadonly(); }
        }

        public int BackgroundColor
        {
            get { return m_backgroundColor; }
            internal set { m_backgroundColor = value; CheckReadonly(); }
        }
    }
}
