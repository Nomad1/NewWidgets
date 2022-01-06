using System;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    internal class WidgetParameterAttribute : NameAttribute
    {
        private readonly Type m_type;
        private readonly WidgetParameterInheritance m_inheritance;

        public Type Type
        {
            get { return m_type; }
        }

        public WidgetParameterInheritance Inheritance
        {
            get { return m_inheritance; }
        }

        public WidgetParameterAttribute(string name, Type type, WidgetParameterInheritance inheritance)
            : base(name)
        {
            m_type = type ?? typeof(string);
            m_inheritance = inheritance;
        }
    }

    /// <summary>
    /// Helper attribute for pre-defined parameters
    /// </summary>
    internal class WidgetCSSParameterAttribute : WidgetParameterAttribute
    {
        public WidgetCSSParameterAttribute(string name, Type type = null, WidgetParameterInheritance inheritance = WidgetParameterInheritance.Initial)
            : base(name, type, inheritance)
        {
        }
    }

    internal class WidgetXMLParameterAttribute : WidgetParameterAttribute
    {
        public WidgetXMLParameterAttribute(string name, Type type = null, WidgetParameterInheritance inheritance = WidgetParameterInheritance.Initial)
            : base("-nw-" + name, type, inheritance)
        {
        }
    }

    /// <summary>
    /// Helper attribute to map CSS pseudo-classes to WidgetStyleType
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Enum, AllowMultiple = true)]
    internal class WidgetPseudoClassAttribute : NameAttribute
    {
        public WidgetPseudoClassAttribute(string name)
            : base(name)
        {
        }
    }
}
