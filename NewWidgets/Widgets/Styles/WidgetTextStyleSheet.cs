using NewWidgets.UI;

namespace NewWidgets.Widgets.Styles
{
    /// <summary>
    /// Base style class for widgets with text and no background (WidgetLabel, WidgetText, etc.)
    /// </summary>
    public class WidgetTextStyleSheet : WidgetStyleSheet
    {
        [WidgetStyleValue("font")]
        private Font m_font = WidgetManager.MainFont;

        [WidgetStyleValue("font_size")]
        private float m_fontSize = 1.0f;

        [WidgetStyleValue("text_color")]
        private int m_textColor = 0;

        [WidgetStyleValue("line_spacing")]
        private float m_lineSpacing = 5.0f;

        [WidgetStyleValue("text_align")]
        private WidgetAlign m_textAlign = WidgetAlign.Left | WidgetAlign.Top;

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
    }
}
