using System;
using System.Collections.Generic;
using System.Numerics;
using NewWidgets.UI;
using NewWidgets.UI.Styles;
using NewWidgets.Utility;

#if RUNMOBILE
using RunMobile.Utility;
#endif

namespace NewWidgets.Widgets
{
    /// <summary>
    /// Simple tuple struct to hold id and class name of the Widget
    /// </summary>
    public struct WidgetStyle
    {
        public readonly string Id;
        public readonly string[] Classes;
        public readonly bool IsEmpty;

        /// <summary>
        /// Creates WidgetStyle with classes and id
        /// </summary>
        /// <param name="classes"></param>
        /// <param name="id"></param>
        public WidgetStyle(string[] classes, string id)
        {
            IsEmpty = false;
            Id = id;
            Classes = classes;
        }

        /// <summary>
        /// Creates WidgetStyle with id and no classes
        /// </summary>
        /// <param name="class"></param>
        public WidgetStyle(string id)
        {
            IsEmpty = false;
            Id = id;
            Classes = null;
        }
    }

    /// <summary>
    /// Base class for abstract widgets, i.e. Image or Label
    /// </summary>
    public abstract class Widget : WindowObject
    {
        public const string ElementType = "*";

        /// <summary>
        /// This is pretty much obsolete but was widely used in previous versions to specify a style
        /// without any decorations. Now we have to drag along .none class to maintain at least a partial compat
        /// </summary>
        public static readonly WidgetStyle DefaultStyle = new WidgetStyle(new string[] { "none" }, null);
        //

        public delegate bool TooltipDelegate(Widget sender, string text, Vector2 position);

        private bool m_needUpdateStyle;
        private bool m_needsLayout; // flag to indicate that inner label size/opacity/formatting has changed

        private WidgetStyleSheet m_style;
        private readonly StyleSheetData m_ownStyle;

        private readonly string m_elementType;
        private string m_id;
        private string[] m_styleClasses;
        private WidgetState m_currentState;

        private string m_tooltip;

        #region Style-related stuff

        /// <summary>
        /// Pseudo-class flag
        /// </summary>
        public WidgetState CurrentState
        {
            get { return m_currentState; }
            protected set
            {
                if (m_currentState != value)
                {
                    m_currentState = value;
                    InvalidateStyle();
                }
            }
        }

        /// <summary>
        /// Element type, i.e. button, label, checkbox
        /// </summary>
        public string StyleElementType
        {
            get { return m_elementType; }
        }

        /// <summary>
        /// Class name
        /// </summary>
        public string [] StyleClasses
        {
            get { return m_styleClasses; }
            set { m_styleClasses = value; InvalidateStyle(); }
        }

        /// <summary>
        /// Element #id
        /// </summary>
        public string StyleId
        {
            get { return m_id; }
            set { m_id = value; InvalidateStyle(); }
        }

        /// <summary>
        /// Pseudo-class name. TODO: get rid of strings
        /// </summary>
        public string [] StyleState
        {
            get
            {
                List<string> pseudoClasses = new List<string>(3);

                if ((m_currentState & WidgetState.Hovered) != 0)
                    pseudoClasses.Add(":hover");
                if ((m_currentState & WidgetState.Selected) != 0)
                    pseudoClasses.Add(":focus");
                if ((m_currentState & WidgetState.Disabled) != 0)
                    pseudoClasses.Add(":disabled");

                return pseudoClasses.ToArray();
            }
        }

        #endregion

        public override bool Enabled
        {
            get { return base.Enabled; }
            set
            {
                if (Enabled != value)
                {
                    if (!value)
                        CurrentState |= WidgetState.Disabled;
                    else
                        CurrentState &= ~WidgetState.Disabled;

                    base.Enabled = value;
                }
            }
        }

        public override bool Selected
        {
            get { return base.Selected; }
            set
            {
                if (Selected != value)
                {
                    if (value)
                        CurrentState |= WidgetState.Selected;
                    else
                        CurrentState &= ~WidgetState.Selected;

                    base.Selected = value;
                }
            }
        }

        public override bool Hovered
        {
            get { return base.Hovered; }
            set
            {
                if (Hovered != value)
                {
                    if (value)
                        CurrentState |= WidgetState.Hovered;
                    else
                        CurrentState &= ~WidgetState.Hovered;

                    base.Hovered = value;
                }
            }
        }

        /// <summary>
        /// Indicates if the contents should be clipped. Almost the same as overflow:hidden and overflow:visible in HTML
        /// </summary>
        public WidgetOverflow Overflow
        {
            get { return GetProperty(WidgetParameterIndex.Overflow, WidgetOverflow.Visible); }
            set { SetProperty(WidgetParameterIndex.Overflow, value); } // clipping is applied on each redraw so we don't need to call Invalidate of any kind
        }

        /// <summary>
        /// Wrapper for Overflow
        /// </summary>
        [Obsolete]
        public bool ClipContent
        {
            get { return Overflow == WidgetOverflow.Hidden; }
            set { Overflow = value ? WidgetOverflow.Hidden : WidgetOverflow.Visible; } // clipping is applied on each redraw so we don't need to call Invalidate of any kind
        }

        /// <summary>
        /// Wrapper for Overflow
        /// </summary>
        [Obsolete]
        public bool ClipContents
        {
            get { return Overflow == WidgetOverflow.Hidden; }
            set { Overflow = value ? WidgetOverflow.Hidden : WidgetOverflow.Visible; } // clipping is applied on each redraw so we don't need to call Invalidate of any kind
        }

        /// <summary>
        /// Margin for border clipping if ClipContents is on
        /// </summary>
        public Margin ClipMargin
        {
            get { return GetProperty(WidgetParameterIndex.ClipMargin, new Margin(0)); }
            set { SetProperty(WidgetParameterIndex.ClipMargin, value); } // clipping is applied on each redraw so we don't need to call Invalidate of any kind
        }

        /// <summary>
        /// Overall opacity of this Widget. TODO: think of the difference between content and background opacity
        /// </summary>
        public float Opacity
        {
            get { return GetProperty(WidgetParameterIndex.Opacity, 1.0f); }
            set { SetProperty(WidgetParameterIndex.Opacity, value); } // Opacity and color should be applied on each redraw - it's cheap and it is the best way to handle colors and transparency
        }

        /// <summary>
        /// Gets actual opacity value as multiplication of current and all parent values
        /// </summary>
        public float OpacityValue
        {
            get { return Parent == null ? Opacity : Opacity * Parent.Opacity; }
        }

        /// <summary>
        /// Gets tooltip string for this control
        /// </summary>
        public string Tooltip
        {
            get { return m_tooltip; }
            set { m_tooltip = value; }
        }

        /// <summary>
        /// Widget parent up in the control tree
        /// </summary>
        public new Widget Parent
        {
            get { return base.Parent as Widget; }
            set { base.Parent = value; }
        }

        /// <summary>
        /// Windows Object that is parent to this control. Nomad: I believe this is kind of shit, but we still need
        /// window based parents :(
        /// </summary>
        public WindowObject ParentObject
        {
            get { return base.Parent; }
        }

        public bool NeedsLayout
        {
            get { return m_needsLayout; }
        }

    
        public event TooltipDelegate OnTooltip;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.Widget"/> class.
        /// </summary>
        /// <param name="style">Style.</param>
        protected Widget(string elementType, WidgetStyle style = default(WidgetStyle))
            : base(null)
        {
            m_elementType = elementType;
            m_id = string.IsNullOrEmpty(style.Id) ? string.Empty : style.Id;
            m_styleClasses = style.Classes;

            m_currentState = WidgetState.Normal;

            // creating own style sheet
            m_ownStyle = new StyleSheetData();

            // and complex object containing only that sheet
            m_style = new WidgetStyleSheet(elementType + "_" + GetHashCode(), null);
            m_style.SetOwnStyle(m_ownStyle);

            //Size = m_style.Get(WidgetParameterIndex.Size, new Vector2(0, 0)); // obsolete, needed in some very rare cases

            m_needUpdateStyle = true;
            m_needsLayout = true;
        }

        ///// <summary>
        ///// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.Widget"/> class for internal use
        ///// </summary>
        ///// <param name="styles">Styles.</param>
        //protected Widget(string id, string style)
        //   : base(null)
        //{
        //    m_styleClass = style;
        //    m_id = id;

        //    m_currentState = WidgetState.Normal;
        //    m_needUpdateStyle = true;
        //    m_needsLayout = true;
        //}

        #region Styles

        internal T GetProperty<T>(WidgetParameterIndex index, T defaultValue)
        {
            return m_style.Get(index, defaultValue);
        }

        internal void SetProperty<T>(WidgetParameterIndex index, T value)
        {
            m_style.Set(index, value);
        }

        /// <summary>
        /// Retrieve stylesheet property value by name
        /// </summary>
        /// <typeparam name="T">Desired type</typeparam>
        /// <param name="name">property name</param>
        /// <param name="defaultValue">default value</param>
        /// <returns></returns>
        public T GetProperty<T>(string name, T defaultValue)
        {
            return m_style.Get(name, defaultValue);
        }

        /// <summary>
        /// Sets a named property for all assigned stylesheets
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetProperty(string name, string value)
        {
            m_style.Set(name, value);
        }

        /// <summary>
        /// This method should be called if the hierarchy or parent state are changed
        /// </summary>
        protected void InvalidateStyle()
        {
            m_needUpdateStyle = true;
        }

        /// <summary>
        /// This method should be called when widget layout is changed (size, padding, etc.)
        /// </summary>
        public void InvalidateLayout()
        {
            m_needsLayout = true;
        }

        /// <summary>
        /// Adds one class name to a style
        /// </summary>
        /// <param name="className"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void AddStyleClass(string className)
        {
            if (string.IsNullOrEmpty(className))
                throw new ArgumentNullException("@className should not be empty!");

            string[] array = new string[m_styleClasses.Length + 1];
            m_styleClasses.CopyTo(array, 0);
            array[m_styleClasses.Length] = className;
            m_styleClasses = array;
            InvalidateLayout();
        }

        /// <summary>
        /// Removes one class from the style
        /// </summary>
        /// <param name="className"></param>
        /// <returns>true if the class was removed</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool RemoveStyleClass(string className)
        {
            if (string.IsNullOrEmpty(className))
                throw new ArgumentNullException("@className should not be empty!");

            bool changed = false;

            for (int i = 0; i < m_styleClasses.Length; i++)
            {
                if (m_styleClasses[i] == className)
                {
                    m_styleClasses[i] = string.Empty;
                    changed = true;
                }
            }

            if (changed)
                InvalidateLayout();

            return changed;
        }

        /// <summary>
        /// This method is to be called when:
        /// 1. Widget size has changed (Resize was called)
        /// 2. Widget style has changed
        /// 3. Widget content has changed and widget should be resized
        /// </summary>
        public virtual void UpdateLayout()
        {
            m_needsLayout = false;
            // nothing to do in base
        }

        protected override void Resize(Vector2 size)
        {
            base.Resize(size);

            SetProperty(WidgetParameterIndex.Width, size.X);
            SetProperty(WidgetParameterIndex.Height, size.Y);

            InvalidateLayout();
        }

        public virtual void UpdateStyle()
        {
            m_needUpdateStyle = false;
            m_needsLayout = true; // make sure that any style changes result in layout updates as well

            List<StyleSelector> styles = new List<StyleSelector>();
            List<StyleSelectorCombinator> combinators = new List<StyleSelectorCombinator>();

            Widget current = this;

            do
            {
                styles.Insert(0, new StyleSelector(current.StyleElementType, current.StyleClasses, current.StyleId, current.StyleState));
                current = current.Parent;

                combinators.Add(current == null ? StyleSelectorCombinator.None : StyleSelectorCombinator.Descendant);
            }
            while (current != null);

            StyleSelectorList list = new StyleSelectorList(styles, combinators);

            m_style = WidgetManager.GetStyle(list);

            m_style.SetOwnStyle(m_ownStyle);

            //Console.WriteLine("Resolved style: {0} {{\n{1}\n}}", list, m_style);

            Vector2 size = Size;

            float width;

            if (m_style.TryGetValue(WidgetParameterIndex.Width, out width))
                size.X = width;

            float height;
            if (m_style.TryGetValue(WidgetParameterIndex.Height, out height))
                size.Y = height;

            Size = size;
        }

        #endregion

        public override bool Update()
        {
            if (m_needUpdateStyle)
                UpdateStyle();

            if (m_needsLayout)
                UpdateLayout();

            return base.Update();
        }

        public override void Draw()
        {
            base.Draw(); // does nothing 

            if (!Visible)
                return;

            if (Overflow == WidgetOverflow.Hidden)
            {
                Vector2 clipTopLeft = this.Transform.GetScreenPoint(new Vector2(ClipMargin.Left, ClipMargin.Top));
                Vector2 clipBottomRight = this.Transform.GetScreenPoint(new Vector2(this.Size.X - ClipMargin.Right, this.Size.Y - ClipMargin.Bottom));

                WindowController.Instance.SetClipRect(
                    (int)Math.Floor(clipTopLeft.X),
                    (int)Math.Floor(clipTopLeft.Y),
                    (int)Math.Ceiling(clipBottomRight.X - clipTopLeft.X),
                    (int)Math.Ceiling(clipBottomRight.Y - clipTopLeft.Y));
            }

            DrawContents();
            
            if (Overflow == WidgetOverflow.Hidden)
                WindowController.Instance.CancelClipRect();
        }

        public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            if ((!string.IsNullOrEmpty(m_tooltip) || OnTooltip != null) && ((pointer == 0 && !unpress && !press) || (press && WindowController.Instance.IsTouchScreen)))
                return WidgetManager.HandleTooltip(this, m_tooltip, new Vector2(x, y), OnTooltip);

            return base.Touch(x, y, press, unpress, pointer);
        }

        /// <summary>
        /// This method draws widget contents with clipping
        /// </summary>
        protected virtual void DrawContents()
        {
        }
        
        public void FadeTo(float alpha, int time, Action callback)
        {
            AnimationManager.Instance.StartAnimation(this, AnimationKind.Alpha, Opacity, alpha, time, (float x, float from, float to) => Opacity = MathHelper.LinearInterpolation(x, from, to), callback);
        }
    }
}

