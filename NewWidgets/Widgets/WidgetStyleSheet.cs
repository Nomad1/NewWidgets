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

        public T GetParameter<T>(WidgetParameterIndex index, T defaultValue)
        {
            object result;
            if (!m_parameters.TryGetValue(index, out result) || result.GetType() != typeof(T))
                return defaultValue;

            return (T)result;
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
            {
                var attr = WidgetParameterMap.GetAttributeByIndex(pair.Key);

                builder.AppendFormat("    {0}: {1};\n", attr.Name, ConversionHelper.FormatValue(pair.Value.GetType(), attr.UnitType, pair.Value));
            }

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

        private readonly LinkedList<ValueTuple<StyleNode, StyleNodeMatch>> m_data;
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

        internal WidgetStyleSheet(string name, ICollection<ValueTuple<StyleNode, StyleNodeMatch>> data)
        {
            m_name = name;

            m_hasOwnStyle = false;

            m_data = new LinkedList<ValueTuple<StyleNode, StyleNodeMatch>>();

            if (data != null)
                foreach (ValueTuple<StyleNode, StyleNodeMatch> sheetData in data)
                    m_data.AddFirst(sheetData);
        }

        internal void SetOwnStyle(StyleSheetData ownStyle)
        {
            if (m_hasOwnStyle)
                throw new WidgetException("Trying to set own style when it is already set!");

            m_data.AddFirst(new ValueTuple<StyleNode, StyleNodeMatch>(new StyleNode(new StyleSelectorList(new StyleSelector("", null, "")), ownStyle), StyleNodeMatch.OwnStyle)); // local style, the same as HTML tag style="..."

            m_hasOwnStyle = true;
        }

        internal T Get<T>(WidgetParameterIndex index, T defaultValue)
        {
            WidgetParameterAttribute attr = WidgetParameterMap.GetAttributeByIndex(index);

            bool inherited = attr != null && attr.Inheritance == WidgetParameterInheritance.Inherit; // should be read fromParent and GrandParent nodes

            object result = null;

            LinkedListNode<ValueTuple<StyleNode, StyleNodeMatch>> node = m_data.First;

            // Let's iterate all the styles and select properties based on inheritance

            while (node != null)
            {
                StyleNode data = node.Value.Item1;

                if (((StyleSheetData)data.Data).TryGetParameter(index, out result))
                {
                    break;
                }

                if (node.Next == null)
                    break;

                //if ((node.Value.Item2 & StyleNodeMatch.OwnStyle) != 0) // owns styles has maximum priority but if not found, they won't stop the cascade
                //{
                //    node = node.Next;
                //    continue;
                //}

                // if next style is the same as this one less on pseudo-class, we can think of it as a parent and do a one-time exception for data lookup
                // otherwise we need to check if the property inheritance is Initial and then break

                StyleNodeMatch nextNodeType = node.Next.Value.Item2;

                // Pseudo classes should inherit data from their parent classes
                if (!inherited && (nextNodeType & (StyleNodeMatch.Parent | StyleNodeMatch.GrandParent)) != 0)
                    break;

                node = node.Next;
            }

            if (result == null)
                return defaultValue;

            if (result.GetType() != typeof(T))
                throw new WidgetException(string.Format("Trying to retrieve parameter {0} with cast to incompatible type {1} from type {2}", index, typeof(T), result.GetType()));

            return (T)result;
        }
        /*
        internal bool TryGetValue<T>(WidgetParameterIndex index, out T tresult)
        {
            object result = null;

            foreach (StyleNode data in m_data)
                if (((StyleSheetData)data.Data).TryGetParameter(index, out result))
                {
                    break;
                }

            if (result == null)
            {
                tresult = default(T);
                return false;
            }

            if (result.GetType() != typeof(T))
                throw new WidgetException(string.Format("Trying to retrieve parameter {0} with cast to incompatible type {1} from type {2}", index, typeof(T), result.GetType()));

            tresult = (T)result;
            return true;
        }
        */
        /// <summary>
        /// Retrieve parameter by name
        /// </summary>
        /// <returns>The parameter.</returns>
        /// <param name="name">Name.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public T Get<T>(string name, T defaultValue)
        {
            return Get(WidgetParameterMap.GetIndexByName(name), defaultValue);
        }

        internal void Set(WidgetParameterIndex index, object value)
        {
            if (!m_hasOwnStyle)
                throw new WidgetException("Trying to set data for read only style!");

            WidgetParameterAttribute attribute = WidgetParameterMap.GetAttributeByIndex(index);

            if (attribute != null && attribute.Type != null && value != null)
                if (value.GetType() != attribute.Type)
                    throw new WidgetException(string.Format("Setting attribute {0} to value {1} type {2} while expecting type {3}", index, value, value.GetType(), attribute.Type));

            ((StyleSheetData)m_data.First.Value.Item1.Data).SetParameter(index, value);
        }
      
        /// <summary>
        /// Set the specified parameter by name
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="value">Value.</param>
        public void Set(string name, object value)
        {
            Set(WidgetParameterMap.GetIndexByName(name), value);
        }

        public override string ToString()
        {
            IStyleData temp = new StyleSheetData();

            for (var node = m_data.Last; node != null; node = node.Previous)
                temp.LoadData(node.Value.Item1.Data);

            return temp.ToString();
        }
    }
}
