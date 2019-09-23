using System;
using System.Numerics;
using NewWidgets.UI;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    public class WidgetCheckBox : WidgetBackground
    {
        public static readonly new WidgetStyleSheet DefaultStyle = WidgetManager.GetStyle("default_checkbox", true);

        private WidgetImage m_image;
        private WidgetLabel m_linkedLabel;

        private bool m_animating;

        public event Action<WidgetCheckBox> OnChecked;

        public bool Checked
        {
            get { return Selected; }
            set
            {
                if (m_image != null)
                {
                    m_image.Position = value ? ImagePadding.TopLeft : (ImagePadding.TopLeft + new Vector2(0, Size.Y / 4.0f));
                    m_image.Alpha = value ? 1.0f : 0.0f;
                }

                Selected = value;
            }
        }

        public string Image
        {
            get { return m_image.Image; }
            set { m_image.Image = value; }
        }

        public WidgetLabel LinkedLabel
        {
            get { return m_linkedLabel; }
            set
            {
                if (m_linkedLabel != null)
                    m_linkedLabel.OnTouch -= Touch;

                m_linkedLabel = value;
                m_linkedLabel.OnTouch += Touch;
            }
        }

        public Margin ImagePadding
        {
            get { return GetProperty(WidgetParameterIndex.ButtonImagePadding, new Margin(0)); }
            set { SetProperty(WidgetParameterIndex.ButtonImagePadding, value); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetCheckBox"/> class.
        /// </summary>
        /// <param name="isChecked">If set to <c>true</c> is checked.</param>
        public WidgetCheckBox(bool isChecked)
            : this(default(WidgetStyleSheet), isChecked)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetCheckBox"/> class.
        /// </summary>
        /// <param name="style">Style.</param>
        /// <param name="isChecked">If set to <c>true</c> is checked.</param>
        public WidgetCheckBox(WidgetStyleSheet style = default(WidgetStyleSheet), bool isChecked = false)
            : base(style.IsEmpty ? DefaultStyle : style)
        {
            m_image = new WidgetImage(GetProperty(WidgetParameterIndex.ButtonImageStyle, style.IsEmpty ? DefaultStyle : style));
            m_image.Parent = this;

            Selected = isChecked;

            Resize(Size);
        }

        protected override void Resize(Vector2 size)
        {
            base.Resize(size);

            if (m_image != null)
            {
                m_image.Size = new Vector2(size.X - ImagePadding.Width, size.Y - ImagePadding.Height);
                m_image.Position = ImagePadding.TopLeft;
            }
        }

        public override bool SwitchStyle(WidgetStyleType styleType)
        {
            if (base.SwitchStyle(styleType))
            {
                if (m_image != null)
                    m_image.SwitchStyle(styleType);

                if (m_linkedLabel != null)
                    m_linkedLabel.SwitchStyle(styleType);

                return true;
            }
            return false;
        }

        public override bool Update()
        {
            if (!base.Update())
                return false;

            m_image.Update();

            return true;
        }

        protected override void DrawContents(object canvas)
        {
            base.DrawContents(canvas);

            if (m_image != null && (Checked || m_animating))
                m_image.Draw(canvas);
        }

        public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            if (Enabled)
            {
                if (press)
                {
                    return true;
                } else
                if (unpress)
                {
                    Press();
                    return true;
                }
                else if (!Hovered)
                {
                    Hovered = true;
                    WindowController.Instance.OnTouch += UnHoverTouch;
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
            }
            return false;
        }

        public void Press()
        {
            if (!Enabled)
                return;

            //GameSound.PlaySound(m_clickSound);

            Checked = !Checked;
            
            AnimatePress();
        }

        protected virtual void AnimatePress()
        {
            m_animating = true;

            int animateTime = 100;
            if (Checked)
            {
                m_image.Alpha = 0.0f;
                m_image.Position = ImagePadding.TopLeft + new Vector2(0, Size.Y / 4.0f);
                m_image.Move(ImagePadding.TopLeft, animateTime, AnimateFinished);
                m_image.FadeTo(1.0f, animateTime, null);
            } else
            {
                m_image.Alpha = 1.0f;
                m_image.Position = ImagePadding.TopLeft;
                m_image.Move(ImagePadding.TopLeft + new Vector2(0, Size.Y / 4.0f), animateTime, AnimateFinished);
                m_image.FadeTo(0.0f, animateTime, null);
            }
        }

        protected void AnimateFinished()
        {
            m_animating = false;
            
            if (OnChecked != null)
                WindowController.Instance.ScheduleAction(delegate { OnChecked(this); }, 1);
        }
    }
}

