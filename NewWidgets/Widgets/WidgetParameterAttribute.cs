using System;

namespace NewWidgets.Widgets
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    internal class WidgetParameterAttribute : Attribute
    {
        private readonly string m_name;
        private readonly Type m_type;

        public string Name
        {
            get { return m_name; }
        }

        public Type Type
        {
            get { return m_type; }
        }

        public WidgetParameterAttribute(string name, Type type = null)
        {
            m_name = name;
            m_type = type ?? typeof(string);
        }
    }
}
