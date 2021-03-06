﻿using System;
using System.Numerics;
using NewWidgets.UI;
using NewWidgets.Utility;

#if RUNMOBILE
using RunMobile.Utility;
#endif

namespace NewWidgets.Widgets
{
    public class Widget : WindowObject
    {
        public static readonly WidgetStyleSheet DefaultStyle = WidgetManager.GetStyle("default", true);

        public delegate bool TooltipDelegate(Widget sender, string text, Vector2 position);

        internal protected readonly WidgetStyleSheet[] m_styles;
        private WidgetStyleType m_styleType;

        private float m_alpha = 1.0f; // the only property that could be changed for simple widget without affecting its stylesheet

        private string m_tooltip;

        #region Style-related stuff

        public WidgetStyleType StyleType
        {
            get { return m_styleType; }
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

        public virtual float Alpha
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

            m_styleType = WidgetStyleType.Normal;

            m_styles = new WidgetStyleSheet[(int)WidgetStyleType.Max];

            LoadStyle(WidgetStyleType.Normal, style);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.Widget"/> class for internal use
        /// </summary>
        /// <param name="styles">Styles.</param>
        internal protected Widget(WidgetStyleSheet[] styles)
           : base(null)
        {
            Size = new Vector2(0, 0);
            m_styleType = WidgetStyleType.Normal;
            m_styles = styles; // use the same styles as parent
        }

        #region Styles

        internal T GetProperty<T>(WidgetParameterIndex index, T defaultValue)
        {
            return m_styles[(int)m_styleType].Get(index, defaultValue);
        }

        internal T GetProperty<T>(WidgetStyleType style, WidgetParameterIndex index, T defaultValue)
        {
            if (!HasStyle(style))
                style = m_styleType;

            return m_styles[(int)style].Get(index, defaultValue);
        }

        internal void SetProperty<T>(WidgetParameterIndex index, T value)
        {
            for (int i = 0; i < m_styles.Length; i++)
                if (!m_styles[i].IsEmpty)
                    m_styles[i].Set(m_styles, index, value);
        }

        internal void SetProperty<T>(WidgetStyleType style, WidgetParameterIndex index, T value)
        {
            if (!HasStyle(style))
                throw new ArgumentException("Widget doesn't have style " + style + " assigned!");

            m_styles[(int)style].Set(m_styles, index, value);
        }

        public T GetProperty<T>(string name, T defaultValue)
        {
            return m_styles[(int)m_styleType].Get(name, defaultValue);
        }

        public void SetProperty(string name, string value)
        {
            for (int i = 0; i < m_styles.Length; i++)
                if (!m_styles[i].IsEmpty)
                    m_styles[i].Set(m_styles, name, value);
        }

        protected bool HasStyle(WidgetStyleType styleType)
        {
            return !m_styles[(int)styleType].IsEmpty;
        }

        protected void DelayedSwitchStyle(WidgetStyleType styleType)
        {
            Animator.StartCustomAnimation(this, AnimationKind.Custom, null, 1, null,
                delegate {
                    SwitchStyle(styleType);
                });
        }

        protected void DelayedUpdateStyle()
        {
            Animator.StartCustomAnimation(this, AnimationKind.Custom, null, 1, null, UpdateStyle);
        }

        public void UpdateStyle()
        {
            WidgetStyleType type = WidgetStyleType.Normal;

            if (Selected)
                type |= WidgetStyleType.Selected;

            if (!Enabled)
                type |= WidgetStyleType.Disabled;

            if (Hovered)
                type |= WidgetStyleType.Hovered;

            if (!HasStyle(type)) // try to fall back
            {
                if (HasStyle(type & ~WidgetStyleType.Selected))
                    type &= ~WidgetStyleType.Selected;

                if (HasStyle(type & ~WidgetStyleType.Disabled))
                    type &= ~WidgetStyleType.Disabled;

                if (HasStyle(type & ~WidgetStyleType.Hovered))
                    type &= ~WidgetStyleType.Hovered;
            }

            if (HasStyle(type)) // only perform switch if we have where to switch
            {
                if (type == m_styleType)
                    return;

                DelayedSwitchStyle(type);
            }
        }

        /// <summary>
        /// Switches the style.
        /// </summary>
        /// <param name="styleType">Style type.</param>
        public virtual bool SwitchStyle(WidgetStyleType styleType)
        {
            if (!HasStyle(styleType))
                return false;

            if (m_styleType == styleType)
                return false;

            m_styleType = styleType;

            return true;
        }

        /// <summary>
        /// Loads the style and possible sub-styles
        /// </summary>
        /// <param name="styleType">Style type.</param>
        /// <param name="style">Style.</param>
        public void LoadStyle(WidgetStyleType styleType, WidgetStyleSheet style)
        {
            if (style.IsEmpty)
                return;

            m_styles[(int)styleType] = style;

            // Hovered can be only subset of Normal, Disabled, Selected or SelectedDisabled
            if (styleType == WidgetStyleType.Normal || styleType == WidgetStyleType.Disabled || styleType == WidgetStyleType.Selected || styleType == WidgetStyleType.SelectedDisabled)
            {
                var hoveredStyleReference = style.Get(WidgetParameterIndex.HoveredStyle, default(WidgetStyleSheet));

                if (!hoveredStyleReference.IsEmpty)
                {
                    WidgetStyleType targetStyleType = 0;
                    switch (styleType)
                    {
                        case WidgetStyleType.Normal:
                            targetStyleType = WidgetStyleType.Hovered;
                            break;
                        case WidgetStyleType.Selected:
                            targetStyleType = WidgetStyleType.SelectedHovered;
                            break;
                        case WidgetStyleType.Disabled:
                            if (style.Name == m_styles[(int)WidgetStyleType.Normal].Name)
                                targetStyleType = 0; // workaround to prevent using hovered style when disabled is set to same style as normal
                            else
                                targetStyleType = WidgetStyleType.DisabledHovered;
                            break;
                        case WidgetStyleType.SelectedDisabled:
                            targetStyleType = WidgetStyleType.SelectedDisabledHovered;
                            break;
                    }

                    if (targetStyleType != 0)
                        LoadStyle(targetStyleType, hoveredStyleReference);

                }
            }

            // Disabled can be only subset of Normal or Selected
            if (styleType == WidgetStyleType.Normal || styleType == WidgetStyleType.Selected)
            {
                var disabledStyleReference = style.Get(WidgetParameterIndex.DisabledStyle, default(WidgetStyleSheet));

                if (!disabledStyleReference.IsEmpty)
                {
                    WidgetStyleType targetStyleType = 0;
                    switch (styleType)
                    {
                        case WidgetStyleType.Normal:
                            targetStyleType = WidgetStyleType.Disabled;
                            break;
                        case WidgetStyleType.Selected:
                            targetStyleType = WidgetStyleType.SelectedDisabled;
                            break;
                    }

                    if (targetStyleType != 0)
                        LoadStyle(targetStyleType, disabledStyleReference);
                }
            }

            // Selected can be only subset of Normal
            if (styleType == WidgetStyleType.Normal)
            {
                var selectedStyleReference = style.Get(WidgetParameterIndex.SelectedStyle, default(WidgetStyleSheet));

                if (!selectedStyleReference.IsEmpty)
                    LoadStyle(WidgetStyleType.Selected, selectedStyleReference);

                var selectedHoveredStyleReference = style.Get(WidgetParameterIndex.SelectedHoveredStyle, default(WidgetStyleSheet));

                if (!selectedHoveredStyleReference.IsEmpty)
                    LoadStyle(WidgetStyleType.SelectedHovered, selectedHoveredStyleReference);
            }
        }

        #endregion

        public override void Draw(object canvas)
        {
            base.Draw(canvas); // does nothing 

            if (!Visible)
                return;

            if (ClipContents)
            {
                Vector2 clipTopLeft = this.Transform.GetScreenPoint(new Vector2(ClipMargin.Left, ClipMargin.Top));
                Vector2 clipBottomRight = this.Transform.GetScreenPoint(new Vector2(this.Size.X - ClipMargin.Right, this.Size.Y - ClipMargin.Bottom));

                WindowController.Instance.SetClipRect(
                    (int)clipTopLeft.X,
                    (int)clipTopLeft.Y,
                    (int)(clipBottomRight.X - clipTopLeft.X + 0.5f),
                    (int)(clipBottomRight.Y - clipTopLeft.Y + 0.5f));
            }

            DrawContents(canvas);
            
            if (ClipContents)
                WindowController.Instance.CancelClipRect();
        }

        public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            if ((!string.IsNullOrEmpty(m_tooltip) || OnTooltip != null) && ((pointer == 0 && !unpress && !press) || (press && WindowController.Instance.IsTouchScreen)))
                return WidgetManager.HandleTooltip(this, m_tooltip, new Vector2(x, y), OnTooltip);

            return base.Touch(x, y, press, unpress, pointer);
        }

        protected virtual void DrawContents(object canvas)
        {
        }
        
        public void FadeTo(float alpha, int time, Action callback)
        {
            Animator.StartAnimation(this, AnimationKind.Alpha, Alpha, alpha, time, (float x, float from, float to) => Alpha = MathHelper.LinearInterpolation(x, from, to), callback);
        }
    }
}

