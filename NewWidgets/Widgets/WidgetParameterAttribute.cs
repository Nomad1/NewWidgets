using System;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    /// <summary>
    /// Helper attribute for pre-defined parameters
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    internal class WidgetParameterAttribute : NameAttribute
    {
        private readonly Type m_type;

        public Type Type
        {
            get { return m_type; }
        }

        public WidgetParameterAttribute(string name, Type type = null)
            : base(name)
        {
            m_type = type ?? typeof(string);
        }
    }
}
