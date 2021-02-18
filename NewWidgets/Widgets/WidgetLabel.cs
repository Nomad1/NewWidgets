using System.Numerics;
using NewWidgets.UI;
using NewWidgets.Utility;

#if RUNMOBILE
using RunMobile.Utility;
#endif

namespace NewWidgets.Widgets
{
    public class WidgetLabel : Widget
    {
        public static readonly new WidgetStyleSheet DefaultStyle = WidgetManager.GetStyle("default_label", true);

        private LabelObject m_label;

        private string m_text;

        private bool m_needLayout; // flag to indicate that inner label size/opacity/formatting has changed
        private bool m_needRecreate; // flag to indicate that we need a new inner label
        private bool m_needAnimate; // flag to indicate that animation is in progress
        private bool m_needAnimateRandom; // animation type

        private int m_color = 0xffffff;
        
        public Font Font
        {
            get { return GetProperty(WidgetParameterIndex.Font, WidgetManager.MainFont); }
            set
            {
                SetProperty(WidgetParameterIndex.Font, value);
                InivalidateLayout();
                m_needRecreate = true; // there is no way to change the font without label recreation
            }
        }

        public float FontSize
        {
            get { return GetProperty(WidgetParameterIndex.FontSize, 1.0f); }
            set
            {
                SetProperty(WidgetParameterIndex.FontSize, value);
                InivalidateLayout();
            }
        }

        public WidgetAlign TextAlign
        {
            get { return GetProperty(WidgetParameterIndex.TextAlign, WidgetAlign.Left | WidgetAlign.Top); }
            set
            {
                SetProperty(WidgetParameterIndex.TextAlign, value);
                InivalidateLayout();
                m_needRecreate = true; // there is no way to change text alignment without label recreation
            }
        }

        public bool RichText
        {
            get { return GetProperty(WidgetParameterIndex.RichText, false); }
            set
            {
                SetProperty(WidgetParameterIndex.RichText, value);

                if (m_label != null) // try to avoid settings m_needLayout
                    m_label.RichText = value;

                InivalidateLayout();
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
                InivalidateLayout();
            }
        }

        public int Color
        {
            get { return GetProperty(WidgetParameterIndex.TextColor, 0xffffff); }
            set
            {
                if (m_color == value)
                    return;

                SetProperty(WidgetParameterIndex.TextColor, value);

                if (m_label != null) // try to avoid settings m_needLayout
                    m_label.Color = value;

                m_color = value;
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
        public WidgetLabel(string text)
            : this(default(WidgetStyleSheet), text)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetLabel"/> class.
        /// </summary>
        /// <param name="style">Style.</param>
        /// <param name="text">Text.</param>
        public WidgetLabel(WidgetStyleSheet style = default(WidgetStyleSheet), string text = "")
           : base(style.IsEmpty ? DefaultStyle : style)
        {
            Text = text;
        }

        internal WidgetLabel(WidgetStyleSheet [] styles, string text = "")
           : base(styles)
        {
            Text = text;
        }

        private void InivalidateLayout()
        {
            m_needLayout = true;
        }

        protected override void Resize(Vector2 size)
        {
            base.Resize(size);
            InivalidateLayout();
        }

        public override bool SwitchStyle(WidgetStyleType styleType)
        {
            if (base.SwitchStyle(styleType))
            {
                InivalidateLayout();
                return true;
            }
            return false;
        }

        public void Relayout()
        {
            if (m_label != null && m_needRecreate)
            {
                m_label.Remove();
                m_label = null;
            }

            if (m_label == null)
                m_label = new LabelObject(this, Font, string.Empty, LabelAlign.Start, LabelAlign.Start, RichText);
            
            m_label.Color = Color;
            m_label.Alpha = Alpha;
            m_label.Scale = FontSize;

            if (!string.IsNullOrEmpty(m_text))
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

        public override bool Update()
        {
            if (!base.Update())
                return false;

            if (m_needLayout)
                Relayout();

            if (m_label != null)
                m_label.Update();

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

            bool oldClip = this.ClipContents;
            
            this.ClipContents = true;
            
            Vector2 targetPosition = m_label.Position;
            m_label.Position = targetPosition + new Vector2(0, 10);
            m_label.Move(targetPosition, 100, delegate {
                this.ClipContents = oldClip;
                m_label.Position = targetPosition;
            });

            Alpha = 1.0f;

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

