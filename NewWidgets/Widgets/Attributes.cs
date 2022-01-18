using System;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Enum, AllowMultiple = true)]
    internal class WidgetParameterAttribute : Attribute
    {
       
        private readonly string m_name;
        private readonly string m_xmlName;
        private readonly Type m_type;
        private readonly UnitType m_unitType;
        private readonly WidgetParameterInheritance m_inheritance;
        private readonly Type m_processorType;
        private readonly string [] m_processorParams;

        public string Name
        {
            get { return m_name; }
        }

        public string XmlName
        {
            get { return m_xmlName; }
        }

        public Type Type
        {
            get { return m_type; }
        }

        public UnitType UnitType
        {
            get { return m_unitType; }
        }

        public WidgetParameterInheritance Inheritance
        {
            get { return m_inheritance; }
        }

        public Type ProcessorType
        {
            get { return m_processorType; }
        }

        public string [] ProcessorParams
        {
            get { return m_processorParams; }
        }

        public WidgetParameterAttribute(string name, Type type = null, UnitType unitType = UnitType.None, WidgetParameterInheritance inheritance = WidgetParameterInheritance.Initial)
            : this (name, name, type, unitType, inheritance)
        {
        }

        public WidgetParameterAttribute(string xmlName, string name, Type type = null, UnitType unitType = UnitType.None, WidgetParameterInheritance inheritance = WidgetParameterInheritance.Initial,
            Type processorType = null,
            params string [] parameters)
        {
            m_unitType = unitType;
            m_name = name;
            m_xmlName = xmlName;
            m_type = type ?? typeof(string);
            m_inheritance = inheritance;
            m_processorType = processorType;
            m_processorParams = parameters;
        }
    }


    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Enum, AllowMultiple = true)]
    internal class WidgetPseudoClass : NameAttribute
    {
        public WidgetPseudoClass(string name)
            : base(name)
        {
        }
    }
}
