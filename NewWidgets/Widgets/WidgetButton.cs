using System;
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

        private WidgetLabel m_label;
        private WidgetImage m_image;

        private string m_clickSound;

        private bool m_animating;
        private bool m_overridePress;

        public event Action<WidgetButton> OnPress;
        public event Action<WidgetButton> OnHover;
        public event Action<WidgetButton> OnUnhover;

        private bool m_needLayout;

        // Dynamic properties

        public string Text
        {
            get { return m_label.Text; }
            set { m_label.Text = value; m_needLayout = true; }
        }

        public string Image
        {
            get { return m_image.Image; }
            set { m_image.Image = value; m_needLayout = true; }
        }

        // Forwarded style properties

        public int ImageTint
        {
            get { return m_image.Color; }
            set { m_image.Color = value; }
        }

        public int TextColor
        {
            get { return m_label.Color; }
            set { m_label.Color = value; }
        }

        public Font Font
        {
            get { return m_label.Font; }
            set { m_label.Font = value; m_needLayout = true; }
        }

        public float FontSize
        {
            get { return m_label.FontSize; }
            set { m_label.FontSize = value; m_needLayout = true; }
        }

        // Own style properties

        public WidgetButtonLayout Layout
        {
            get { return GetProperty(WidgetParameterIndex.ButtonLayout, WidgetButtonLayout.Center); }
            set { SetProperty(WidgetParameterIndex.ButtonLayout, value); m_needLayout = true; }
        }

        public Margin ImagePadding
        {
            get { return GetProperty(WidgetParameterIndex.ButtonImagePadding, new Margin(0)); }
            set { SetProperty(WidgetParameterIndex.ButtonImagePadding, value); m_needLayout = true; }
        }

        public Margin TextPadding
        {
            get { return GetProperty(WidgetParameterIndex.ButtonTextPadding, new Margin(0)); }
            set { SetProperty(WidgetParameterIndex.ButtonTextPadding, value); m_needLayout = true; }
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
       
        protected WidgetImage InternalImage
        {
            get { return m_image; }
        }

        protected WidgetLabel InternalLabel
        {
            get { return m_label; }
        }

        public override float Alpha
        {
            get { return base.Alpha;}
            set
            {
                base.Alpha = value;
                if (m_image != null)
                    m_image.Alpha = value;

                if (m_label != null)
                    m_label.Alpha = value;
            }
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

            m_needLayout = true;

            m_label = new WidgetLabel(GetProperty(WidgetParameterIndex.ButtonTextStyle, style.IsEmpty ? DefaultStyle : style), text);
            m_label.Parent = this;

            m_image = new WidgetImage(GetProperty(WidgetParameterIndex.ButtonImageStyle, style.IsEmpty ? DefaultStyle : style));
            m_image.Parent = this;

            m_clickSound = "click";
        }

        public override bool SwitchStyle(WidgetStyleType styleType)
        {
            if (base.SwitchStyle(styleType))
            {
                m_label.SwitchStyle(styleType);
                m_image.SwitchStyle(styleType);

                return true;
            }
            return false;
        }

        protected override void Resize(Vector2 size)
        {
            base.Resize(size);

            m_needLayout = true;
        }

        public virtual void Relayout()
        {
            if (m_label != null && (Size.X <= 0 || Size.Y <= 0))
            {
                m_label.Relayout();

                Size = new Vector2(Math.Max(TextPadding.Width + m_label.Size.X, ImagePadding.Width + m_image.Size.X), Math.Max(TextPadding.Height + m_label.Size.Y, ImagePadding.Height + m_image.Size.Y));
            }

            if (m_label != null && !string.IsNullOrEmpty(m_label.Text))
            {
                if ((Layout & WidgetButtonLayout.TextLeft) != 0)
                {
                    m_label.Size = new Vector2(Size.X - TextPadding.Width, Size.Y - TextPadding.Height);
                    m_label.Position = TextPadding.TopLeft;
                    m_label.TextAlign = WidgetAlign.Left | WidgetAlign.Top;
                }
                else
                {
                    m_label.TextAlign = WidgetAlign.VerticalCenter | WidgetAlign.HorizontalCenter;
                    m_label.Size = new Vector2(Size.X - TextPadding.Width, Size.Y - TextPadding.Height);
                    m_label.Position = TextPadding.TopLeft;
                }
            }

            if (m_image != null && !string.IsNullOrEmpty(m_image.Image))
            {
                if ((Layout & WidgetButtonLayout.ImageLeft) != 0)
                {
                    m_image.Size = new Vector2(Size.X - ImagePadding.Width, Size.Y - ImagePadding.Height);
                    m_image.ImageStyle = WidgetBackgroundStyle.ImageTopLeft;
                    m_image.ImagePivot = new Vector2(0, 0);
                    m_image.Position = ImagePadding.TopLeft;
                }
                else
                {
                    m_image.Size = new Vector2(Size.X - ImagePadding.Width, Size.Y - ImagePadding.Height);
                    m_image.Position = ImagePadding.TopLeft;
                    m_image.ImageStyle = WidgetBackgroundStyle.ImageFit;
                }
            }
            /*else
            {

                m_label.Size = new Vector2(Size.X - m_textPadding.Width, Size.Y - m_textPadding.Height);
                m_label.Position = m_textPadding.TopLeft;
            }*/

            m_needLayout = false;
        }

        public override bool Update()
        {
            if (m_needLayout)
                Relayout();
        
            if (!base.Update())
                return false;
            
            if (m_image != null)
            {
                m_image.Alpha = Alpha;
                m_image.Update();
            }

            if (m_label != null)
            {
                m_label.Alpha = Alpha;
                m_label.Update();
            }

            return true;
        }

        protected override void DrawContents(object canvas)
        {
            base.DrawContents(canvas);
            
            if (!string.IsNullOrEmpty(Image))
                m_image.Draw(Image);
            
            if (!string.IsNullOrEmpty(Text))
                m_label.Draw(canvas);
        }

        
        public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            if (Enabled)
            {
                if (press)
                {
                    if (!m_overridePress)
                    {
                        return true;
                    }
                } else
                if (unpress)
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

        protected virtual void AnimatePress(bool immediate = false)
        {
            if (m_animating)
                return;

            if (immediate)
                SchedulePress();

            m_animating = true;
            float startScale = Scale;
            ScaleTo(startScale * 0.95f, 100,
                delegate
                {
                    ScaleTo(startScale, 100,
                        delegate
                        {
                            m_animating = false;
                            if (!immediate)
                                SchedulePress();
                        }
                    );
                });
            
            // compensate non-center pivot point
            Vector2 startPosition = Position;
            Move(startPosition + (Size * startScale * 0.5f) * (1 - 0.95f), 100,
                delegate
                {
                    Move(startPosition, 100, null);
                });
        }

        protected void SchedulePress()
        {
            if (OnPress != null)
                WindowController.Instance.ScheduleAction(delegate { OnPress(this); }, 1);
        }
    }
}

