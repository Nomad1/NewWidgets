using System.Numerics;
using NewWidgets.Utility;

namespace NewWidgets.Widgets.Styles
{
    /// <summary>
    /// Base class for widgets with background
    /// </summary>
    public class WidgetBackgroundStyleSheet : WidgetStyleSheet
    {
        private WidgetBackgroundStyle m_backgroundStyle = WidgetBackgroundStyle.None;
        private WidgetBackgroundDepth m_backgroundDepth = WidgetBackgroundDepth.Back;
        private string m_backgroundTexture = "";
        private float m_backgroundScale = 1.0f;
        private float m_backgroundRotation = 0.0f;
        private Vector2 m_backgroundPivot = new Vector2(0);
        private Margin m_backgroundPadding = new Margin(0);
        private int m_backgroundColor = 0xffffff;
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

        public WidgetBackgroundStyleSheet(string name, WidgetStyleSheet parent)
            : base(name, parent)
        {
            WidgetBackgroundStyleSheet backParent = parent as WidgetBackgroundStyleSheet;

            if (backParent != null)
            {
                m_backgroundColor = backParent.m_backgroundColor;
                m_backgroundOpacity = backParent.m_backgroundOpacity;
                m_backgroundScale = backParent.m_backgroundScale;
                m_backgroundDepth = backParent.m_backgroundDepth;
                m_backgroundStyle = backParent.m_backgroundStyle;
                m_backgroundTexture = backParent.m_backgroundTexture;
                m_backgroundPivot = backParent.m_backgroundPivot;
                m_backgroundPadding = backParent.m_backgroundPadding;
            }
        }

        protected override bool ParseParameter(string name, string value)
        {
            if (base.ParseParameter(name, value))
                return true;

            switch (name)
            {
                case "back_style":
                    m_backgroundStyle = EnumParse<WidgetBackgroundStyle>(value);
                    break;
                case "back_depth":
                    m_backgroundDepth = EnumParse<WidgetBackgroundDepth>(value);
                    break;
                case "back_image":
                    m_backgroundTexture = value;
                    break;
                case "back_scale":
                    m_backgroundScale = FloatParse(value);
                    break;
                case "back_angle":
                    m_backgroundRotation = FloatParse(value);
                    break;
                case "back_pivot":
                    m_backgroundPivot = Vector2Parse(value);
                    break;
                case "back_color":
                    m_backgroundColor = ColorParse(value);
                    break;
                case "back_opacity":
                    m_backgroundOpacity = FloatParse(value);
                    break;
                case "back_padding":
                    m_backgroundPadding = MarginParse(value);
                    break;
                default:
                    return false;
            }
            return true;
        }

        protected override WidgetStyleSheet Clone()
        {
            return new WidgetBackgroundStyleSheet(null, this);
        }
    }
}
