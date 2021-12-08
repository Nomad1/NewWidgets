using System;

namespace NewWidgets.Widgets
{
    /// <summary>
    /// Helper exeption class for widgets
    /// </summary>
    public class WidgetException : ApplicationException
    {
        public WidgetException()
            : base()
        {
        }

        public WidgetException(string message)
            : base(message)
        {
        }

        public WidgetException(string message, Exception ex)
            : base(message, ex)
        {
        }
    }
}
