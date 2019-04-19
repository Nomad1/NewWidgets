using NewWidgets.UI;
using NewWidgets.Utility;

namespace NewWidgets.Widgets.Styles
{
    /// <summary>
    /// Style for TextEdit type widgets
    /// </summary>
    public class WidgetTextEditStyleSheet : WidgetBackgroundStyleSheet
    {
        [WidgetStyleValue("font")]
        private Font m_font = WidgetManager.MainFont;

        [WidgetStyleValue("font_size")]
        private float m_fontSize = 1.0f;

        [WidgetStyleValue("text_color")]
        private int m_textColor = 0;

        [WidgetStyleValue("cursor_color")]
        private int m_cursorColor = 0;

        [WidgetStyleValue("line_spacing")]
        private float m_lineSpacing = 5.0f;

        [WidgetStyleValue("text_align")]
        private WidgetAlign m_textAlign = WidgetAlign.Left | WidgetAlign.Top;

        [WidgetStyleValue("text_padding")]
        private Margin m_textPadding = new Margin(0);

        [WidgetStyleValue("cursor_char")]
        private string m_cursorChar = "|";

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

        public string CursorChar
        {
            get { return m_cursorChar; }
            internal set { m_cursorChar = value; CheckReadonly(); }
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
    }
}
