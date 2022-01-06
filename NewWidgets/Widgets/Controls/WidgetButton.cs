﻿using System;
using System.Numerics;
using NewWidgets.UI;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    [Flags]
    public enum WidgetButtonLayout
    {
        Center = 0,
        ImageLeft = 0x01,
        ImageRight = 0x02,
        TextLeft = 0x04,
        TextRight = 0x08
    }

    public class WidgetButton : WidgetBackground
    {
        public static readonly new WidgetStyleSheet DefaultStyle = WidgetManager.GetStyle("default_button", true);

        private readonly WidgetLabel m_label;
        private readonly WidgetImage m_image;

        private string m_clickSound;

        private bool m_animating;
        private bool m_overridePress;

        public event Action<WidgetButton> OnPress;
        public event Action<WidgetButton> OnHover;
        public event Action<WidgetButton> OnUnhover;

        // Dynamic properties

        public string Text
        {
            get { return m_label.Text; }
            set { m_label.Text = value; InvalidateLayout(); }
        }

        public string Image
        {
            get { return m_image.Image; }
            set { m_image.Image = value; InvalidateLayout(); }
        }

        // Forwarded style properties

        public uint ImageTint
        {
            get { return m_image.Color; }
            set { m_image.Color = value; }
        }

        public uint TextColor
        {
            get { return m_label.Color; }
            set { m_label.Color = value; }
        }

        public Font Font
        {
            get { return m_label.Font; }
            set { m_label.Font = value; InvalidateLayout(); }
        }

        public float FontSize
        {
            get { return m_label.FontSize; }
            set { m_label.FontSize = value; InvalidateLayout(); }
        }

        // Own style properties

        public WidgetButtonLayout Layout
        {
            get { return GetProperty(WidgetParameterIndex.ButtonLayout, WidgetButtonLayout.Center); }
            set { SetProperty(WidgetParameterIndex.ButtonLayout, value); InvalidateLayout(); }
        }

        public Margin ImagePadding
        {
            get { return GetProperty(WidgetParameterIndex.ButtonImagePadding, new Margin(0)); }
            set { SetProperty(WidgetParameterIndex.ButtonImagePadding, value); InvalidateLayout(); }
        }

        public Margin TextPadding
        {
            get { return GetProperty(WidgetParameterIndex.ButtonTextPadding, new Margin(0)); }
            set { SetProperty(WidgetParameterIndex.ButtonTextPadding, value); InvalidateLayout(); }
        }

        public Vector2 AnimatePivot
        {
            get { return GetProperty(WidgetParameterIndex.ButtonAnimatePivot, new Vector2(0.5f, 0.5f)); }
            set { SetProperty(WidgetParameterIndex.ButtonAnimatePivot, value); }
        }

        public int AnimateTime
        {
            get { return GetProperty(WidgetParameterIndex.ButtonAnimateTime, 100); }
            set { SetProperty(WidgetParameterIndex.ButtonAnimateTime, value); }
        }

        public float AnimateScale
        {
            get { return GetProperty(WidgetParameterIndex.ButtonAnimateScale, 0.95f); }
            set { SetProperty(WidgetParameterIndex.ButtonAnimateScale, value); }
        }

        // Button properties

        public string ClickSound
        {
            get { return m_clickSound; }
            set { m_clickSound = value; }
        }
        
        public bool OverridePress
        {
            get { return m_overridePress; }
            set { m_overridePress = value; }
        }

        public bool IsImageVisible
        {
            get { return !string.IsNullOrEmpty(m_image.Image); }
        }

        public bool IsLabelVisible
        {
            get { return !string.IsNullOrEmpty(m_label.Text); }
        }

        protected WidgetImage InternalImage
        {
            get { return m_image; }
        }

        protected WidgetLabel InternalLabel
        {
            get { return m_label; }
        }

        public override string StyleElementType
        {
            get { return "button"; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetButton"/> class.
        /// </summary>
        /// <param name="text">Text.</param>
        public WidgetButton(string text)
            : this(default(WidgetStyleSheet), text)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetButton"/> class.
        /// </summary>
        /// <param name="style">Style.</param>
        /// <param name="text">Text.</param>
        public WidgetButton(WidgetStyleSheet style = default(WidgetStyleSheet), string text = "")
           : base(style.IsEmpty ? DefaultStyle : style)
        {
            // This one is for compatibility reasons: nested styles were used in XML stylesheet
            m_label = new WidgetLabel(WidgetManager.GetStyle(GetProperty(WidgetParameterIndex.ButtonTextStyle, "")), text);
            m_label.Parent = this;

            //m_image = new WidgetImage(GetProperty(WidgetParameterIndex.ButtonImageStyle, style.IsEmpty ? DefaultStyle : style));
            m_image = new WidgetImage(WidgetManager.GetStyle(GetProperty(WidgetParameterIndex.ButtonImageStyle, "")));
            m_image.Parent = this;

            m_clickSound = "click";
        }

        /// <summary>
        /// Updates 
        /// </summary>
        public override void UpdateStyle()
        {
            base.UpdateStyle();

            m_label.UpdateStyle();

            m_image.UpdateStyle();
        }

        public override void UpdateLayout()
        {
            if (IsImageVisible)
            {
                m_image.Size = Size - ImagePadding.Size;
                m_image.UpdateLayout();
            }


            if (IsLabelVisible)
            {
                m_label.Size = Size - TextPadding.Size;
                m_label.UpdateLayout();

            }

            // now we're ready for automatic resize if needed

            // auto-resize attempt. Little bit clumsy and ignores image size and button layout. TODO: re-implement this part
            if (Size.X <= 0 || Size.Y <= 0)
            {
                Size = new Vector2(Math.Max(TextPadding.Width + m_label.Size.X, ImagePadding.Width + m_image.Size.X), Math.Max(TextPadding.Height + m_label.Size.Y, ImagePadding.Height + m_image.Size.Y));
            }


            if (IsLabelVisible)
            {
                m_label.Position = TextPadding.TopLeft;

                if ((Layout & WidgetButtonLayout.TextLeft) != 0)
                {
                    m_label.TextAlign = WidgetAlign.Left | WidgetAlign.Top;
                }
                else
                {
                    m_label.TextAlign = WidgetAlign.VerticalCenter | WidgetAlign.HorizontalCenter;
                }
            }

            if (m_image != null && !string.IsNullOrEmpty(m_image.Image))
            {
                m_image.Position = ImagePadding.TopLeft;

                if ((Layout & WidgetButtonLayout.ImageLeft) != 0)
                {
                    m_image.ImageStyle = WidgetBackgroundStyle.ImageTopLeft;
                    m_image.ImagePivot = new Vector2(0, 0);
                }
                else
                {
                    m_image.ImageStyle = WidgetBackgroundStyle.ImageFit;
                    // ImagePivot?
                }
            }
            /*else
            {

                m_label.Size = new Vector2(Size.X - m_textPadding.Width, Size.Y - m_textPadding.Height);
                m_label.Position = m_textPadding.TopLeft;
            }*/


            base.UpdateLayout();
        }

        public override bool Update()
        {
            if (!base.Update())
                return false;
            
            if (m_image != null && !string.IsNullOrEmpty(m_image.Image))
                m_image.Update();

            if (m_label != null && !string.IsNullOrEmpty(m_label.Text))
                m_label.Update();

            return true;
        }

        protected override void DrawContents()
        {
            base.DrawContents();

            if (m_image != null && !string.IsNullOrEmpty(m_image.Image))
                m_image.Draw();

            if (m_label != null && !string.IsNullOrEmpty(m_label.Text))
                m_label.Draw();
        }

        
        public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            if (m_animating)
                return true;
            //if (Enabled)
            {
                if (Enabled && press)
                {
                    if (!m_overridePress)
                    {
                        return true;
                    }
                } else
                if (Enabled && unpress)
                {
                    if (!m_overridePress)
                    {
                        Press();
                        return true;
                    }
                }
                else if (!press && !unpress && !Hovered)
                {
                    Hovered = true;
                    WindowController.Instance.OnTouch += UnHoverTouch;

                    if (OnHover != null)
                        OnHover(this);
                }
            }

            return base.Touch(x, y, press, unpress, pointer);
        }

        private bool UnHoverTouch(float x, float y, bool press, bool unpress, int pointer)
        {
            if (Hovered && !HitTest(x, y))
            {
                Hovered = false;
                WindowController.Instance.OnTouch -= UnHoverTouch;

                if (OnUnhover != null)
                    OnUnhover(this);
            }
            return false;
        }

        public void Press(bool immediate = false)
        {
            if (!Enabled)
                return;

            if (!string.IsNullOrEmpty(m_clickSound))
                WindowController.Instance.PlaySound(m_clickSound);

            AnimatePress(immediate);
        }

        protected virtual void AnimatePress(bool immediate = false, bool animateOnly = false)
        {
            if (m_animating)
                return;

            if (immediate && !animateOnly)
                SchedulePress();

            Vector2 animatePivot = AnimatePivot; // center point compensation
            float animateTo = AnimateScale; // scales down to this value
            int animateTime = AnimateTime; // animations takes animateTime*2 milliseconds

            m_animating = true;
            float startScale = Scale;
            ScaleTo(startScale * animateTo, animateTime,
                delegate
                {
                    ScaleTo(startScale, animateTime,
                        delegate
                        {
                            m_animating = false;
                            if (!immediate && !animateOnly)
                                SchedulePress();
                        }
                    );
                });

            // compensate non-center pivot point. Not needed if pivot is top-left
            if (animatePivot.LengthSquared() > 0)
            {
                Vector2 startPosition = Position;
                Move(startPosition + (Size * startScale * animatePivot) * (1.0f - animateTo), animateTime,
                    delegate
                    {
                        Move(startPosition, animateTime, null);
                    });
            }
        }

        protected void SchedulePress()
        {
            var onPress = OnPress;

            if (onPress != null)
                WindowController.Instance.ScheduleAction(delegate { onPress(this); }, 1);
        }
    }
}

