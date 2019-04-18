using System.Numerics;
using NewWidgets.Utility;

namespace NewWidgets.Widgets.Styles
{
    /// <summary>
    /// Base class for WidgetImage
    /// </summary>
    public class WidgetImageStyleSheet : WidgetStyleSheet
    {
        private WidgetBackgroundStyle m_imageStyle = WidgetBackgroundStyle.None;
        private string m_image = "";
        private float m_imageRotation = 0.0f;
        private Vector2 m_imagePivot = new Vector2(0);
        private Margin m_imagePadding = new Margin(0);
        private int m_imageColor = 0xffffff;
        private float m_imageOpacity = 1.0f;

        public WidgetBackgroundStyle ImageStyle
        {
            get { return m_imageStyle; }
            internal set { m_imageStyle = value; CheckReadonly(); }
        }

        public Margin ImagePadding
        {
            get { return m_imagePadding; }
            internal set { m_imagePadding = value; CheckReadonly(); }
        }
       
        public Vector2 ImagePivot
        {
            get { return m_imagePivot; }
            internal set { m_imagePivot = value; CheckReadonly(); }
        }

        public float ImageRotation
        {
            get { return m_imageRotation; }
            internal set { m_imageRotation = value; CheckReadonly(); }
        }

        public float ImageOpacity
        {
            get { return m_imageOpacity; }
            internal set { m_imageOpacity = value; CheckReadonly(); }
        }

        public string Image
        {
            get { return m_image; }
            internal set { m_image = value; CheckReadonly(); }
        }

        public int ImageColor
        {
            get { return m_imageColor; }
            internal set { m_imageColor = value; CheckReadonly(); }
        }

        public WidgetImageStyleSheet(string name, WidgetStyleSheet parent)
            : base(name, parent)
        {
            WidgetImageStyleSheet imageParent = parent as WidgetImageStyleSheet;

            if (imageParent != null)
            {
                m_imageColor = imageParent.m_imageColor;
                m_imageOpacity = imageParent.m_imageOpacity;
                m_imageStyle = imageParent.m_imageStyle;
                m_image = imageParent.m_image;
                m_imagePivot = imageParent.m_imagePivot;
                m_imagePadding = imageParent.m_imagePadding;
            }
        }

        protected override bool ParseParameter(string name, string value)
        {
            if (base.ParseParameter(name, value))
                return true;

            switch (name)
            {
                case "image_style":
                    m_imageStyle = EnumParse<WidgetBackgroundStyle>(value);
                    break;
                case "image":
                    m_image = value;
                    break;
                case "image_angle":
                    m_imageRotation = FloatParse(value);
                    break;
                case "image_pivot":
                    m_imagePivot = Vector2Parse(value);
                    break;
                case "image_color":
                    m_imageColor = ColorParse(value);
                    break;
                case "image_opacity":
                    m_imageOpacity = FloatParse(value);
                    break;
                case "image_padding":
                    m_imagePadding = MarginParse(value);
                    break;
                default:
                    return false;
            }
            return true;
        }

        protected override WidgetStyleSheet Clone()
        {
            return new WidgetImageStyleSheet(null, this);
        }
    }
}
