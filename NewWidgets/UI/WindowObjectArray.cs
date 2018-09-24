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
    public class WindowObjectArray
    {
        private WindowObject[] m_array;
        private SortedLinkedList<int, WindowObject> m_list;
        private int m_version;
        private int m_arrayVersion;

        public int Count
        {
            get
            {
                return EnsureArray().Length;
            }
        }

        public WindowObject this[int index]
        {
            get
            {
                return EnsureArray()[index];
            }
        }

        public WindowObject [] List
        {
            get
            {
                return EnsureArray();
            }
        }

        public WindowObjectArray()
        {
            m_list = new SortedLinkedList<int, WindowObject>();
            m_array = new WindowObject[0];
        }

        private WindowObject[] EnsureArray()
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
            WindowObject[] array = EnsureArray();

            for (int i = 0; i < array.Length; i++)
                if (array[i] != null)
					array[i].LastList = null;

            m_list.Clear();
            m_array = new WindowObject[0];
            m_arrayVersion = ++m_version;
        }

        public void Remove(WindowObject obj)
        {
            if (obj.LastList == this)
            {
                obj.LastList = null;
                m_list.Remove(obj);

                m_version++;
            }
        }

        public void Add(WindowObject obj)
        {
            if (obj.LastList == this && Array.IndexOf(EnsureArray(), obj) != -1)
			{
				obj.TempZIndex = m_list.Last.Value.Key + 1;
				return;
			}
            else
				obj.TempZIndex = 0;

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
    				WindowObject obj = node.Value.Value;

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

            return hasChanges;
        }

        public void Draw(object canvas)
        {
            WindowObject[] array = EnsureArray();

            for (int i = 0; i < array.Length; i++)
                if (array[i] != null/*&& array[i].Visible*/)
                    array[i].Draw(canvas);
        }

        public bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            WindowObject[] array = EnsureArray();

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
            WindowObject[] array = EnsureArray();

            for (int i = array.Length - 1; i >= 0; i--)
                if (array[i] != null && array[i].Visible && array[i].HitTest(x, y))
                {
                    if (array[i].Zoom(x, y, value))
                        return true;
                }

            return false;
        }

        public bool Key(SpecialKey key, bool up, char character)
        {
            WindowObject[] array = EnsureArray();

            for (int i = array.Length - 1; i >= 0; i--)
                if (array[i] != null && array[i].Visible)
                {
                    if (array[i].Key(key, up, character))
                        return true;
                }

            return false;
        }
    }
}

