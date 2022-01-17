using System.Collections.Generic;
using System.Numerics;

namespace NewWidgets.UI
{
    /// <summary>
    /// Low-level sprite based font
    /// </summary>
    public class Font
    {
        private readonly Dictionary<char, Glyph> m_glyphs;
        private readonly Glyph m_spaceGlyph;
        private readonly float m_spacing;
        private readonly int m_leading;
        private readonly ISprite m_fontSprite;
        private readonly int m_height;
        private readonly int m_baseline;
        private readonly int m_shift;
        private readonly string m_fontName;

        public int Height
        {
            get { return m_height; }
        }
        
        public float Spacing
        {
            get { return m_spacing; }
        }

        public int Leading
        {
            get { return m_leading; }
        }

        public int SpaceWidth
        {
            get { return m_spaceGlyph.Width; }
        }

        public int Shift
        {
            get { return m_shift; }
        }

        public int Baseline
        {
            get { return m_baseline; }
        }

        public ISprite Sprite
        {
            get { return m_fontSprite; }
        }

        public Font(string name, string font, float spacing, int leading, int baseline, int shift)
            : this(WindowController.Instance.CreateSprite(font), spacing, leading, baseline, shift)
        {
            m_fontName = name;
        }

        public Font(ISprite fontSprite, float spacing, int leading, int baseline, int shift)
        {
            m_fontName = fontSprite.ToString();

            m_baseline = baseline;
            m_shift = shift;
            m_fontSprite = fontSprite;
            m_spacing = spacing;
            m_leading = leading;

            m_glyphs = new Dictionary<char, Glyph>();
            int maxHeight = 0;

            // Here goes hacky magic: we iterate all the frames of `fontSprite` object and set it's Frame property
            // to retrieve FrameTag and FrameSize values. That allows us to think of font as of an animation with lot of frames
            // but that is the only thing in whole UI that requires frames and FrameTag.

            for (int i = 0; i < fontSprite.FrameCount; i++)
            {
                fontSprite.Frame = i;

                if ((int)fontSprite.FrameSize.Y > maxHeight)
                    maxHeight = (int)fontSprite.FrameSize.Y;

                m_glyphs[(char)fontSprite.FrameTag] = new Glyph((int)fontSprite.FrameSize.X, (int)fontSprite.FrameSize.Y, (char)fontSprite.FrameTag, i);
            }

            m_spaceGlyph = m_glyphs[' '];
            m_height = maxHeight;
        }

        public ISprite[] GetSprites(string text, Vector2 position)
        {
            ISprite[] result = new ISprite[text.Length];

            for (int i = 0; i < text.Length; i++)
            {
                Glyph glyph;
                if (!m_glyphs.TryGetValue(text[i], out glyph))
                    glyph = m_spaceGlyph;

                ISprite sprite = WindowController.Instance.CloneSprite(m_fontSprite);
                sprite.Transform.FlatPosition = position + new Vector2(m_leading, m_shift);
                sprite.Frame = glyph.Frame;
                result[i] = sprite;

                position.X += glyph.Width + m_spacing;
            }

            return result;
        }

        public void GetSprites(string text, Vector2 position, ref ISprite[] result)
        {
            if (result == null || result.Length != text.Length)
            {
                result = new ISprite[text.Length];
                // TODO: release old sprites?
            }

            for (int i = 0; i < text.Length; i++)
            {
                Glyph glyph;
                if (!m_glyphs.TryGetValue(text[i], out glyph))
                    glyph = m_spaceGlyph;

                if (result[i] != null)
                {
                    result[i].Transform.FlatPosition = position + new Vector2(m_leading, 0);
                    result[i].Frame = glyph.Frame;
                }
                else
                {
                    ISprite sprite = WindowController.Instance.CloneSprite(m_fontSprite);
                    sprite.Transform.FlatPosition = position + new Vector2(m_leading, m_shift);
                    sprite.Frame = glyph.Frame;
                    result[i] = sprite;
                }

                position.X += glyph.Width + m_spacing;
            }
        }

        public Vector2 MeasureString(string text)
        {
            Vector2 result = new Vector2(m_leading, m_height);
            for (int i = 0; i < text.Length; i++)
            {
                Glyph glyph;
                if (!m_glyphs.TryGetValue(text[i], out glyph))
                    glyph = m_spaceGlyph;

                result.X += glyph.Width + m_spacing;
            }

            if (text.Length > 0)
                result.X -= m_spacing;

            return result;
        }
        
        public float [] MeasureChars(string text)
        {
            float[] result = new float[text.Length];
            
            for (int i = 0; i < text.Length; i++)
            {
                Glyph glyph;
                if (!m_glyphs.TryGetValue(text[i], out glyph))
                    glyph = m_spaceGlyph;

                result[i] = glyph.Width + m_spacing;
            }

            return result;
        }

        public bool HaveSymbol(char symbol)
        {
            return m_glyphs.ContainsKey(symbol);
        }

        public override string ToString()
        {
            return m_fontName;
        }

        private struct Glyph
        {
            public readonly int Width;
            public readonly int Height;
            public readonly char Value;
            public readonly int Frame;

            public Glyph(int width, int height, char value, int frame)
            {
                Width = width;
                Height = height;
                Value = value;
                Frame = frame;
            }
        }
    }
}

