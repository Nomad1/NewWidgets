using NewWidgets.Utility;

namespace NewWidgets.Widgets.Styles
{
    /// <summary>
    /// Style sheet for buttons
    /// </summary>
    public class WidgetButtonStyleSheet : WidgetBackgroundStyleSheet
    {
        [WidgetStyleValue("button_layout")]
        private ButtonLayout m_buttonLayout = ButtonLayout.Center;

        [WidgetStyleValue("text_padding")]
        private Margin m_textPadding = new Margin(0);

        [WidgetStyleValue("image_padding")]
        private Margin m_imagePadding = new Margin(0);

        [WidgetNestedStyle]
        private WidgetImageStyleSheet m_imageStyle = new WidgetImageStyleSheet();

        [WidgetNestedStyle]
        private WidgetTextStyleSheet m_textStyle = new WidgetTextStyleSheet();
       
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

        public WidgetStyleReference ImageStyle
        {
            get { return new WidgetStyleReference(Name + ":image", m_imageStyle); }
        }

        public WidgetStyleReference TextStyle
        {
            get { return new WidgetStyleReference(Name + ":text", m_textStyle); }
        }
    }
}
