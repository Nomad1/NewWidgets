using System;
using System.Numerics;
using NewWidgets.UI;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    public class WidgetCheckBox : Widget
    {
        private WidgetImage m_image;
        private WidgetAlign m_imageAlign;
        private Margin m_imagePadding;
        private WidgetLabel m_linkedLabel;

        private bool m_animating;
        private bool m_checked;
        private bool m_hovered;

        private readonly WidgetStyleSheet m_hoveredStyle;
        private readonly WidgetStyleSheet m_disabledStyle;

        public event Action<WidgetCheckBox> OnChecked;

        public bool Checked
        {
            get { return m_checked; }
            set { m_checked = value; }
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

        public override bool Enabled
        {
            get
            {
                return base.Enabled;
            }
            set
            {
                base.Enabled = value;

                if (m_disabledStyle != null)
                    ApplyStyle(value ? Style : m_disabledStyle);
            }
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
            m_image.Color = style.GetParameterColor("image_color", 0xffffff);
            m_imagePadding = style.GetParameter<Margin>("image_padding");

            m_imageAlign = WidgetAlign.VerticalCenter | WidgetAlign.HorizontalCenter;

            Size = style.Size;

            m_checked = isChecked;

            m_hoveredStyle = WidgetManager.GetStyle(style.GetParameter("hovered_style"));
            m_disabledStyle = WidgetManager.GetStyle(style.GetParameter("disabled_style"));
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

        public override bool Update()
        {
            if (!base.Update())
                return false;

            m_image.Update();

            return true;
        }
        
        protected override void ApplyStyle(WidgetStyleSheet style)
        {
            m_image.Image = style.GetParameter("check_image");
            m_image.Color = style.GetParameterColor("image_color", 0xffffff);

            if (m_linkedLabel != null)
                m_linkedLabel.Color = style.GetParameterColor("text_color", 0xffffff);

            base.ApplyStyle(style);
        }

        protected override void DrawContents(object canvas)
        {
            base.DrawContents(canvas);

            if (!string.IsNullOrEmpty(Image) && (m_checked || m_animating))
                m_image.Draw(Image);
        }

        public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            if (Enabled)
            {
                if (unpress)
                    Press();
                else if (!press && !unpress && !m_hovered && m_hoveredStyle != null)
                {
                    ApplyStyle(m_hoveredStyle);
                    
                    m_hovered = true;
                    WindowController.Instance.OnTouch += UnHoverTouch;
                }

                return true;
            }

            return false;
        }

        private bool UnHoverTouch(float x, float y, bool press, bool unpress, int pointer)
        {
            if (m_hovered && !HitTest(x, y))
            {
                ApplyStyle(Style);
                
                m_hovered = false;
                WindowController.Instance.OnTouch -= UnHoverTouch;
            }
            return false;
        }

        public void Press()
        {
            if (!Enabled)
                return;

            //GameSound.PlaySound(m_clickSound);

            m_checked = !m_checked;
            
            AnimatePress();
        }

        protected virtual void AnimatePress()
        {
            m_animating = true;
            
            if (m_checked)
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

