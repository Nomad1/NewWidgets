using NewWidgets.UI;
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

        [WidgetStyleValue("image_style")]
        private WidgetStyleReference m_imageStyle = WidgetImage.DefaultStyle;

        [WidgetStyleValue("text_style")]
        private WidgetStyleReference m_textStyle = WidgetLabel.DefaultStyle;


        [WidgetStyleValue("font")]
        internal Font Font
        {
            get { return m_textStyle.Get<WidgetTextStyleSheet>().Font; }
            set { m_textStyle.Get<WidgetTextStyleSheet>(this).Font = value; }
        }

        [WidgetStyleValue("font_size")]
        internal float FontSize
        {
            get { return m_textStyle.Get<WidgetTextStyleSheet>().FontSize; }
            set { m_textStyle.Get<WidgetTextStyleSheet>(this).FontSize = value; }
        }

        [WidgetStyleValue("text_color")]
        internal int TextColor
        {
            get { return m_textStyle.Get<WidgetTextStyleSheet>().TextColor; }
            set { m_textStyle.Get<WidgetTextStyleSheet>(this).TextColor = value; }
        }

        [WidgetStyleValue("text_align")]
        internal WidgetAlign TextAlign
        {
            get { return m_textStyle.Get<WidgetTextStyleSheet>().TextAlign; }
            set { m_textStyle.Get<WidgetTextStyleSheet>(this).TextAlign = value; }
        }

        [WidgetStyleValue("image")]
        internal string Image
        {
            get { return m_imageStyle.Get<WidgetImageStyleSheet>().Image; }
            set { m_imageStyle.Get<WidgetImageStyleSheet>(this).Image = value; }
        }

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
            get { return m_imageStyle; }
        }

        public WidgetStyleReference TextStyle
        {
            get { return m_textStyle; }
        }
    }
}
