﻿using System;
using System.Collections.Generic;
using NewWidgets.UI;

namespace NewWidgets.Widgets
{
    public class WidgetPanel : WidgetBackground, IWindowContainer
    {
        public static readonly new WidgetStyleSheet DefaultStyle = WidgetManager.GetStyle("default_panel", true);

        private readonly WindowObjectArray<Widget> m_children;

        public IList<Widget> Children
        {
            get { return m_children.List; }
        }

        ICollection<WindowObject> IWindowContainer.Children
        {
            get { return m_children.List; }
        }

        public int MaximumZIndex
        {
            get { return m_children.MaximumZIndex; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetPanel"/> class.
        /// </summary>
        /// <param name="style">Style.</param>
        public WidgetPanel(WidgetStyleSheet style = default(WidgetStyleSheet))
            : base(style.IsEmpty ? DefaultStyle : style)
        {
            m_children = new WindowObjectArray<Widget>();
        }

        public override bool Update()
        {
            if (!base.Update())
                return false;

            m_children.Update();

            return true;
        }

        protected override void DrawContents(object canvas)
        {
            base.DrawContents(canvas);

            m_children.Draw(canvas);
        }

        public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            bool processed = base.Touch(x, y, press, unpress, pointer);

            if (processed)
                return true;

            if (!Enabled)
                return true;

            if (m_children.Touch(x, y, press, unpress, pointer))
                return true;

            if (m_background.Touch(x, y, press, unpress, pointer)) // make sure that click inside panel is not transparent
                return true;

            return false;
        }

        public override bool Zoom(float x, float y, float value)
        {
            //While it's not required for Widget descendants, all WidgetPanel descendants should use
            //the following lines:

            //bool processed = base.Zoom(x, y, value);

            //if (processed)
            //    return true;

            if (!Enabled)
                return true;

            if (m_children.Zoom(x, y, value))
                return true;

            if (m_background.Zoom(x, y, value))
                return true;

            return false;
        }

        public override bool Key(SpecialKey key, bool up, string keyString)
        {
            if (!Enabled)
                return true;

            if (m_children.Key(key, up, keyString))
                return true;

            if (m_background.Key(key, up, keyString))
                return true;

            return false;
        }

        public void AddChild(Widget child)
        {
            var parentContainer = child.Parent as IWindowContainer;
            if (parentContainer != null && parentContainer != this)
                parentContainer.RemoveChild(child);

            m_children.Add(child);
            child.Parent = this;
        }

        public bool RemoveChild(WindowObject child)
        {
            var childWidget = child as Widget;
            if (child.Parent != this)
                return false;

            if (childWidget == null)
                throw new ArgumentException(nameof(child));

            m_children.Remove(childWidget);

            return true;
        }

        void IWindowContainer.AddChild(WindowObject child)
        {
            if (child is Widget)
                AddChild((Widget)child);
            else
                throw new ArgumentException(nameof(child));
        }

        public virtual void Clear()
        {
            foreach (Widget obj in m_children.List)
                obj.Remove();

            m_children.Clear();
        }

        public override void Remove()
        {
            Clear();

            base.Remove();
        }
    }
}

