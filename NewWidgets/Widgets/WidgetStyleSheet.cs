using System;
using System.Collections.Generic;
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
    /// Style sheet for various widget parameters
    /// </summary>
    public struct WidgetStyleSheet
    {
        private class StyleSheetData
        {
            private StyleSheetData m_parent;
            private readonly IDictionary<WidgetParameterIndex, object> m_indexedParameters = new Dictionary<WidgetParameterIndex, object>();

            public StyleSheetData(StyleSheetData parent)
            {
                m_parent = parent;
            }

            public void SetParent(StyleSheetData parent)
            {
                m_parent = parent;
            }

            public object GetParameter(WidgetParameterIndex index)
            {
                object result;

                // check if we have this parameter in local dictionary

                if (!m_indexedParameters.TryGetValue(index, out result))
                {
                    if (m_parent != null)
                        result = m_parent.GetParameter(index);
                }

                return result;
            }

            public object GetParameter(string name)
            {
                return GetParameter(IndexNameMap<WidgetParameterIndex>.GetIndexByName(name));
            }

            public void SetParameter(WidgetParameterIndex index, object value)
            {
                m_indexedParameters[index] = value;
            }

            public void SetParameter(string name, object value)
            {
                SetParameter(IndexNameMap<WidgetParameterIndex>.GetIndexByName(name), value);
            }
        }

        private string m_name;
        private int m_instancedFor; // This variable contains hash code of specific object for which this instance of style sheet was created
                                    // raises assertion if zero and any setter is called

        private StyleSheetData m_data;
        // Internal properties

        public string Name
        {
            get { return m_name; }
        }

        public bool IsEmpty
        {
            get { return m_data == null; }
        }

        internal int InstancedFor
        {
            get { return m_instancedFor; }
            set { m_instancedFor = value; }
        }

        internal WidgetStyleSheet(string name, WidgetStyleSheet parent)
        {
            m_name = name;
            m_data = new StyleSheetData(parent.m_data);
            m_instancedFor = 0;
        }

        /// <summary>
        /// This method is needed to repair hierarchy that could be broken because of premature load
        /// </summary>
        /// <param name="parent">Parent.</param>
        internal void SetParent(WidgetStyleSheet parent)
        {
            m_data.SetParent(parent.m_data);
        }

        private bool MakeWritable(object instance)
        {
            if (instance == null)
                return false;

            int instanceHash = instance.GetHashCode();

            if (instanceHash == m_instancedFor)
                return true;

            m_instancedFor = instanceHash;
            m_name = m_name + "_" + m_instancedFor;
            m_data = new StyleSheetData(m_data);

            return true;
        }

        public override string ToString()
        {
            return string.Format("Style: {0}, instanced {1}", m_name, m_instancedFor != 0);
        }

        internal T Get<T>(WidgetParameterIndex index, T defaultValue)
        {
            object result = m_data.GetParameter(index);

            if (result == null)
                return defaultValue;

            if (result.GetType() != typeof(T))
                throw new WidgetException("Trying to retrieve parameter " + index + " with cast to incompatible type " + typeof(T));

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
            object result = m_data.GetParameter(name);

            if (result == null)
                return defaultValue;

            if (result.GetType() != typeof(T))
                throw new WidgetException("Trying to retrieve parameter " + name + " with cast to incompatible type " + typeof(T));

            return (T)result;
        }

        internal void Set(object instance, WidgetParameterIndex index, object value)
        {
            if (instance != null)
                MakeWritable(instance);

            m_data.SetParameter(index, value);
        }
      
        /// <summary>
        /// Set the specified parameter by name
        /// </summary>
        /// <param name="instance">Instance.</param>
        /// <param name="name">Name.</param>
        /// <param name="value">Value.</param>
        public void Set(object instance, string name, string value)
        {
            if (instance != null)
                MakeWritable(instance);

            m_data.SetParameter(name, value);
        }
    }
}
