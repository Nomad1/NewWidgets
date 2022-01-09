using System;
using System.Collections.Generic;
using System.Reflection;

namespace NewWidgets.Widgets
{
    internal interface IParameterProcessor
    {
        void Init(string target, Type type, string[] parameters);
        void Process(IDictionary<WidgetParameterIndex, object> data, string value);
    }

    internal static class WidgetParameterMap
    {
        private static readonly IDictionary<string, WidgetParameterIndex> s_indexCache = new Dictionary<string, WidgetParameterIndex>();
        
        private static readonly IDictionary<WidgetParameterIndex, WidgetParameterAttribute> s_attributeCache = new Dictionary<WidgetParameterIndex, WidgetParameterAttribute>();

        private static readonly IDictionary<string, IParameterProcessor> s_parserCache = new Dictionary<string, IParameterProcessor>();

        static WidgetParameterMap()
        {
            FieldInfo[] fields = typeof(WidgetParameterIndex).GetFields(BindingFlags.Public | BindingFlags.Static);

            foreach (FieldInfo field in fields)
            {
                WidgetParameterIndex index = (WidgetParameterIndex)field.GetValue(null);

                foreach (WidgetParameterAttribute attribute in field.GetCustomAttributes(typeof(WidgetParameterAttribute), true))
                {
                    s_indexCache[attribute.Name] = index;
                    s_attributeCache[index] = attribute; // last attribute is considered main

                    IParameterProcessor processor = attribute.ProcessorType != null
                        ? (IParameterProcessor)Activator.CreateInstance(attribute.ProcessorType)
                        : new DefaultProcessor();

                    processor.Init(attribute.Name, attribute.Type, attribute.ProcessorParams);

                    s_parserCache[attribute.XmlName] = processor;
                }
            }
        }


        /// <summary>
        /// Returns first attribute of specified TAttribute type. Note that if there are many attributes they will be skipped
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="index"></param>
        /// <returns></returns>
        public static WidgetParameterAttribute GetAttributeByIndex(WidgetParameterIndex index)
        {
            WidgetParameterAttribute result;

            if (s_attributeCache.TryGetValue(index, out result))
                return result;

            return null;
        }

        /// <summary>
        /// Gets parameter CSS name by index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static string GetNameByIndex(WidgetParameterIndex index)
        {
            WidgetParameterAttribute result;

            if (s_attributeCache.TryGetValue(index, out result))
            {
                return result.Name;
            }

            return index.ToString();
        }

        /// <summary>
        /// Gets parameter index by CSS name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static WidgetParameterIndex GetIndexByName(string name)
        {
            WidgetParameterIndex result;

            if (s_indexCache.TryGetValue(name, out result))
                return result;

            return 0;
        }

        public static IParameterProcessor GetProcessorByXmlName(string name)
        {
            IParameterProcessor result;

            if (s_parserCache.TryGetValue(name, out result))
                return result;

            return null;
        }
    }
}

