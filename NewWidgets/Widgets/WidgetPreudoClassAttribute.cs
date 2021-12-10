using System;

namespace NewWidgets.Widgets
{
    /// <summary>
    /// Helper attribute to map CSS pseudo-classes to WidgetStyleType
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    internal class WidgetPseudoClassAttribute : Attribute
    {
        private readonly string m_name;

        public string Name
        {
            get { return m_name; }
        }

        public WidgetPseudoClassAttribute(string name)
        {
            m_name = name;
        }
    }
}
