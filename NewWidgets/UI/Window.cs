using System;
using System.Collections.Generic;
using System.Numerics;

namespace NewWidgets.UI
{
    public interface IWindowContainer
    {
        ICollection<WindowObject> Children { get; }
        int MaximumZIndex { get; }
        void AddChild(WindowObject child);
        bool RemoveChild(WindowObject child);
    }

    public class Window : WindowObject, IWindowContainer
    {
        private readonly WindowObjectArray<WindowObject> m_children;

        private WindowFlags m_flags;

        public WindowFlags Flags
        {
            get { return m_flags; }
            protected set { m_flags = value; }
        }

        public bool Modal
        {
            get { return (m_flags & WindowFlags.FullScreen) == 0; }
        }

        public bool Controlling
        {
            get { return (m_flags & WindowFlags.Controlling) != 0; }
            set { m_flags = value ? m_flags | WindowFlags.Controlling : m_flags & ~WindowFlags.Controlling; }
        }

        public bool IsFocusable
        {
            get { return (m_flags & WindowFlags.Focusable) != 0; }
            set { m_flags = value ? m_flags | WindowFlags.Focusable : m_flags & ~WindowFlags.Focusable; }
        }

        public bool IsFocused
        {
            get { return (m_flags & WindowFlags.Focused) != 0; }
            protected set { m_flags = value ? m_flags | WindowFlags.Focused : m_flags & ~WindowFlags.Focused; }
        }

        public ICollection<WindowObject> Children
        {
            get { return m_children.List; }
        }

        public int MaximumZIndex
        {
            get { return m_children.MaximumZIndex; }
        }

        public Window(WindowFlags flags)
            : base(null)
        {
            m_flags = flags | WindowFlags.Controlling;

            if ((m_flags & WindowFlags.FullScreen) != 0) // auto scale, TODO: separate flag or even remove this
            {
                Position = Vector2.Zero;
                Scale = Size.X == 0 ? 1.0f : WindowController.Instance.ScreenWidth / Size.X;
            }

            m_children = new WindowObjectArray<WindowObject>();
        }

        public override bool Update()
        {
            if (!base.Update())
                return false;

            HasChanges = m_children.Update();

            return true;
        }

        public override void Draw()
        {
            base.Draw();

            if (Visible)
                m_children.Draw();
        }

        public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            bool processed = base.Touch(x, y, press, unpress, pointer);

            if (processed)
                return true;

            if (!Controlling)
                return true;

            if (m_children.Touch(x, y, press, unpress, pointer))
                return true;

            return false;
        }

        public override bool Zoom(float x, float y, float value)
        {
            //While it's not required for Widget descendants, all Window descendants should use
            //the following lines:

            //bool processed = base.Zoom(x, y, value);

            //if (processed)
            //    return true;

            if (!Controlling)
                return true;

            if (m_children.Zoom(x, y, value))
                return true;

            return false;
        }

        public override bool Key(SpecialKey key, bool up, string keyString)
        {
            if (!Controlling)
                return true;

            if (m_children.Key(key, up, keyString))
                return true;

            if (Modal && key == SpecialKey.Back && up)
            {
                Remove();
                return true;
            }

            if (IsFocusable && up)
            {
                if (key == SpecialKey.Left)
                {
                    return FocusNext(false);
                }
                else if (key == SpecialKey.Right)
                {
                    return FocusNext(true);
                }
                else if ((key == SpecialKey.Select || key == SpecialKey.Enter || key == SpecialKey.Enter || key == SpecialKey.Joystick_Start))
                {
                    foreach (WindowObject child in m_children.List)
                        if (child is IFocusable && ((IFocusable)child).IsFocusable && ((IFocusable)child).IsFocused)
                        {
                            ((IFocusable)child).Press();
                            return true;
                        }
                }
            }


            return false;
        }

        public bool FocusNext(bool next)
        {
            LinkedListNode<IFocusable> current = null;
            LinkedList<IFocusable> focusable = new LinkedList<IFocusable>();

            IList<WindowObject> children = m_children.List;

            IFocusable focusedChild = null;

            for (int i = 0; i < children.Count; i++)
                if (children[i] is IFocusable && ((IFocusable)children[i]).IsFocusable)
                {
                    focusable.AddLast((IFocusable)children[i]);
                    if (((IFocusable)children[i]).IsFocused)
                    {
                        current = focusable.Last;
                        focusedChild = (IFocusable)children[i];
                    }
                }

            if (current == null)
            {
                current = next ? focusable.First : focusable.Last;
            }
            else
            {
                if (next)
                {
                    current = current.Next;
                    if (current == null)
                        current = focusable.First;
                }
                else
                {
                    current = current.Previous;
                    if (current == null)
                        current = focusable.Last;
                }
            }

            if (focusedChild != null)
            {
                focusedChild.SetFocused(false);
                focusedChild = null;
            }

            if (current != null)
            {
                focusedChild = current.Value;
                focusedChild.SetFocused(true);
                return true;
            }
            return false;
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

        public static bool FindChildren(IWindowContainer container, Func<WindowObject, bool> checker, IList<WindowObject> result, bool one = false)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            foreach (WindowObject obj in container.Children)
            {
                if (!obj.Visible)
                    continue;

                if (checker(obj))
                {
                    result.Add(obj);
                    if (one)
                        return true;
                }

                if (obj is IWindowContainer)
                {
                    if (FindChildren((IWindowContainer)obj, checker, result, one))
                        return true;
                }
            }

            return false;
        }

        public WindowObject FindChild(Func<WindowObject, bool> checker)
        {
            WindowObject[] result = new WindowObject[1];
            if (FindChildren(this, checker, result, false))
                return result[0];
            return null;
        }

        public void FindChildren(Func<WindowObject, bool> checker, IList<WindowObject> result)
        {
            FindChildren(this, checker, result, false);
        }

        public void ClearChildren()
        {
            m_children.Clear();
        }
    }
}

