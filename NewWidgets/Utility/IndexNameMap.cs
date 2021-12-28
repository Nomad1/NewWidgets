using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace NewWidgets.Utility
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
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
    internal static class IndexNameMap<TIndex> where TIndex : struct, IConvertible  // Enum specification is only available in C# 7.3
    {
        private static int s_maximumIndex;

        private static readonly IDictionary<string, TIndex> s_indexCache = new ConcurrentDictionary<string, TIndex>();

        /// <summary>
        /// This constructor checks the input type and also enu,erates all of Enum members
        /// to find the one with maximum index and also check NameAttribute on the each field
        /// for string-value mapping
        /// </summary>
        static IndexNameMap()
        {
            Type type = typeof(TIndex);

            if (!type.IsEnum || Enum.GetUnderlyingType(type) != typeof(int))
                throw new ArgumentException("IndexedData<> supports only Enum members based on Int32");

            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            foreach (FieldInfo field in fields)
            {
                TIndex index = (TIndex)field.GetValue(null);

                foreach (NameAttribute attribute in field.GetCustomAttributes(typeof(NameAttribute), true))
                    s_indexCache[attribute.Name] = index;

                int iindex = index.ToInt32(null);

                if (s_maximumIndex < iindex)
                    s_maximumIndex = iindex;
            }
        }

        public static TIndex GetIndexByName(string stringIndex)
        {
            TIndex result;
            if (s_indexCache.TryGetValue(stringIndex, out result))
                return result;

            // Guaranteed unique
            result = (TIndex)Enum.ToObject(typeof(TIndex), System.Threading.Interlocked.Increment(ref s_maximumIndex));

            s_indexCache[stringIndex] = result; // will be trasformed to AddOrUpdate

            return result;
        }
    }
}
