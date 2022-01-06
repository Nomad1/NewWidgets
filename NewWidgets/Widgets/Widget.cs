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
    /// Base class for abstract widgets, i.e. Image or Label
    /// </summary>
    public abstract class Widget : WindowObject
    {
        [Obsolete]
        public static readonly WidgetStyleSheet DefaultStyle = WidgetManager.GetStyle("default", true);

        public delegate bool TooltipDelegate(Widget sender, string text, Vector2 position);

        private bool m_needUpdateStyle;
        private bool m_needsLayout; // flag to indicate that inner label size/opacity/formatting has changed

        private WidgetStyleSheet m_style;
        private readonly StyleSheetData m_ownStyle;

        private WidgetState m_currentState;
        private string m_styleClass;
        private string m_id;

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
        /// Class name
        /// </summary>
        public string StyleClass
        {
            get { return m_styleClass; }
        }

        /// <summary>
        /// Element #id
        /// </summary>
        public string StyleId
        {
            get { return m_id; }
        }

        /// <summary>
        /// Pseudo-class name. TODO: get rid of strings
        /// </summary>
        public string StyleState
        {
            get
            {
                switch (m_currentState)
                {
                    case WidgetState.Disabled:
                        return ":disabled";
                    case WidgetState.Hovered:
                        return ":hover";
                    case WidgetState.Selected:
                        return ":active";
                    case WidgetState.SelectedDisabled:
                        return ":active:disabled";
                    case WidgetState.SelectedHovered:
                        return ":hover:active";
                    case WidgetState.SelectedDisabledHovered:
                        return ":hover:active:disabled";
                    case WidgetState.DisabledHovered:
                        return ":hover:disabled";
                    default:
                        return "";
                }
            }
        }

        /// <summary>
        /// Element type name. TODO: get rid of strings
        /// </summary>
        public virtual string StyleElementType
        {
            get { return "panel"; }
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
        public bool ClipContents
        {
            get { return GetProperty(WidgetParameterIndex.Clip, false); }
            set { SetProperty(WidgetParameterIndex.Clip, value); } // clipping is applied on each redraw so we don't need to call Invalidate of any kind
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

        public bool NeedsLayout
        {
            get { return m_needsLayout; }
        }

        public event TooltipDelegate OnTooltip;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.Widget"/> class.
        /// </summary>
        /// <param name="style">Style.</param>
        protected Widget(WidgetStyleSheet style = default(WidgetStyleSheet))
            : base(null)
        {
            m_ownStyle = new StyleSheetData();

            m_style = style;
            m_style.SetOwnStyle(m_ownStyle);

            m_styleClass = style.Name;

            if (!style.IsEmpty) // obsolete, needed in some very rare cases
                Size = style.Get(WidgetParameterIndex.Size, new Vector2(0, 0));

            m_currentState = WidgetState.Normal;

            m_needUpdateStyle = true;
            m_needsLayout = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.Widget"/> class for internal use
        /// </summary>
        /// <param name="styles">Styles.</param>
        protected Widget(string id, string style)
           : base(null)
        {
            m_styleClass = style;
            m_id = id;

            m_currentState = WidgetState.Normal;
            m_needUpdateStyle = true;
            m_needsLayout = true;
        }

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
        public void InvalidateStyle()
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
                styles.Insert(0, new StyleSelector(StyleElementType, StyleClass, StyleId, StyleState));
                current = current.Parent;

                combinators.Add(current == null ? StyleSelectorCombinator.None : StyleSelectorCombinator.Descendant);
            }
            while (current != null);

            m_style = WidgetManager.GetStyle(new StyleSelectorList(styles, combinators));

            m_style.SetOwnStyle(m_ownStyle);
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

            if (ClipContents)
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
            
            if (ClipContents)
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

