using System;
using System.Collections.Generic;

namespace StyleTree
{
    /// <summary>
    /// Simple data storage for properties. Theoretically short-hand properties should be expanded before going there
    /// Also there is no support for expressions ATM
    /// </summary>
    internal class StyleData
    {
        private IDictionary<string, string> m_properties;

        public bool IsEmpty
        {
            get { return m_properties.Count == 0; }
        }

        public IDictionary<string, string> Properties
        {
            get { return m_properties; }
        }

        public StyleData()
        {
            m_properties = new Dictionary<string, string>();
        }

        public StyleData(IDictionary<string, string> data)
        {
            m_properties = data;
        }

        public void LoadData(IDictionary<string, string> data)
        {
            foreach (KeyValuePair<string, string> pair in data)
            {
                string oldValue;

                if (m_properties.TryGetValue(pair.Key, out oldValue))
                {
                    Console.WriteLine("Style Warning: property {0}:{1} overrides old property {0}:{2}", pair.Key, pair.Value, oldValue);
                }

                m_properties[pair.Key] = pair.Value;
            }
        }
    }
}
