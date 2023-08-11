using System;
using System.Numerics;
using NewWidgets.UI;

namespace NewWidgets.Widgets
{
    /// <summary>
    /// Checkbox class. Note, that it contains a WidgetImage with id #checkbox_image for the check mark
    /// All the styles related to the image should be put to it's style. Check image is always zoomed to take the whole checkbox.
    /// Nomad: there is remaints of ancient checkbox animation: check image is appearing from the below (Size.Y/4) and fading in.
    /// Same goes for disappearing, whick movement to the bottom. It's not very customizable and could be changed in the future
    /// </summary>
    public class WidgetCheckBox : WidgetBackground
    {
        public new const string ElementType = "checkbox";
        //
        private const string ImageId = "checkbox_image";

        private readonly WidgetImage m_image;
        private WidgetLabel m_linkedLabel;

        private bool m_animating;

        /// <summary>
        /// This event is raized after checkbox value has been changed. Note that settings Checked = value does not calls this method
        /// </summary>
        public event Action<WidgetCheckBox> OnChecked;

        /// <summary>
        /// This event allows to cancel the checkbox mark if false is returned
        /// </summary>
        public event Func<WidgetCheckBox, bool> OnCheckChanged;

        public bool Checked
        {
            get { return Selected; }
            set
            {
                if (OnCheckChanged != null && !OnCheckChanged(this))
                    return;

                if (m_image != null)
                {
                    m_image.Position = value ? Vector2.Zero : new Vector2(0, Size.Y / 4.0f); // Size.Y/4 is needed for animation
                    m_image.Opacity = value ? 1.0f : 0.0f;
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

        /// <summary>
        /// Direct access to the underlying WidgetImage
        /// </summary>
        protected WidgetImage InternalImage
        {
            get { return m_image; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetCheckBox"/> class.
        /// </summary>
        /// <param name="isChecked">If set to <c>true</c> is checked.</param>
        public WidgetCheckBox(bool isChecked = false)
            : this(ElementType, default(WidgetStyle), isChecked)
        {
        }

        public WidgetCheckBox(WidgetStyle style, bool isChecked = false)
            :this(ElementType, style, isChecked)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetCheckBox"/> class.
        /// </summary>
        /// <param name="style">Style.</param>
        /// <param name="isChecked">If set to <c>true</c> is checked.</param>
        protected WidgetCheckBox(string elementType, WidgetStyle style, bool isChecked)
            : base(elementType, style)
        {
            m_image = new WidgetImage(new WidgetStyle(ImageId));
            m_image.Parent = this;

            Selected = isChecked;

            //Resize(Size);
        }

        protected override void UpdateLayout()
        {
            base.UpdateLayout();

            if (m_image != null)
                m_image.Size = Size;
        }

        protected override void UpdateStyle()
        {
            base.UpdateStyle();

            if (m_image != null)
#pragma warning disable CS0618 // Type or member is obsolete
                m_image.ForceUpdateStyle();
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public override bool Update()
        {
            if (!base.Update())
                return false;

            if (m_image != null)
                m_image.Update();

            return true;
        }

        protected override void DrawContents()
        {
            base.DrawContents();

            if (m_image != null && (Checked || m_animating))
                m_image.Draw();
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
                    if (m_image != null)
                        m_image.Hovered = true;
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
                if (m_image != null)
                    m_image.Hovered = false;
                WindowController.Instance.OnTouch -= UnHoverTouch;
            }
            return false;
        }

        public void Press()
        {
            if (!Enabled)
                return;

            //GameSound.PlaySound(m_clickSound);

            if (OnCheckChanged != null && !OnCheckChanged(this))
                return;

            Checked = !Checked;
            
            AnimatePress();
        }

        protected virtual void AnimatePress()
        {
            m_animating = true;

            int animateTime = 100;
            if (Checked)
            {
                m_image.Opacity = 0.0f;
                m_image.Position = new Vector2(0, Size.Y / 4.0f);
                m_image.Move(Vector2.Zero, animateTime, AnimateFinished);
                m_image.FadeTo(1.0f, animateTime, null);
            } else
            {
                m_image.Opacity = 1.0f;
                m_image.Position = Vector2.Zero;
                m_image.Move(new Vector2(0, Size.Y / 4.0f), animateTime, AnimateFinished);
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

