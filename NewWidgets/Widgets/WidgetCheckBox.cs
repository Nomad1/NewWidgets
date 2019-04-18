using System;
using System.Numerics;
using NewWidgets.UI;
using NewWidgets.Utility;
using NewWidgets.Widgets.Styles;

namespace NewWidgets.Widgets
{
    public class WidgetCheckBox : Widget
    {
        private WidgetImage m_image;
        private WidgetAlign m_imageAlign;
        private Margin m_imagePadding;
        private WidgetLabel m_linkedLabel;

        private bool m_animating;

        public event Action<WidgetCheckBox> OnChecked;

        public bool Checked
        {
            get { return Selected; }
            set { Selected = value; }
        }

        public string Image
        {
            get { return m_image.Image; }
            set { m_image.Image = value; }
        }

        public WidgetAlign ImageAlign
        {
            get { return m_imageAlign; }
            set { m_imageAlign = value; }
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
            get { return m_imagePadding; }
            set { m_imagePadding = value; }
        }

        public WidgetCheckBox()
            : this(WidgetManager.DefaultCheckBoxStyle, false)
        {
        }

        public WidgetCheckBox(bool isChecked)
            : this(WidgetManager.DefaultCheckBoxStyle, isChecked)
        {
        }
        
        public WidgetCheckBox(WidgetStyleSheet style, bool isChecked)
            : base(style)
        {
            m_image = new WidgetImage(WidgetBackgroundStyle.ImageFit, style.GetParameter("check_image"));
            m_image.Parent = this;

            m_imageAlign = WidgetAlign.VerticalCenter | WidgetAlign.HorizontalCenter;

            Size = style.Size;

            Selected = isChecked;
        }

        protected override void Resize(Vector2 size)
        {
            base.Resize(size);

            if (m_image != null)
            {
                m_image.Size = new Vector2(size.X - m_imagePadding.Width, size.Y - m_imagePadding.Height);
                m_image.Position = m_imagePadding.TopLeft;
            }
        }

        public override void SwitchStyle(WidgetStyleType styleType)
        {
            base.SwitchStyle(styleType);

            if (m_linkedLabel != null)
                m_linkedLabel.SwitchStyle(styleType);
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

            if (!string.IsNullOrEmpty(Image) && (Selected || m_animating))
                m_image.Draw(Image);
        }

        public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            if (Enabled)
            {
                if (unpress)
                {
                    Press();
                    return true;
                }
                else if (!press && !unpress && !Hovered)
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
            
            if (Checked)
            {
                m_image.Position = m_imagePadding.TopLeft + new Vector2(0, 10);
                m_image.Move(m_imagePadding.TopLeft, 100, AnimateFinished);
                m_image.FadeTo(1.0f, 100, null);
            } else
            {
                m_image.Position = m_imagePadding.TopLeft;
                m_image.Move(m_imagePadding.TopLeft + new Vector2(0, 10), 100, AnimateFinished);
                m_image.FadeTo(0.0f, 100, null);
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

