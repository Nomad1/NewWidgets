using System;
using System.Numerics;
using NewWidgets.UI;
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
        public static readonly WidgetStyleSheet DefaultStyle = WidgetManager.GetStyle("default", true);

        public delegate bool TooltipDelegate(Widget sender, string text, Vector2 position);

        internal protected readonly WidgetStyleSheet[] m_styles;

        private WidgetState m_currentState;
        private string m_styleClass;
        private string m_styleElementName;

        private float m_alpha = 1.0f; // the only property that could be changed for simple widget without affecting its stylesheet

        private string m_tooltip;

        #region Style-related stuff

        public WidgetState CurrentState
        {
            get { return m_currentState; }
        }

        public string StyleClass
        {
            get { return m_styleClass; }
        }

        public string StyleElementName
        {
            get { return m_styleElementName; }
        }

        public virtual string StyleClassType
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
                    base.Enabled = value;
                    DelayedUpdateStyle();
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
                    base.Selected = value;
                    DelayedUpdateStyle();
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
                    base.Hovered = value;
                    DelayedUpdateStyle();
                }
            }
        }
       
        public bool ClipContents
        {
            get { return GetProperty(WidgetParameterIndex.Clip, false); }
            set { SetProperty(WidgetParameterIndex.Clip, value); }
        }

        public Margin ClipMargin
        {
            get { return GetProperty(WidgetParameterIndex.ClipMargin, new Margin(0)); }
            set { SetProperty(WidgetParameterIndex.ClipMargin, value); }
        }

        public virtual float Opacity
        {
            get { return m_alpha; }
            set { m_alpha = value; }
        }

        public string Tooltip
        {
            get { return m_tooltip; }
            set { m_tooltip = value; }
        }

        public event TooltipDelegate OnTooltip;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.Widget"/> class.
        /// </summary>
        /// <param name="style">Style.</param>
        protected Widget(WidgetStyleSheet style = default(WidgetStyleSheet))
            : base(null)
        {

            if (style.IsEmpty)
                style = DefaultStyle;
            Size = style.Get(WidgetParameterIndex.Size, new Vector2(0, 0));

            m_currentState = WidgetState.Normal;

            m_styles = new WidgetStyleSheet[(int)WidgetState.Max];

            LoadStyle(WidgetState.Normal, style);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.Widget"/> class for internal use
        /// </summary>
        /// <param name="styles">Styles.</param>
        internal protected Widget(WidgetStyleSheet[] styles)
           : base(null)
        {
            Size = new Vector2(0, 0);
            m_currentState = WidgetState.Normal;
            m_styles = styles; // use the same styles as parent
        }

        #region Styles

        internal T GetProperty<T>(WidgetParameterIndex index, T defaultValue)
        {
            return m_styles[(int)m_currentState].Get(index, defaultValue);
        }

        internal T GetProperty<T>(WidgetState style, WidgetParameterIndex index, T defaultValue)
        {
            if (!HasStyle(style))
                style = m_currentState;

            return m_styles[(int)style].Get(index, defaultValue);
        }

        internal void SetProperty<T>(WidgetParameterIndex index, T value)
        {
            for (int i = 0; i < m_styles.Length; i++)
                if (!m_styles[i].IsEmpty)
                    m_styles[i].Set(m_styles, index, value);
        }

        internal void SetProperty<T>(WidgetState style, WidgetParameterIndex index, T value)
        {
            if (!HasStyle(style))
                throw new ArgumentException("Widget doesn't have style " + style + " assigned!");

            m_styles[(int)style].Set(m_styles, index, value);
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
            return m_styles[(int)m_currentState].Get(name, defaultValue);
        }

        /// <summary>
        /// Sets a named property for all assigned stylesheets
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetProperty(string name, string value)
        {
            for (int i = 0; i < m_styles.Length; i++)
                if (!m_styles[i].IsEmpty)
                    m_styles[i].Set(m_styles, name, value);
        }

        private bool HasStyle(WidgetState styleType)
        {
            return !m_styles[(int)styleType].IsEmpty;
        }

        private void DelayedSwitchStyle(WidgetState styleType)
        {
            AnimationManager.Instance.StartCustomAnimation(this, AnimationKind.Custom, null, 1, null, () => SwitchStyle(styleType));
        }

        private void DelayedUpdateStyle()
        {
            AnimationManager.Instance.StartCustomAnimation(this, AnimationKind.Custom, null, 1, null, UpdateStyle);
        }

        public void UpdateStyle()
        {
            WidgetState type = WidgetState.Normal;

            if (Selected)
                type |= WidgetState.Selected;

            if (!Enabled)
                type |= WidgetState.Disabled;

            if (Hovered)
                type |= WidgetState.Hovered;

            if (!HasStyle(type)) // try to fall back
            {
                if (HasStyle(type & ~WidgetState.Selected))
                    type &= ~WidgetState.Selected;

                if (HasStyle(type & ~WidgetState.Disabled))
                    type &= ~WidgetState.Disabled;

                if (HasStyle(type & ~WidgetState.Hovered))
                    type &= ~WidgetState.Hovered;
            }

            if (HasStyle(type)) // only perform switch if we have where to switch
            {
                if (type == m_currentState)
                    return;

                DelayedSwitchStyle(type);
            }
        }

        /// <summary>
        /// Switches the style.
        /// </summary>
        /// <param name="styleType">Style type.</param>
        public virtual bool SwitchStyle(WidgetState styleType)
        {
            if (!HasStyle(styleType))
                return false;

            if (m_currentState == styleType)
                return false;

            m_currentState = styleType;

            return true;
        }

        /// <summary>
        /// Loads the style and possible sub-styles
        /// </summary>
        /// <param name="styleType">Style type.</param>
        /// <param name="style">Style.</param>
        public void LoadStyle(WidgetState styleType, WidgetStyleSheet style)
        {
            if (style.IsEmpty)
                return;

            m_styles[(int)styleType] = style;

            // Hovered can be only subset of Normal, Disabled, Selected or SelectedDisabled
            if (styleType == WidgetState.Normal || styleType == WidgetState.Disabled || styleType == WidgetState.Selected || styleType == WidgetState.SelectedDisabled)
            {
                var hoveredStyleReference = style.Get(WidgetParameterIndex.HoveredStyle, default(WidgetStyleSheet));

                if (!hoveredStyleReference.IsEmpty)
                {
                    WidgetState targetStyleType = 0;
                    switch (styleType)
                    {
                        case WidgetState.Normal:
                            targetStyleType = WidgetState.Hovered;
                            break;
                        case WidgetState.Selected:
                            targetStyleType = WidgetState.SelectedHovered;
                            break;
                        case WidgetState.Disabled:
                            if (style.Name == m_styles[(int)WidgetState.Normal].Name)
                                targetStyleType = 0; // workaround to prevent using hovered style when disabled is set to same style as normal
                            else
                                targetStyleType = WidgetState.DisabledHovered;
                            break;
                        case WidgetState.SelectedDisabled:
                            targetStyleType = WidgetState.SelectedDisabledHovered;
                            break;
                    }

                    if (targetStyleType != 0)
                        LoadStyle(targetStyleType, hoveredStyleReference);

                }
            }

            // Disabled can be only subset of Normal or Selected
            if (styleType == WidgetState.Normal || styleType == WidgetState.Selected)
            {
                var disabledStyleReference = style.Get(WidgetParameterIndex.DisabledStyle, default(WidgetStyleSheet));

                if (!disabledStyleReference.IsEmpty)
                {
                    WidgetState targetStyleType = 0;
                    switch (styleType)
                    {
                        case WidgetState.Normal:
                            targetStyleType = WidgetState.Disabled;
                            break;
                        case WidgetState.Selected:
                            targetStyleType = WidgetState.SelectedDisabled;
                            break;
                    }

                    if (targetStyleType != 0)
                        LoadStyle(targetStyleType, disabledStyleReference);
                }
            }

            // Selected can be only subset of Normal
            if (styleType == WidgetState.Normal)
            {
                var selectedStyleReference = style.Get(WidgetParameterIndex.SelectedStyle, default(WidgetStyleSheet));

                if (!selectedStyleReference.IsEmpty)
                    LoadStyle(WidgetState.Selected, selectedStyleReference);

                var selectedHoveredStyleReference = style.Get(WidgetParameterIndex.SelectedHoveredStyle, default(WidgetStyleSheet));

                if (!selectedHoveredStyleReference.IsEmpty)
                    LoadStyle(WidgetState.SelectedHovered, selectedHoveredStyleReference);
            }
        }

        #endregion

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

        protected virtual void DrawContents()
        {
        }
        
        public void FadeTo(float alpha, int time, Action callback)
        {
            AnimationManager.Instance.StartAnimation(this, AnimationKind.Alpha, Opacity, alpha, time, (float x, float from, float to) => Opacity = MathHelper.LinearInterpolation(x, from, to), callback);
        }
    }
}

