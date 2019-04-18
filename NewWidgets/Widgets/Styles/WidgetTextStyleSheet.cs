using NewWidgets.UI;

namespace NewWidgets.Widgets.Styles
{
    /// <summary>
    /// Base style class for widgets with text and no background (WidgetLabel, WidgetText, etc.)
    /// </summary>
    public class WidgetTextStyleSheet : WidgetStyleSheet
    {
        private Font m_font = WidgetManager.MainFont;
        private float m_fontSize = 1.0f;
        private int m_textColor = 0;
        private float m_lineSpacing = 5.0f;
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

        public WidgetTextStyleSheet(string name, WidgetStyleSheet parent)
            : base(name, parent)
        {
            WidgetTextStyleSheet textParent = parent as WidgetTextStyleSheet;

            if (textParent != null)
            {
                m_font = textParent.m_font;
                m_fontSize = textParent.m_fontSize;
                m_textColor = textParent.m_textColor;
                m_textAlign = textParent.m_textAlign;
                m_lineSpacing = textParent.m_lineSpacing;
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
                case "text_align":
                    m_textAlign = EnumParse<WidgetAlign>(value);
                    break;
                default:
                    return false;
            }
            return true;
        }

        protected override WidgetStyleSheet Clone()
        {
            return new WidgetTextStyleSheet(null, this);
        }
    }
}
