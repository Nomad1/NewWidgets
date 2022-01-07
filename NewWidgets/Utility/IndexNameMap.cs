using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    internal static class IndexNameMap<TIndex> where TIndex : struct, IConvertible  // Enum specification is only available in C# 7.3
    {
        private static int s_maximumIndex;

        private static readonly IDictionary<string, TIndex> s_indexCache = new ConcurrentDictionary<string, TIndex>();
        private static readonly IDictionary<TIndex, NameAttribute[]> s_attributeCache = new ConcurrentDictionary<TIndex, NameAttribute[]>();

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
            List<NameAttribute> attributes = new List<NameAttribute>();

            foreach (FieldInfo field in fields)
            {
                TIndex index = (TIndex)field.GetValue(null);

                attributes.Clear();

                foreach (NameAttribute attribute in field.GetCustomAttributes(typeof(NameAttribute), true))
                {
                    s_indexCache[attribute.Name] = index;
                    attributes.Add(attribute);
                }

                if (attributes.Count > 0)
                    s_attributeCache[index] = attributes.ToArray();

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

        /// <summary>
        /// Returns first attribute of specified TAttribute type. Note that if there are many attributes they will be skipped
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="index"></param>
        /// <returns></returns>
        public static TAttribute GetAttributeByIndex<TAttribute>(TIndex index) where TAttribute : NameAttribute
        {
            NameAttribute[] result;

            if (s_attributeCache.TryGetValue(index, out result))
                foreach(NameAttribute attribute in result)
                    if (attribute is TAttribute)
                        return (TAttribute)attribute;

            return default(TAttribute);
        }

        /// <summary>
        /// Returns first name-attribute value corresponding to TAttribute type
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static string GetNameByIndex<TAttribute>(TIndex index) where TAttribute : NameAttribute
        {
            NameAttribute[] result;

            if (s_attributeCache.TryGetValue(index, out result))
                foreach (NameAttribute attribute in result)
                    if (attribute is TAttribute)
                        return attribute.Name;

            return index.ToString();
        }
    }
}
