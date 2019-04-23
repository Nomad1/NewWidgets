using NewWidgets.Utility;

namespace NewWidgets.Widgets.Styles
{
    /// <summary>
    /// Style sheet for buttons
    /// </summary>
    public class WidgetCheckBoxStyleSheet : WidgetBackgroundStyleSheet
    {
        [WidgetStyleValue("text_padding")]
        private Margin m_textPadding = new Margin(0);

        [WidgetStyleValue("image_padding")]
        private Margin m_imagePadding = new Margin(0);

        [WidgetStyleValue("image_style")]
        private WidgetStyleReference m_imageStyle = WidgetImage.DefaultStyle;

        [WidgetStyleValue("text_style")]
        private WidgetStyleReference m_textStyle = WidgetLabel.DefaultStyle;

        public Margin TextPadding
        {
            get { return m_textPadding; }
            internal set { m_textPadding = value; CheckReadonly(); }
        }

        public Margin ImagePadding
        {
            get { return m_imagePadding; }
            internal set { m_imagePadding = value; CheckReadonly(); }
        }

        public WidgetStyleReference ImageStyle
        {
            get { return m_imageStyle; }
        }

        public WidgetStyleReference TextStyle
        {
            get { return m_textStyle; }
        }

        [WidgetStyleValue("check_image")]
        internal string CheckImage
        {
            get { return m_imageStyle.Get<WidgetImageStyleSheet>().Image; }
            set { m_imageStyle.Get<WidgetImageStyleSheet>(this).Image = value; }
        }
    }
}
