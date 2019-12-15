using System.Collections.Generic;

namespace NewWidgets.UI
{
    /// <summary>
    /// Simplified window container without focusing, Z-order, touch events, etc.
    /// Use it only for draw lists
    /// </summary>
    public class SimpleWindow : WindowObject, IWindowContainer
    {
        private readonly List<WindowObject> m_children;

        public ICollection<WindowObject> Children
        {
            get { return m_children; }
        }

        public int MaximumZIndex
        {
            get { return 0; }
        }

        public SimpleWindow()
            : base(null)
        {
            m_children = new List<WindowObject>();
        }

        public void ClearChildren()
        {
            m_children.Clear();
        }

        public override bool Update()
        {
            if (!base.Update())
                return false;

            bool hasChanges = false;

            for (int i = 0; i < m_children.Count; i++)
            {
                WindowObject child = m_children[i];
                if (!child.Update())
                {
                    m_children.RemoveAt(i);
                    hasChanges = true;
                }
                else
                {
                    if (child.HasChanges)
                        hasChanges = true;
                }
            }

            HasChanges = hasChanges;

            return true;
        }

        public override void Draw(object canvas)
        {
            base.Draw(canvas);

            if (Visible)
                foreach (WindowObject child in m_children)
                    child.Draw(canvas);
        }
      
        public void AddChild(WindowObject child)
        {
            var parentContainer = child.Parent as IWindowContainer;
            if (parentContainer != null && parentContainer != this)
                parentContainer.RemoveChild(child);

            m_children.Add(child);
            child.Parent = this;
        }

        public bool RemoveChild(WindowObject child)
        {
            if (child.Parent != this)
                return false;

            m_children.Remove(child);

            return true;
        }
    }
}
