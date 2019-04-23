using System;
using System.Collections.Generic;
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

        private readonly Dictionary<WidgetStyleType, WidgetStyleSheet> m_styles;
        protected WidgetStyleSheet m_style;
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
                base.Enabled = value;
                DelayedUpdateStyle();
            }
        }

        public override bool Selected
        {
            get { return base.Selected; }
            set
            {
                base.Selected = value;
                DelayedUpdateStyle();
            }
        }

        public override bool Hovered
        {
            get { return base.Hovered; }
            set
            {
                base.Hovered = value;
                DelayedUpdateStyle();
            }
        }
       
        public bool ClipContents
        {
            get { return m_style.Get(WidgetParameterIndex.Clip, false); }
            set { m_style.Set(this, WidgetParameterIndex.Clip, value); }
        }

        public Margin ClipMargin
        {
            get { return m_style.Get(WidgetParameterIndex.ClipMargin, new Margin(0)); }
            set { m_style.Set(this, WidgetParameterIndex.ClipMargin, value); }
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

        protected Widget(WidgetStyleSheet style = default(WidgetStyleSheet))
            : base(null)
        {
            if (style.IsEmpty)
                style = DefaultStyle;

            m_styles = new Dictionary<WidgetStyleType, WidgetStyleSheet>();

            m_styleType = WidgetStyleType.Normal;
            m_style = style;

            LoadStyle(WidgetStyleType.Normal, style);

            Size = style.Get(WidgetParameterIndex.Size, new Vector2(0, 0));
        }

        #region Styles

        /// <summary>
        /// Makes shallow copy of the style object for own modifications
        /// </summary>

        protected bool HasStyle(WidgetStyleType styleType)
        {
            return m_styles.ContainsKey(styleType);
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
                DelayedSwitchStyle(type);
        }

        /// <summary>
        /// Switches the style.
        /// </summary>
        /// <param name="styleType">Style type.</param>
        public virtual void SwitchStyle(WidgetStyleType styleType)
        {
            if (!HasStyle(styleType))
                return;

            m_style = m_styles[styleType];
            m_styleType = styleType;
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

            m_styles[styleType] = style;

            // Hovered can be only subset of Normal, Disabled, Selected or SelectedDisabled
            if (styleType == WidgetStyleType.Normal || styleType == WidgetStyleType.Disabled || styleType == WidgetStyleType.Selected || styleType == WidgetStyleType.SelectedDisabled)
            {
                var hoveredStyleReference = WidgetManager.GetStyle(style.Get(WidgetParameterIndex.HoveredStyle, ""));

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
                var disabledStyleReference = WidgetManager.GetStyle(style.Get(WidgetParameterIndex.DisabledStyle, ""));

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
                var selectedStyleReference = WidgetManager.GetStyle(style.Get(WidgetParameterIndex.SelectedStyle, ""));

                if (!selectedStyleReference.IsEmpty)
                {
                    WidgetStyleType targetStyleType = 0;
                    switch (styleType)
                    {
                        case WidgetStyleType.Normal:
                            targetStyleType = WidgetStyleType.Selected;
                            break;
                    }

                    if (targetStyleType != 0)
                        LoadStyle(targetStyleType, selectedStyleReference);
                }
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
                Vector2 actualScale = Transform.ActualScale;

                WindowController.Instance.SetClipRect(
                    (int)(this.Transform.ActualPosition.X + ClipMargin.Left * actualScale.X),
                    (int)(this.Transform.ActualPosition.Y + ClipMargin.Top * actualScale.Y),
                    (int)((this.Size.X - ClipMargin.Left - ClipMargin.Right) * actualScale.X + 0.5f),
                    (int)((this.Size.Y - ClipMargin.Top - ClipMargin.Bottom) * actualScale.Y + 0.5f));
            }

            DrawContents(canvas);
            
            if (ClipContents)
                WindowController.Instance.CancelClipRect();
        }

        public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            if ((!string.IsNullOrEmpty(m_tooltip) || OnTooltip != null) && ((pointer == 0 && !unpress && !press) || (pointer != 0 && press)))
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

