using System;
using System.Numerics;
using NewWidgets.UI;

#if RUNMOBILE
using RunMobile.Utility;
#else
using NewWidgets.Utility;
#endif

namespace NewWidgets.Widgets
{
    public class WidgetLabel : Widget
    {
        private LabelObject m_label;

        private string m_text;
        private Font m_font;
        private float m_fontSize;
        private WidgetAlign m_textAlign;

        private bool m_needLayout;
        private bool m_needAnimate;
        private bool m_needAnimateRandom;
        
        private bool m_richText;
        
        public Font Font
        {
            get { return m_font; }
            set { m_font = value; m_needLayout = true; }
        }

        public float FontSize
        {
            get { return m_fontSize; }
            set { m_fontSize = value; m_needLayout = true; }
        }

        public bool RichText
        {
            get { return m_richText; }
            set { m_richText = value; if (m_label != null) m_label.RichText = value; m_needLayout = true; }
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

        public WidgetAlign TextAlign
        {
            get { return m_textAlign; }
            set { m_textAlign = value; m_needLayout = true; }
        }

        public override int Color
        {
            get { return base.Color; }
            set { base.Color = value; if (m_label != null) m_label.Color = value; }
        }

        public override float Alpha
        {
            get { return base.Alpha; }
            set
            {
                base.Alpha = value; if (m_label != null) m_label.Alpha = value;
            }
        }
        
        public WidgetLabel()
            : this(WidgetManager.DefaultLabelStyle, string.Empty)
        {
        }

        public WidgetLabel(string text)
            : this(WidgetManager.DefaultLabelStyle, text)
        {
        }
        
        public WidgetLabel(WidgetStyleSheet style, string text)
            : base(style, true)
        {
            m_needLayout = true;
            m_text = text;

            m_textAlign = WidgetAlign.Left | WidgetAlign.Top;
            
            m_font = style.Font;
            m_fontSize = style.FontSize;
            m_richText = true;
        }

        public void Relayout()
        {
            if (m_label == null)
            {
                m_label = new LabelObject(this, m_font, string.Empty,
                    LabelAlign.Start, LabelAlign.Start, m_richText);
            }
            
            m_label.Color = base.Color;
            m_label.Alpha = Alpha;
            m_label.Scale = m_fontSize;
            
            m_label.Text = m_text;

            Vector2 minSize = m_label.Size * m_fontSize;
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
            Vector2 labelSize = m_label.Size * m_fontSize;

            float x = 0;

            if ((m_textAlign & WidgetAlign.HorizontalCenter) == WidgetAlign.HorizontalCenter)
                x = (Size.X - labelSize.X) / 2;
            else if ((m_textAlign & WidgetAlign.Right) == WidgetAlign.Right)
                x = Size.X - labelSize.X;

            float y = 0;

            if ((m_textAlign & WidgetAlign.VerticalCenter) == WidgetAlign.VerticalCenter)
                y = (Size.Y - labelSize.Y) / 2;
            else if ((m_textAlign & WidgetAlign.Bottom) == WidgetAlign.Bottom)
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

