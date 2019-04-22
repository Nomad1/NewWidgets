using NewWidgets.Utility;

namespace NewWidgets.Widgets.Styles
{
    /// <summary>
    /// Style sheet for buttons
    /// </summary>
    public class WidgetScrollViewStyleSheet : WidgetBackgroundStyleSheet
    {
        [WidgetStyleValue("button_layout")]
        private ButtonLayout m_buttonLayout = ButtonLayout.Center;

        [WidgetStyleValue("text_padding")]
        private Margin m_textPadding = new Margin(0);

        [WidgetStyleValue("image_padding")]
        private Margin m_imagePadding = new Margin(0);

        [WidgetStyleValue("image_style")]
        private WidgetStyleReference<WidgetImageStyleSheet> m_imageStyle = WidgetImage.DefaultStyle;

        [WidgetStyleValue("text_style")]
        private WidgetStyleReference<WidgetTextStyleSheet> m_textStyle = WidgetLabel.DefaultStyle;

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

        public ButtonLayout ButtonLayout
        {
            get { return m_buttonLayout; }
            internal set { m_buttonLayout = value; CheckReadonly(); }
        }

        public WidgetImageStyleSheet ImageStyle
        {
            get { return m_imageStyle.Style; }
        }

        public WidgetTextStyleSheet TextStyle
        {
            get { return m_textStyle.Style; }
        }
    }
}
