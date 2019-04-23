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
        public static readonly WidgetStyleReference DefaultStyle = WidgetManager.RegisterDefaultStyle<WidgetStyleSheet>("default");

        public delegate bool TooltipDelegate(Widget sender, string text, Vector2 position);

        private readonly Dictionary<WidgetStyleType, WidgetStyleReference> m_styles;
        protected WidgetStyleReference m_style;
        private WidgetStyleType m_styleType;

        private float m_alpha = 1.0f; // the only property that could be changed for simple widget without affecting its stylesheet

        private string m_tooltip;

        #region Style-related stuff

        public WidgetStyleType StyleType
        {
            get { return m_styleType; }
        }

        /// <summary>
        /// Private style getter. All descendants should implement their own property using m_style.Get
        /// </summary>
        /// <value>The style.</value>
        private WidgetStyleSheet Style
        {
            get { return m_style.Get<WidgetStyleSheet>(); }
        }

        /// <summary>
        /// This property is mandatory for modifying personal style of a widget. Trying to write something to non-cloned style should raise an assertion
        /// All descendants should implement their own property using m_style.Get(this)
        /// </summary>
        /// <value>The writable style.</value>
        private WidgetStyleSheet WritableStyle
        {
            get { return m_style.Get<WidgetStyleSheet>(this); }
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

        protected Widget(WidgetStyleReference style = default(WidgetStyleReference))
            : base(null)
        {
            if (style.IsEmpty)
                style = DefaultStyle;

            m_styles = new Dictionary<WidgetStyleType, WidgetStyleReference>();

            m_styleType = WidgetStyleType.Normal;
            m_style = style;

            LoadStyle(WidgetStyleType.Normal, style);

            Size = Style.Size;
        }

        #region Styles

        protected bool HasStyle(WidgetStyleType styleType)
        {
            WidgetStyleReference style;

            if (!m_styles.TryGetValue(styleType, out style))
                return false;

            return style.IsValid;
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
            WidgetStyleReference style;

            if (!m_styles.TryGetValue(styleType, out style))
                return;

            m_styleType = styleType;
            m_style = style;
        }

        /// <summary>
        /// Loads the style and possible sub-styles
        /// </summary>
        /// <param name="styleType">Style type.</param>
        /// <param name="style">Style.</param>
        public void LoadStyle(WidgetStyleType styleType, WidgetStyleReference style)
        {
            if (style.IsEmpty)
                return;

            m_styles[styleType] = style;

            // Hovered can be only subset of Normal, Disabled, Selected or SelectedDisabled
            if (styleType == WidgetStyleType.Normal || styleType == WidgetStyleType.Disabled || styleType == WidgetStyleType.Selected || styleType == WidgetStyleType.SelectedDisabled)
            {
                var hoveredStyleReference = style.Get<WidgetStyleSheet>().HoveredStyle;

                if (hoveredStyleReference.IsValid)
                {
                    if (hoveredStyleReference.Type != m_style.Type && !hoveredStyleReference.Type.IsSubclassOf(m_style.Type))
                        throw new Exception(string.Format("Hovered style {0} for {1} has incompatible type", hoveredStyleReference, this));

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
                var disabledStyleReference = style.Get<WidgetStyleSheet>().DisabledStyle;

                if (disabledStyleReference.IsValid)
                {
                    if (disabledStyleReference.Type != m_style.Type && !disabledStyleReference.Type.IsSubclassOf(m_style.Type))
                        throw new Exception(string.Format("Disabled style {0} for {1} has incompatible type", disabledStyleReference, this));

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
                var selectedStyleReference = style.Get<WidgetStyleSheet>().SelectedStyle;

                if (selectedStyleReference.IsValid)
                {
                    if (selectedStyleReference.Type != m_style.Type && !selectedStyleReference.Type.IsSubclassOf(m_style.Type))
                        throw new Exception(string.Format("Selected style {0} for {1} has incompatible type", selectedStyleReference, this));

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

