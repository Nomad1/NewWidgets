using NewWidgets.UI;
using NewWidgets.Utility;

namespace NewWidgets.Widgets.Styles
{
    /// <summary>
    /// Style for TextEdit type widgets
    /// </summary>
    public class WidgetTextEditStyleSheet : WidgetBackgroundStyleSheet
    {
        private Font m_font = WidgetManager.MainFont;
        private float m_fontSize = 1.0f;
        private int m_textColor = 0;
        private int m_cursorColor = 0;
        private float m_lineSpacing = 5.0f;
        private WidgetAlign m_textAlign = WidgetAlign.Left | WidgetAlign.Top;
        private Margin m_textPadding = new Margin(0);

        public float FontSize
        {
            get { return m_fontSize; }
            internal set { m_fontSize = value; CheckReadonly(); }
        }

        public Font Font
        {
            get { return m_font; }
            internal set { m_font = value; CheckReadonly(); }
        }

        public int TextColor
        {
            get { return m_textColor; }
            internal set { m_textColor = value; CheckReadonly(); }
        }

        public int CursorColor
        {
            get { return m_cursorColor; }
            internal set { m_cursorColor = value; CheckReadonly(); }
        }

        public float LineSpacing
        {
            get { return m_lineSpacing; }
            internal set { m_lineSpacing = value; CheckReadonly(); }
        }

        public WidgetAlign TextAlign
        {
            get { return m_textAlign; }
            internal set { m_textAlign = value; CheckReadonly(); }
        }

        public Margin TextPadding
        {
            get { return m_textPadding; }
            internal set { m_textPadding = value; CheckReadonly(); }
        }

        public WidgetTextEditStyleSheet(string name, WidgetStyleSheet parent)
            : base(name, parent)
        {
            WidgetTextEditStyleSheet textEditParent = parent as WidgetTextEditStyleSheet;

            if (textEditParent != null)
            {
                m_font = textEditParent.m_font;
                m_fontSize = textEditParent.m_fontSize;
                m_textColor = textEditParent.m_textColor;
                m_cursorColor = textEditParent.m_cursorColor;
                m_textAlign = textEditParent.m_textAlign;
                m_lineSpacing = textEditParent.m_lineSpacing;
                m_textPadding = textEditParent.m_textPadding;
            }
        }

        protected override bool ParseParameter(string name, string value)
        {
            if (base.ParseParameter(name, value))
                return true;

            switch (name)
            {
                case "font":
                    m_font = WidgetManager.GetFont(value);
                    break;
                case "font_size":
                    m_fontSize = FloatParse(value) * WidgetManager.FontScale;
                    break;
                case "line_spacing":
                    m_fontSize = FloatParse(value);
                    break;
                case "text_color":
                    m_textColor = ColorParse(value);
                    break;
                case "cursor_color":
                    m_cursorColor = ColorParse(value);
                    break;
                case "text_align":
                    m_textAlign = EnumParse<WidgetAlign>(value);
                    break;
                case "text_padding":
                    m_textPadding = MarginParse(value);
                    break;
                default:
                    return false;
            }
            return true;
        }

        protected override WidgetStyleSheet Clone()
        {
            return new WidgetTextEditStyleSheet(null, this);
        }
    }
}
