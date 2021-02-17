using System;
using System.Numerics;

#if RUNMOBILE
using RunMobile.Utility;
#else
using NewWidgets.Utility;
using System.Drawing;
#endif

namespace NewWidgets.UI
{
    /// <summary>
    /// Complex label object having an array of daughter sprites based on some text
    /// </summary>
    public class LabelObject : WindowObject
    {
        private readonly Font m_font;
        private readonly Vector2 m_pivot;

        private int m_color;
        private float m_alpha;
        private ISprite[] m_sprites;
        private string m_text;
        private bool m_richText;

        public Font Font
        {
            get { return m_font; }
        }

        public float Alpha
        {
            get { return m_alpha; }
            set
            {
                m_alpha = value;
                int ialpha = MathHelper.Clamp((int)(m_alpha * 255), 0, 255);

                if (m_sprites != null)
                    foreach (ISprite sprite in m_sprites)
                        sprite.Alpha = (byte)ialpha;
            }
        }

        public int Color
        {
            get { return m_color; }
            set
            {
                m_color = value;
                if (m_sprites != null)
                    foreach (ISprite sprite in m_sprites)
                        sprite.Color = m_color;
            }
        }

        public string Text
        {
            get { return m_text; }
            set { Init(value, m_richText); }
        }

        public bool RichText
        {
            get { return m_richText; }
            set { Init(m_text, value); }
        }

        public LabelObject(WindowObject parent, Font font, string text, LabelAlign horizontalAlign, LabelAlign verticalAlign, bool richText = false)
            : base(parent)
        {
            m_color = 0xffffff;
            m_alpha = 1.0f;
            m_font = font;

            float x = 1;
            float y = 1;

            switch (horizontalAlign)
            {
                case LabelAlign.Start:
                    x = 0;
                    break;
                case LabelAlign.Center:
                    x = 0.5f;
                    break;
            }
            switch (verticalAlign)
            {
                case LabelAlign.Start:
                    y = 0;
                    break;
                case LabelAlign.Center:
                    y = 0.5f;
                    break;
            }

            m_pivot = new Vector2(x, y);

            Init(text, richText);
        }

        internal void SetColors(TextSpan[] colors)
        {
            if (colors != null && colors.Length < m_sprites.Length)
                throw new ArgumentException("Too few colors in array!");

            int ialpha = MathHelper.Clamp((int)(m_alpha * 255), 0, 255);

            for (int i = 0; i < m_sprites.Length; i++)
            {
                ISprite sprite = m_sprites[i];

                if (colors != null && colors[i] != null)
                {
                    if (colors[i] is IconSpan)
                    {
                        IconSpan icon = (IconSpan)colors[i];
                        ISprite iconSprite = WindowController.Instance.CreateSprite(icon.Icon, Vector2.Zero);

                        if (iconSprite != null) // consume the error cause there is no place for log or message
                        {
                            iconSprite.Position = new Vector2(sprite.Position.X, sprite.Position.Y + m_font.Baseline - icon.Height / 2);

                            iconSprite.Transform.Scale = new Vector3(icon.Width / iconSprite.Size.X, icon.Height / iconSprite.Size.Y, 1.0f);

                            m_sprites[i] = iconSprite;
                            sprite = m_sprites[i];
                        }
                    }

                    sprite.Color = colors[i].Color;
                }
                else
                    sprite.Color = m_color;

                sprite.Alpha = (byte)ialpha;
                sprite.Transform.Parent = Transform;
            }
        }

        public static string ParseRichText(string text, int defaultColor, out TextSpan[] colors, int spaceWidth)
        {
            colors = new TextSpan[text.Length];
            char[] chars = new char[text.Length];

            int length = 0;
            TextSpan currentColor = new TextSpan(defaultColor);

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '|' && i < text.Length - 1)
                {
                    char code = text[i + 1];

                    switch (code)
                    {
                        case 'c': // color
                            if (i < text.Length - 9)
                            {
                                string colorString = text.Substring(i + 2, 6);

                                int color;
                                if (int.TryParse(colorString, System.Globalization.NumberStyles.HexNumber, null, out color))
                                {
                                    currentColor = new TextSpan(color);
                                    i += 7;
                                    continue;
                                }
                            }
                            break;
                        case 'r': // restore color
                            currentColor = new TextSpan(defaultColor);
                            i++;
                            continue;
                        case '|': // symbol |
                            i++;
                            chars[length] = '|';
                            colors[length] = currentColor;
                            length++;
                            continue;
                        case 'n': // new line
                            i++;
                            chars[length] = '\n';
                            colors[length] = currentColor;
                            length++;
                            continue;
                        case 't': // image
                            if (i < text.Length - 4)
                            {
                                // we wait for string alike
                                // |ticon_money:32:32:ffbbee|t

                                int nextT = text.IndexOf("|t", i + 1);
                                if (nextT != -1)
                                {
                                    string iconText = text.Substring(i + 2, nextT - i - 2);
                                    string[] iconData = iconText.Split(':');
                                    string icon = iconData[0];
                                    int width;
                                    int height;
                                    int color;

                                    // ignore the errors if any

                                    if (iconData.Length > 2 && int.TryParse(iconData[1], out width) && int.TryParse(iconData[2], out height))
                                    {
                                        if (iconData.Length > 3 && int.TryParse(iconData[3], System.Globalization.NumberStyles.HexNumber, null, out color))
                                        {
                                        }
                                        else
                                            color = 0xffffff; // defaults to white color

                                        // use spaces to preserve space for image
                                        TextSpan span = new IconSpan(color, icon, width, height);

                                        int spaces = (int)Math.Ceiling(width / (float)spaceWidth);

                                        colors[length] = span;

                                        for (int k = 0; k < spaces; k++)
                                        {
                                            chars[length] = '\u00a0'; // &nbsp; symbol will default to space but avoid line breaks
                                            length++;
                                        }

                                        i = nextT + 1;

                                        continue;
                                    }
                                }
                            }

                            break;
                    }
                }

                chars[length] = text[i];
                colors[length] = currentColor;
                length++;
            }

            return new string(chars, 0, length);
        }

        /// <summary>
        /// Init the specified text and richText. This method also works as initializer for m_text and m_richText fields.
        /// Try to avoid setting them without this call
        /// </summary>
        /// <param name="text">Text.</param>
        /// <param name="richText">If set to <c>true</c> rich text.</param>
        private void Init(string text, bool richText)
        {
            if (text == m_text && m_richText == richText)
                return;

            m_text = text;
            m_richText = richText;

            TextSpan[] colors = null;

            if (richText && text.Contains("|"))
                text = ParseRichText(text, m_color, out colors, (int)(m_font.SpaceWidth + m_font.Spacing));

            this.Size = m_font.MeasureString(text);

            Vector2 position = m_pivot * Size * -1;

            if (richText)
                m_sprites = null;

            m_font.GetSprites(text, position, ref m_sprites);

            SetColors(colors);
        }

        public override bool Update()
        {
            if (!base.Update())
                return false;

            if (Visible && m_sprites != null)
            {
                foreach (ISprite sprite in m_sprites)
                    sprite.Update();
            }

            return true;
        }

        public override void Draw(object canvas)
        {
            if (!Visible)
                return;

            base.Draw(canvas);

            if (m_sprites != null)
            {
                foreach (ISprite sprite in m_sprites)
                    sprite.Draw(canvas);
            }
        }

        public override void Remove()
        {
            base.Remove();

            m_sprites = null;
        }

        public RectangleF GetCharFrame(int index)
        {
            if (m_sprites == null || m_sprites.Length == 0)
                return RectangleF.Empty;

            ISprite sprite = m_sprites[index];
            return new RectangleF(sprite.Position.X, sprite.Position.Y, sprite.FrameSize.X + m_font.Spacing, sprite.FrameSize.Y);
        }

        internal ISprite[] InternalGetSprites()
        {
            return m_sprites;
        }

        #region Helper classes

        public class TextSpan
        {
            public readonly int Color;

            public TextSpan(int color)
            {
                Color = color;
            }
        }

        public class IconSpan : TextSpan
        {
            public readonly string Icon;
            public readonly int Width;
            public readonly int Height;

            public IconSpan(int color, string icon, int width, int height)
                : base(color)
            {
                Icon = icon;
                Width = width;
                Height = height;
            }
        }

        #endregion
    }
}

