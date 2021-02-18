using System;

#if RUNMOBILE
using RunMobile.Utility;
#else
using NewWidgets.Utility;
#endif

namespace NewWidgets.UI
{
    /// <summary>
    /// Helper collection class for WindowObjects. Objects are stored in sorted list and also in array that 
    /// is rebuilt on every change. Array operations are much faster and mostly thread-safe and tamper-resistant.
    /// However, the class itself is not 100% thread safe and does not have any locks
    /// </summary>
    public class WindowObjectArray<T> where T : WindowObject
    {
        private readonly SortedLinkedList<int, T> m_list;
        private T[] m_array;

        private int m_version;
        private int m_arrayVersion;

        private int m_maximumZIndex;

        public int Count
        {
            get { return EnsureArray().Length; }
        }

        public T this[int index]
        {
            get { return EnsureArray()[index]; }
        }

        public T[] List
        {
            get { return EnsureArray(); }
        }

        public int MaximumZIndex
        {
            get { return m_maximumZIndex; }
        }

        public WindowObjectArray()
        {
            m_list = new SortedLinkedList<int, T>();
            m_array = new T[0];
        }

        private T[] EnsureArray()
        {
            if (m_version != m_arrayVersion)
            {
                m_array = m_list.ToArray();
                m_arrayVersion = m_version;
            }
            return m_array;
        }

        public void Clear()
        {
            T[] array = EnsureArray();

            for (int i = 0; i < array.Length; i++)
                if (array[i] != null)
                    array[i].LastList = null;

            m_list.Clear();
            m_array = new T[0];
            m_arrayVersion = ++m_version;
        }

        public void Remove(T obj)
        {
            if (obj.LastList == this)
            {
                obj.LastList = null;
                m_list.Remove(obj);

                m_version++;
            }
        }

        public void Add(T obj)
        {
            int tempIndex = m_list.Count == 0 ? 0 : (m_list.Last.Value.Key + 1);
            obj.TempZIndex = tempIndex;

            if (obj.LastList == this && Array.IndexOf(EnsureArray(), obj) != -1) // if we're adding a child second time, it will be raised to the top of visibility. TODO: remove this
            {
                // already added, nothing changes
                return;
            }

            m_list.Add(obj.ZIndex, obj);
            obj.LastList = this;

            m_version++;
        }

        public bool Update()
        {
            bool hasChanges = false;

            // Re-sort list. Magic is here: we're modifying collection during enumeration
            var node = m_list.First;

            if (node != null)
            {
                do
                {
                    var nextNode = node.Next;
                    T obj = node.Value.Value;

                    if (!obj.Update())
                    {
                        m_list.Remove(node);
                        hasChanges = true;
                        m_version++;
                    }
                    else
                    {
                        if (obj.HasChanges)
                            hasChanges = true;

                        if (obj.ZIndex != node.Value.Key)
                        {
                            m_list.Remove(node);
                            m_list.Add(obj.ZIndex, obj);
                            m_version++;
                        }
                    }
                    node = nextNode;
                }
                while (node != null);
            }

            m_maximumZIndex = m_list.Last == null ? 0 : m_list.Last.Value.Value.ZIndex;

            return hasChanges;
        }

        public void Draw()
        {
            T[] array = EnsureArray();

            for (int i = 0; i < array.Length; i++)
                if (array[i] != null/*&& array[i].Visible*/)
                    array[i].Draw();
        }

        public bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            T[] array = EnsureArray();

            for (int i = array.Length - 1; i >= 0; i--)
                if (array[i] != null && array[i].Visible && array[i].HitTest(x, y))
                {
                    if (array[i].Touch(x, y, press, unpress, pointer))
                        return true;
                }

            return false;
        }

        public bool Zoom(float x, float y, float value)
        {
            T[] array = EnsureArray();

            for (int i = array.Length - 1; i >= 0; i--)
                if (array[i] != null && array[i].Visible && array[i].HitTest(x, y))
                {
                    if (array[i].Zoom(x, y, value))
                        return true;
                }

            return false;
        }

        public bool Key(SpecialKey key, bool up, string keyString)
        {
            T[] array = EnsureArray();

            for (int i = array.Length - 1; i >= 0; i--)
                if (array[i] != null && array[i].Visible)
                {
                    if (array[i].Key(key, up, keyString))
                        return true;
                }

            return false;
        }
    }
}

