using NewWidgets.Utility;

namespace NewWidgets.Widgets.Styles
{
    /// <summary>
    /// Style sheet for buttons
    /// </summary>
    public class WidgetButtonStyleSheet : WidgetBackgroundStyleSheet
    {
        private ButtonLayout m_buttonLayout = ButtonLayout.Center;
        private Margin m_textPadding = new Margin(0);
        private Margin m_imagePadding = new Margin(0);
        private string m_imageStyle = "";
        private string m_textStyle = "";

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

        public string ImageStyle
        {
            get { return m_imageStyle; }
        }

        public string TextStyle
        {
            get { return m_textStyle; }
        }

        public WidgetButtonStyleSheet(string name, WidgetStyleSheet parent)
            : base(name, parent)
        {
            WidgetButtonStyleSheet buttonParent = parent as WidgetButtonStyleSheet;

            if (buttonParent != null)
            {
                m_textPadding = buttonParent.m_textPadding;
                m_imagePadding = buttonParent.m_imagePadding;
                m_buttonLayout = buttonParent.m_buttonLayout;
                m_imageStyle = buttonParent.m_imageStyle;
                m_textStyle = buttonParent.m_textStyle;
            }
        }

        protected override bool ParseParameter(string name, string value)
        {
            if (base.ParseParameter(name, value))
                return true;

            switch (name)
            {
                case "text_padding":
                    m_textPadding = MarginParse(value);
                    break;
                case "image_padding":
                    m_imagePadding = MarginParse(value);
                    break;
                case "image_style":
                    m_imageStyle = value;
                    break;
                case "text_style":
                    m_textStyle = value;
                    break;
                case "m_buttonLayout":
                    m_buttonLayout = EnumParse<ButtonLayout>(value);
                    break;
                default:
                    return false;
            }
            return true;
        }

        protected override WidgetStyleSheet Clone()
        {
            return new WidgetButtonStyleSheet(null, this);
        }
    }
}
