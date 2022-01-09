using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace NewWidgets.Utility
{
    /// <summary>
    /// Attribute for enum member name
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Enum, AllowMultiple = false)]
    internal class NameAttribute : Attribute
    {
        private readonly string m_name;

        public string Name
        {
            get { return m_name; }
        }

        public NameAttribute(string name)
        {
            m_name = name;
        }
    }

    /// <summary>
    /// Helper class to access string values by enum indices.
    /// This class is thread safe 
    /// </summary>
    /// <typeparam name="TIndex"></typeparam>
    internal class IndexNameMap<TIndex> where TIndex : struct, IConvertible  // Enum specification is only available in C# 7.3
    {
        #region static

        // This is for general use

        private static readonly Lazy<IndexNameMap<TIndex>> s_instance = new Lazy<IndexNameMap<TIndex>>();

        public static IndexNameMap<TIndex> Instance
        {
            get { return s_instance.Value; }
        }

        public static TIndex GetIndexByName(string stringIndex)
        {
            return Instance.DoGetIndexByName(stringIndex);
        }

        #endregion

        // implementation

        private int m_maximumIndex;

        private readonly IDictionary<string, TIndex> m_indexCache = new ConcurrentDictionary<string, TIndex>();

        
        /// <summary>
        /// This constructor checks the input type and also enu,erates all of Enum members
        /// to find the one with maximum index and also check NameAttribute on the each field
        /// for string-value mapping
        /// </summary>
        protected IndexNameMap()
        {
            Type type = typeof(TIndex);

            Debug.Assert(type.IsEnum && Enum.GetUnderlyingType(type) != typeof(int), "IndexedData<> supports only Enum members based on Int32");

            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            foreach (FieldInfo field in fields)
            {
                TIndex index = (TIndex)field.GetValue(null);

                foreach (NameAttribute attribute in field.GetCustomAttributes(typeof(NameAttribute), true))
                    m_indexCache[attribute.Name] = index;

                int iindex = index.ToInt32(null);

                if (m_maximumIndex < iindex)
                    m_maximumIndex = iindex;
            }
        }

        private TIndex DoGetIndexByName(string stringIndex)
        {
            TIndex result;
            if (m_indexCache.TryGetValue(stringIndex, out result))
                return result;

            // Guaranteed unique
            result = (TIndex)Enum.ToObject(typeof(TIndex), System.Threading.Interlocked.Increment(ref m_maximumIndex));

            m_indexCache[stringIndex] = result; // will be trasformed to AddOrUpdate

            return result;
        }

    }
}
