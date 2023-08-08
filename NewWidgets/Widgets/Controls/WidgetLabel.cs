using System;
using System.Numerics;
using NewWidgets.UI;
using NewWidgets.UI.Styles;
using NewWidgets.Utility;

#if RUNMOBILE
using RunMobile.Utility;
#endif

namespace NewWidgets.Widgets
{
    public class WidgetLabel : Widget
    {
        public new const string ElementType = "label";

        // internal component for text label
        private LabelObject m_label;

        // cached label text
        private string m_text;

        // flag to indicate that animation is in progress
        private bool m_needAnimate;
        // animation type
        private bool m_needAnimateRandom;

        public WidgetAlign TextAlign
        {
            get { return GetProperty(WidgetParameterIndex.TextAlign, WidgetAlign.Left | WidgetAlign.Top); }
            set
            {
                SetProperty(WidgetParameterIndex.TextAlign, value);
                InvalidateLayout();
            }
        }

        /// <summary>
        /// Right now font size is a relative scale. Every change to scale factor results in label resize and realignment
        /// </summary>
        public float FontSize
        {
            get { return GetProperty(WidgetParameterIndex.FontSize, 1.0f); }
            set
            {
                SetProperty(WidgetParameterIndex.FontSize, value);
                InvalidateLayout();
            }
        }
      
        public Font Font
        {
            get { return GetProperty(WidgetParameterIndex.Font, WidgetManager.MainFont); }
            set
            {
                SetProperty(WidgetParameterIndex.Font, value);

                if (m_label != null)
                    m_label.Font = value;

                InvalidateLayout();
            }
        }

        /// <summary>
        /// Indicates whether we need to decode special symbols in the text
        /// </summary>
        public bool RichText
        {
            get { return GetProperty(WidgetParameterIndex.RichText, false); }
            set
            {
                SetProperty(WidgetParameterIndex.RichText, value);

                if (m_label != null)
                    m_label.RichText = value;

                InvalidateLayout();
            }
        }

        /// <summary>
        /// Text string value. If starts with @ it is considered a localized resource string
        /// </summary>
        public string Text
        {
            get { return m_text; }
            set
            {
                if (!string.IsNullOrEmpty(value) && value[0] == '@')
                    value = ResourceLoader.Instance.GetString(value);

                if (m_label != null)
                    m_label.Text = value;

                m_text = value;
                InvalidateLayout();
            }
        }

        public uint Color
        {
            get { return GetProperty(WidgetParameterIndex.TextColor, (uint)0xffffff); }
            set
            {
                SetProperty(WidgetParameterIndex.TextColor, value);

                if (m_label != null)
                    m_label.Color = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetLabel"/> class.
        /// </summary>
        /// <param name="style">Style.</param>
        public WidgetLabel(WidgetStyle style, string text = "")
           : this(ElementType, style, text)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetLabel"/> class.
        /// </summary>
        /// <param name="text">Text.</param>
        public WidgetLabel(string text = "")
            : this(ElementType, default(WidgetStyle), text)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetLabel"/> class.
        /// </summary>
        /// <param name="style">Style.</param>
        /// <param name="text">Text.</param>
        protected WidgetLabel(string elementType, WidgetStyle style, string text)
           : base(elementType, style)
        {
            Text = text;
        }

        protected override void UpdateLayout()
        {
            // If label is not yet created - create it. Otherwise all the changes are already it
            if (m_label == null)
                m_label = new LabelObject(this, Font, Text, RichText);

            m_label.Color = Color;
            m_label.Scale = FontSize;

            Vector2 labelSize = m_label.Size * m_label.Scale;
            Vector2 newSize = Size;

            if (newSize.X <= 0)
                newSize.X = labelSize.X;
            if (newSize.Y <= 0)
                newSize.Y = labelSize.Y;

            Size = newSize;

            // TODO: here we're autosizing the widget to fit the label, but there whould be an option to choose between sizing and overflow modes

            // very simple alignment

            float x = 0;

            if ((TextAlign & WidgetAlign.HorizontalCenter) == WidgetAlign.HorizontalCenter)
                x = (Size.X - labelSize.X) / 2;
            else if ((TextAlign & WidgetAlign.Right) == WidgetAlign.Right)
                x = Size.X - labelSize.X;

            float y = 0;

            if ((TextAlign & WidgetAlign.VerticalCenter) == WidgetAlign.VerticalCenter)
                y = (Size.Y - labelSize.Y) / 2;
            else if ((TextAlign & WidgetAlign.Bottom) == WidgetAlign.Bottom)
                y = Size.Y - labelSize.Y;

            m_label.Position = new Vector2(x, y);

            base.UpdateLayout();
        }

        public override bool Update()
        {
            if (!base.Update())
                return false;

            if (m_label != null)
            {
                m_label.Update();
                m_label.Opacity = OpacityValue;
            }

            if (m_needAnimate)
                DoAnimateAppear(m_needAnimateRandom);

            return true;
        }

        protected override void DrawContents()
        {
            base.DrawContents();

            if (m_label != null)
                m_label.Draw();
        }

        public void AnimateAppear(bool random)
        {
            m_needAnimate = true;
            m_needAnimateRandom = random;
        }
        
        public void AnimateDisappear()
        {
            m_needAnimate = false;
            for (int i = 0; i < m_label.InternalGetSprites().Length; i++)
            {
                ISprite sprite = m_label.InternalGetSprites()[i];
                sprite.Alpha = 255;
                AnimationManager.Instance.RemoveAnimation(this, (AnimationKind)((int)AnimationKind.Custom + i));
            }
            
            FadeTo(0.0f, 100, null);
        }
        
        protected virtual void DoAnimateAppear(bool isRandom)
        {
            m_needAnimate = false;

            WidgetOverflow oldClip = this.Overflow;
            
            this.Overflow = WidgetOverflow.Hidden;
            
            Vector2 targetPosition = m_label.Position;
            m_label.Position = targetPosition + new Vector2(0, 10);
            m_label.Move(targetPosition, 100, delegate {
                this.Overflow = oldClip;
                m_label.Position = targetPosition;
            });

            Opacity = 1.0f;

            for (int i = 0; i < m_label.InternalGetSprites().Length; i++)
            {
                ISprite sprite = m_label.InternalGetSprites()[i];
                sprite.Alpha = 0;

                // Different letters appear with random delay vs constant fade-in

                // TODO: Unreadable, refactor
                int time = isRandom ? 100 + MathHelper.GetRandomInt(0, 200) : 100 + i * 500 / m_label.InternalGetSprites().Length;

                AnimationManager.Instance.StartAnimation(this, (AnimationKind)((int)AnimationKind.Custom + i), 0, 255, time,
                delegate (float x, int from, int to)
                {
                    sprite.Alpha = (byte)MathHelper.LinearInterpolationInt(x, from, to);
                },
                null);
            }
        }
    }
}

