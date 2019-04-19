using System;
using System.Numerics;
using NewWidgets.UI;
using NewWidgets.Widgets.Styles;

#if RUNMOBILE
using RunMobile.Utility;
#else
using NewWidgets.Utility;
#endif

namespace NewWidgets.Widgets
{
    public class WidgetLabel : Widget
    {
        public static readonly new WidgetStyleReference<WidgetTextStyleSheet> DefaultStyle = new WidgetStyleReference<WidgetTextStyleSheet>("default_label");

        private LabelObject m_label;

        private string m_text;

        private bool m_needLayout;
        private bool m_needAnimate;
        private bool m_needAnimateRandom;
        
        private bool m_richText;

        public new WidgetTextStyleSheet Style
        {
            get { return (WidgetTextStyleSheet)base.Style; }
        }

        protected new WidgetTextStyleSheet WritableStyle
        {
            get { return (WidgetTextStyleSheet)base.WritableStyle; }
        }

        public Font Font
        {
            get { return Style.Font; }
            set { WritableStyle.Font = value; m_needLayout = true; }
        }

        public float FontSize
        {
            get { return Style.FontSize; }
            set { WritableStyle.FontSize = value; m_needLayout = true; }
        }

        public WidgetAlign TextAlign
        {
            get { return Style.TextAlign; }
            set { WritableStyle.TextAlign = value; m_needLayout = true; }
        }

        public bool RichText
        {
            get { return m_richText; }
            set
            {
                m_richText = value;
                if (m_label != null)
                    m_label.RichText = value;
                m_needLayout = true;
            }
        }

        public string Text
        {
            get { return m_text; }
            set
            {
                if (!string.IsNullOrEmpty(value) && value[0] == '@')
                    value = ResourceLoader.Instance.GetString(value);
                
                if (m_text == value)
                    return;
                
                m_text = value;
                m_needLayout = true;
            }
        }

        public int Color
        {
            get { return Style.TextColor; }
            set
            {
                WritableStyle.TextColor = value;
                if (m_label != null) // try to avoid settings m_needLayout
                    m_label.Color = value;
            }
        }

        public override float Alpha
        {
            get { return base.Alpha; }
            set
            {
                base.Alpha = value;
                if (m_label != null) // try to avoid settings m_needLayout
                    m_label.Alpha = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetLabel"/> class.
        /// </summary>
        /// <param name="text">Text.</param>
        public WidgetLabel(string text = "")
            : this(null, text)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetLabel"/> class.
        /// </summary>
        /// <param name="style">Style.</param>
        /// <param name="text">Text.</param>
        public WidgetLabel(WidgetTextStyleSheet style, string text)
            : base(style ?? DefaultStyle)
        {
            m_needLayout = true;
            m_text = text;
            m_richText = true;
        }

        public void Relayout()
        {
            if (m_label == null)
            {
                m_label = new LabelObject(this, Font, string.Empty,
                    LabelAlign.Start, LabelAlign.Start, m_richText);
            }
            
            m_label.Color = Color;
            m_label.Alpha = Alpha;
            m_label.Scale = FontSize;
            
            m_label.Text = m_text;

            Vector2 minSize = m_label.Size * FontSize;
            if (minSize.X < Size.X)
                minSize.X = Size.X;
            if (minSize.Y < Size.Y)
                minSize.Y = Size.Y;

            Size = minSize;
            
            RelayoutText();

            m_needLayout = false;
        }

        private void RelayoutText()
        {
            Vector2 labelSize = m_label.Size * FontSize;

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
        }

        protected override void Resize(Vector2 size)
        {
            base.Resize(size);

            m_needLayout = true;
        }

        public override bool Update()
        {
            if (!base.Update())
                return false;

            if (m_needLayout)
                Relayout();

            if (m_label != null)
                m_label.Update();

            if (m_needAnimate)
            {
                DoAnimateAppear(m_needAnimateRandom);
            }

            return true;
        }

        protected override void DrawContents(object canvas)
        {
            base.DrawContents(canvas);

            if (m_label != null)
                m_label.Draw(canvas);
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
                Animator.RemoveAnimation(this, (AnimationKind)((int)AnimationKind.Custom + i));
            }
            
            FadeTo(0.0f, 100, null);
        }
        
        protected virtual void DoAnimateAppear(bool isRandom)
        {
            m_needAnimate = false;

            bool oldClip = this.ClipContents;
            
            this.ClipContents = true;
            
            Vector2 targetPosition = m_label.Position;
            m_label.Position = targetPosition + new Vector2(0, 10);
            m_label.Move(targetPosition, 100, delegate {
                this.ClipContents = oldClip;
                m_label.Position = targetPosition;
            });

            Alpha = 1.0f;

            Random random = new Random();

            for (int i = 0; i < m_label.InternalGetSprites().Length; i++)
            {
                ISprite sprite = m_label.InternalGetSprites()[i];
                sprite.Alpha = 0;

                // Different letters appear with random delay vs constant fade in

                // TODO: Unreadable, refactor
                int time = isRandom ? 100 + random.Next() % 200 : 100 + i * 500 / m_label.InternalGetSprites().Length;

                Animator.StartAnimation(this, (AnimationKind)((int)AnimationKind.Custom + i), 0, 255, time,
                delegate (float x, int from, int to)
                {
                    sprite.Alpha = MathHelper.LinearInterpolationInt(x, (int)from, (int)to);
                },
                null);
            }
        }
    }
}

