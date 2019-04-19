using System;
using System.Collections.Generic;
using System.Numerics;
using NewWidgets.UI;
using NewWidgets.Utility;
using NewWidgets.Widgets.Styles;

#if RUNMOBILE
using RunMobile.Utility;
#endif

namespace NewWidgets.Widgets
{
    public class Widget : WindowObject
    {
        public static readonly WidgetStyleReference<WidgetStyleSheet> DefaultStyle = new WidgetStyleReference<WidgetStyleSheet>("default");

        public delegate bool TooltipDelegate(Widget sender, string text, Vector2 position);

        private readonly Dictionary<WidgetStyleType, WidgetStyleSheet> m_styles;
        private WidgetStyleType m_currentStyleType;
        private WidgetStyleSheet m_currentStyle;

        private float m_alpha; // the only property that could be changed for simple widget without affecting its stylesheet

        private string m_tooltip;

        public WidgetStyleType CurrentStyleType
        {
            get { return m_currentStyleType; }
        }

        public WidgetStyleSheet Style
        {
            get { return m_currentStyle; }
        }

        /// <summary>
        /// This property is mandatory for modifying personal style of a widget. Trying to write something to non-cloned style should raise an assertion
        /// </summary>
        /// <value>The writable style.</value>
        protected WidgetStyleSheet WritableStyle
        {
            get
            {
                WidgetStyleSheet clone = m_currentStyle.Clone(this);
                if (clone != null)
                {
                    m_styles[m_currentStyleType] = clone;
                    m_currentStyle = clone;
                }

                return m_currentStyle;
            }
        }

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
            get { return Style.ClipContents; }
            set { WritableStyle.ClipContents = value; }
        }

        public Margin ClipMargin
        {
            get { return Style.ClipMargin; }
            set { WritableStyle.ClipMargin = value; }
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

        protected Widget(WidgetStyleSheet style = null)
            : base(null)
        {
            if (style == null)
                style = DefaultStyle;

            m_styles = new Dictionary<WidgetStyleType, WidgetStyleSheet>();

            LoadStyle(WidgetStyleType.Normal, style);

            m_currentStyleType = WidgetStyleType.Normal;
            m_currentStyle = style;

            Size = style.Size;
        }

        #region Styles

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

            DelayedSwitchStyle(type);
        }

        /// <summary>
        /// Switches the style.
        /// </summary>
        /// <param name="styleType">Style type.</param>
        public virtual void SwitchStyle(WidgetStyleType styleType)
        {
            WidgetStyleSheet style;

            if (!m_styles.TryGetValue(styleType, out style))
                throw new ArgumentNullException(nameof(style), "Invalid style " + styleType);

            m_currentStyleType = styleType;
            m_currentStyle = style;
        }

        /// <summary>
        /// Loads the style and possible sub-styles
        /// </summary>
        /// <param name="styleType">Style type.</param>
        /// <param name="style">Style.</param>
        public void LoadStyle(WidgetStyleType styleType, WidgetStyleSheet style)
        {
            m_styles[styleType] = style;

            string hoveredStyleName = style.HoveredStyle;

            // Hovered can be only subset of Normal, Disabled, Selected or SelectedDisabled
            if (styleType == WidgetStyleType.Normal || styleType == WidgetStyleType.Disabled || styleType == WidgetStyleType.Selected || styleType == WidgetStyleType.SelectedDisabled)
            {
                if (!string.IsNullOrEmpty(hoveredStyleName))
                {
                    WidgetStyleSheet hoveredStyle = WidgetManager.GetStyle(hoveredStyleName);

                    if (hoveredStyle != null && hoveredStyle != style)
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
                            LoadStyle(targetStyleType, hoveredStyle);
                    }
                }
            }


            // Disabled can be only subset of Normal or Selected
            if (styleType == WidgetStyleType.Normal || styleType == WidgetStyleType.Selected)
            {

                string disabledStyleName = style.DisabledStyle;

                if (!string.IsNullOrEmpty(disabledStyleName))
                {
                    WidgetStyleSheet disabledStyle = WidgetManager.GetStyle(disabledStyleName);

                    if (disabledStyle != null && disabledStyle != style)
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
                            LoadStyle(targetStyleType, disabledStyle);
                    }
                }
            }

            // Selected can be only subset of Normal
            if (styleType == WidgetStyleType.Normal)
            {
                string selectedStyleName = style.SelectedStyle;

                if (!string.IsNullOrEmpty(selectedStyleName))
                {
                    WidgetStyleSheet selectedStyle = WidgetManager.GetStyle(selectedStyleName);

                    if (selectedStyle != null && selectedStyle != style)
                    {
                        WidgetStyleType targetStyleType = 0;
                        switch (styleType)
                        {
                            case WidgetStyleType.Normal:
                                targetStyleType = WidgetStyleType.Selected;
                                break;
                        }

                        if (targetStyleType != 0)
                            LoadStyle(targetStyleType, selectedStyle);
                    }
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

