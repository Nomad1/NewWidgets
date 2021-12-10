#define MEMORY_PRIORITY // undefine for speed priority

using System;
using System.Collections.Generic;

namespace NewWidgets.Widgets
{
    
    /// <summary>
    /// Style sheet for various widget parameters
    /// </summary>
    public struct WidgetStyleSheet
    {
        private class StyleSheetData
        {
            private StyleSheetData m_parent;
#if MEMORY_PRIORITY
            private readonly IDictionary<WidgetParameterIndex, object> m_indexedParameters = new Dictionary<WidgetParameterIndex, object>();
#else
            public readonly object[] m_indexedParameters = new object[(int)WidgetParameterIndex.Max]; // internal storage for known indexed parameters
#endif
            private readonly IDictionary<string, object> m_namedParameters = new Dictionary<string, object>(); // external storage for custom parameters

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

#if MEMORY_PRIORITY
                if (!m_indexedParameters.TryGetValue(index, out result))
#else
                result = m_indexedParameters[(int)index];
                if (result == null)
#endif
                {
                    if (m_parent != null)
                        result = m_parent.GetParameter(index);
                }

                return result;
            }

            public object GetParameter(string name)
            {
                object result;

                if (m_namedParameters.TryGetValue(name, out result))
                    return result; // returns string, not object!

                Tuple<WidgetParameterIndex, Type> index = WidgetManager.GetParameterIndexByName(name);

                if (index != null)
                    return GetParameter(index.Item1);

                // we need this to find non-indexed named params in parent tree
                if (m_parent != null)
                    return m_parent.GetParameter(name);

                return null;
            }

            public void SetParameter(WidgetParameterIndex index, object value)
            {
#if MEMORY_PRIORITY
                m_indexedParameters[index] = value;
#else
                m_indexedParameters[(int)index] = value;
#endif
            }

            public void SetParameter(string name, object value)
            {
                Tuple<WidgetParameterIndex,Type> index = WidgetManager.GetParameterIndexByName(name);

                if (index != null)
                {
                    if (index.Item2 != value.GetType())
                        throw new WidgetException("Invalid data of type " + value.GetType() + " set for index " + name);

                    SetParameter(index.Item1, value);
                }
                else
                    m_namedParameters[name] = value;
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
