using System;
using System.Collections;
using System.Collections.Generic;

namespace StyleTree
{
    /// <summary>
    /// ReadOnly collection that uses specified range of provided IList class and supports enumeration and indexed access to a range
    /// without copying the elements
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ListRange<T> : IEnumerable<T>, IList<T>
    {
        private readonly IList<T> m_list;
        private readonly int m_rangeStart;
        private readonly int m_rangeLength;

        public int Count
        {
            get { return m_rangeLength; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public T this[int index]
        {
            get { return m_list[index + m_rangeStart]; }
            set { throw new NotSupportedException(); }
        }

        public ListRange(IList<T> list, int rangeStart, int rangeLength)
        {
            m_list = list;

            if (rangeStart < 0 || rangeStart + rangeLength >= m_list.Count)
                new ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection");

            m_rangeStart = rangeStart;
            m_rangeLength = rangeLength;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < m_rangeLength; i++)
            {
                yield return m_list[i + m_rangeStart];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int IndexOf(T item)
        {
            int index = m_list.IndexOf(item);

            if (index < m_rangeStart)
                return -1;
            if (index >= m_rangeStart + m_rangeLength)
                return -1;

            return index - m_rangeStart;
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i < m_rangeLength; i++)
                array[arrayIndex + i] = m_list[i + m_rangeStart];
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }
    }
}
