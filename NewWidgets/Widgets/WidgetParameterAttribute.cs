using System;

namespace NewWidgets.Widgets
{
    /// <summary>
    /// Helper attribute for pre-defined parameters. It's faster to have this than look up strings all the time
    /// </summary>
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
