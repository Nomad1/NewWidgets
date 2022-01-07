using System;
using System.Collections.Generic;
using System.Text;
using NewWidgets.UI.Styles;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    //public class WidgetStyleTable
    //{
    //    private readonly string m_elementType;
    //    private readonly string m_elementId;
    //    private readonly string m_elementClass;
    //    private WidgetStyleTable m_parentTable;

    //    //private readonly IDictionary<WidgetState, WidgetStyleSheet> m_styles = new Dictionary<WidgetState, WidgetStyleSheet>();

    //    // in CSS world we could:
    //    // 1. Get names of all parents and their classes to build a string like this: panel .foo #acc.bar button#wee .l
    //    // 2. append our own name in form type#id.class
    //    // 3. append our current state pseudo-class in form :hover
    //    // 4. run a selector function on all possible styles, keep in mind pseudo-classes of all parents, use + > and || combinators, etc.
    //    // 5. look for a property value in this cascading style shit.

    //    // in WSS we will cheat:
    //    // 1. get current style name in form type#id.class:pseudo-class (anything ending with it but with extras in front should be checked somehow)
    //    // 2. if the style is absent strip pseudo-class (starting from less significant bit), than class, then id then try again
    //    // 3. try getting the property from it, otherwise goto 2
    //    // 4. if we have a link to parent style, move to it and goto 1

    //    private WidgetStyleSheet GetStyle(WidgetState state)
    //    {
    //        // 



    //    }

    //}

    /// <summary>
    /// This class is a simple wrapper for IDictionary with LoadData working as AddRange
    /// </summary>
    internal class StyleSheetData : IStyleData
    {
        private readonly IDictionary<WidgetParameterIndex, object> m_parameters;

        public StyleSheetData()
        {
            m_parameters = new Dictionary<WidgetParameterIndex, object>();
        }

        public StyleSheetData(IDictionary<WidgetParameterIndex, object> parameters)
        {
            m_parameters = parameters;
        }

        public bool TryGetParameter(WidgetParameterIndex index, out object result)
        {
            return m_parameters.TryGetValue(index, out result);
        }

        public void SetParameter(WidgetParameterIndex index, object value)
        {
            m_parameters[index] = value;
        }

        void IStyleData.LoadData(IStyleData data)
        {
            StyleSheetData styleData = data as StyleSheetData;

            if (styleData == null)
                throw new ArgumentException("Invalid data providede to LoadData");

            foreach (var pair in styleData.m_parameters)
                m_parameters[pair.Key] = pair.Value;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            foreach (var pair in m_parameters)
                builder.AppendFormat("\t{0}: {1};\n", pair.Key, pair.Value);

            return builder.ToString();
        }
    }

    /// <summary>
    /// This one is a simple wrapper to a collection of StyleSheetData structures for particular widget.
    /// In case of local modifications it also adds a new Data object to the collection to store local changes
    /// </summary>
    internal struct WidgetStyleSheet
    {
        private bool m_hasOwnStyle; // This flag indicates that personal style has been created for the object

        private readonly LinkedList<StyleSheetData> m_data;
        private readonly string m_name;

        // Internal properties

        public bool IsEmpty
        {
            get { return m_data == null; }
        }

        public string Name
        {
            get { return m_name; }
        }

        internal WidgetStyleSheet(string name, ICollection<IStyleData> data)
        {
            m_name = name;

            m_hasOwnStyle = false;

            m_data = new LinkedList<StyleSheetData>();

            if (data != null)
                foreach (StyleSheetData sheetData in data)
                    m_data.AddFirst(sheetData);
        }

        internal void SetOwnStyle(StyleSheetData ownStyle)
        {
            if (m_hasOwnStyle)
                throw new WidgetException("Trying to set own style when it is already set!");

            m_data.AddFirst(ownStyle);

            m_hasOwnStyle = true;
        }

        internal T Get<T>(WidgetParameterIndex index, T defaultValue)
        {
            object result = null;

            foreach (StyleSheetData data in m_data)
                if (data.TryGetParameter(index, out result))
                {
                    break;
                }

            if (result == null)
                return defaultValue;

            if (result.GetType() != typeof(T))
                throw new WidgetException(string.Format("Trying to retrieve parameter {0} with cast to incompatible type {1} from type {2}", index, typeof(T), result.GetType()));

            return (T)result;
        }

        /// <summary>
        /// Retrieve parameter by name
        /// </summary>
        /// <returns>The parameter.</returns>
        /// <param name="name">Name.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public T Get<T>(string name, T defaultValue)
        {
            return Get(IndexNameMap<WidgetParameterIndex>.GetIndexByName(name), defaultValue);
        }

        internal void Set(WidgetParameterIndex index, object value)
        {
            if (!m_hasOwnStyle)
                throw new WidgetException("Trying to set data for read only style!");

            WidgetCSSParameterAttribute attribute = IndexNameMap<WidgetParameterIndex>.GetAttributeByIndex<WidgetCSSParameterAttribute>(index);

            if (attribute != null && attribute.Type != null && value != null)
                if (value.GetType() != attribute.Type)
                    throw new WidgetException(string.Format("Setting attribute {0} to value {1} type {2} while expecting type {3}", index, value, value.GetType(), attribute.Type));

            m_data.First.Value.SetParameter(index, value);
        }
      
        /// <summary>
        /// Set the specified parameter by name
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="value">Value.</param>
        public void Set(string name, object value)
        {
            Set(IndexNameMap<WidgetParameterIndex>.GetIndexByName(name), value);
        }

        public override string ToString()
        {
            IStyleData temp = new StyleSheetData();

            for (var node = m_data.Last; node != null; node = node.Previous)
                temp.LoadData(node.Value);

            return temp.ToString();
        }
    }
}
