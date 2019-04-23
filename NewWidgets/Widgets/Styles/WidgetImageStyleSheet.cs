using System.Numerics;
using NewWidgets.Utility;

namespace NewWidgets.Widgets.Styles
{
    /// <summary>
    /// Base class for WidgetImage
    /// </summary>
    public class WidgetImageStyleSheet : WidgetStyleSheet
    {
        [WidgetStyleValue("image")]
        private string m_image = "";

        [WidgetStyleValue("image_style")]
        private WidgetBackgroundStyle m_imageStyle = WidgetBackgroundStyle.Image;

        [WidgetStyleValue("image_angle")]
        private float m_imageRotation = 0.0f;

        [WidgetStyleValue("image_pivot")]
        private Vector2 m_imagePivot = new Vector2();

        [WidgetStyleValue("image_padding")]
        private Margin m_imagePadding = new Margin(0);

        [WidgetStyleValue("image_color")]
        private int m_imageColor = 0xffffff;

        [WidgetStyleValue("image_opacity")]
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
    }
}
