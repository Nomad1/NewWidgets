
namespace NewWidgets.Widgets.Styles
{
    /// <summary>
    /// Style sheet for scroll view
    /// </summary>
    public class WidgetScrollViewStyleSheet : WidgetBackgroundStyleSheet
    {
        [WidgetStyleValue("horizontal_scroll")]
        private WidgetStyleReference m_horizontalScrollStyle = WidgetBackground.DefaultStyle;

        [WidgetStyleValue("vertical_scroll")]
        private WidgetStyleReference m_verticalScrollStyle = WidgetBackground.DefaultStyle;

        [WidgetStyleValue("horizontal_indicator")]
        private WidgetStyleReference m_horizontalIndicatorStyle = WidgetBackground.DefaultStyle;

        [WidgetStyleValue("vertical_indicator")]
        private WidgetStyleReference m_verticalIndicatorStyle = WidgetBackground.DefaultStyle;

        public WidgetStyleReference HorizontalScrollStyle
        {
            get { return m_horizontalScrollStyle; }
        }

        public WidgetStyleReference VerticalScrollStyle
        {
            get { return m_verticalScrollStyle; }
        }

        public WidgetStyleReference HorizontalIndicatorStyle
        {
            get { return m_horizontalIndicatorStyle; }
        }

        public WidgetStyleReference VerticalIndicatorStyle
        {
            get { return m_verticalIndicatorStyle; }
        }
    }
}
