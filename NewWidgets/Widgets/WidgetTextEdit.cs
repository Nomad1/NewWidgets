using System;
using System.Numerics;

using NewWidgets.UI;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    public class WidgetTextEdit : Widget, IFocusableWidget
    {
        private char m_cursorChar = '|';
        private bool m_focused;
        private int m_cursorPosition;

        private LabelObject m_label;
        private ISprite m_cursor;

        private string m_preffix;
        private string m_text;
        private Font m_font;
        private float m_fontSize;
        private int m_textColor;
        private int m_focusedColor;
        private char m_maskChar;
        
        private readonly Margin m_padding;
        private readonly int m_cursorColor;

        public event Action<WidgetTextEdit, string> OnFocusLost;
        public event Action<WidgetTextEdit, string> OnTextEntered;
        public event Action<WidgetTextEdit, string> OnTextChanged;

        private bool m_needLayout;

        private string m_focusImage;

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
        
        public string Text
        {
            get { return m_text; }
            set
            {
                if (m_text == value)
                    return;
                m_text = value;
                m_needLayout = true;
                m_cursorPosition = value.Length;
            }
        }

        public int TextColor
        {
            get { return m_textColor; }
            set
            {
                m_textColor = value;
                
                UpdateColor();
            }
        }
        
        public int FocusedColor
        {
            get { return m_focusedColor; }
            set
            {
                m_focusedColor = value;

                UpdateColor();
            }
        }

        public char MaskChar
        {
            get { return m_maskChar; }
            set { m_maskChar = value; m_needLayout = true; }
        }

        public bool IsFocused
        {
            get { return m_focused; }
        }

        public bool IsFocusable
        {
            get { return true; }
        }
        
        public string Preffix
        {
            get { return m_preffix; }
            set
            {
                string text = m_text;
                int shift = value.Length;
                if (text.StartsWith(m_preffix, StringComparison.CurrentCulture))
                {
                    text = text.Substring(m_preffix.Length);
                    shift -= m_preffix.Length;
                }
                
                m_preffix = value;
                m_cursorPosition += shift;
                m_text = m_preffix + text;
                m_needLayout = true;
            }
        }

        public WidgetTextEdit()
            : this(WidgetManager.DefaultTextEditStyle)
        {
        }

        public WidgetTextEdit(WidgetStyleSheet style)
            : base(style)
        {
            m_text = string.Empty;
            m_preffix = string.Empty;
            m_needLayout = true;
            
            m_font = style.Font;
            m_fontSize = style.FontSize;
            m_textColor = style.GetParameterColor("text_color", 0x0);
            m_focusedColor = style.GetParameterColor("focus_color", 0xffffff);
            m_cursorColor = style.GetParameterColor("cursor_color", 0x0);

            Size = style.Size;
            ClipContents = true;
            
            m_focusImage = style.GetParameter("focus_image");
            m_padding = style.Padding;
        }

        private void Relayout()
        {
            if (m_label == null)
            {
                m_label = new LabelObject(this, m_font, string.Empty,
                                          LabelAlign.Start, LabelAlign.Start);
                m_label.Position = m_padding.TopLeft;
            }

            m_label.Scale = m_fontSize;

            if (m_cursor == null)
            {
                m_cursor = m_font.GetSprites(m_cursorChar.ToString(), Vector2.Zero)[0];
                m_cursor.Color = m_cursorColor;
                m_cursor.Scale = m_fontSize;
                //m_cursor.Blink(2000);
                m_cursor.Transform.Parent = Transform;
            }

            Vector2 minSize = m_label.Size * m_fontSize + m_padding.Size;
            if (minSize.X < Size.X)
                minSize.X = Size.X;
            if (minSize.Y < Size.Y)
                minSize.Y = Size.Y;

            Size = minSize;

            m_needLayout = false;
            
            UpdateCursor(0);
            UpdateColor();
        }

        protected override void Resize(Vector2 size)
        {
            base.Resize(size);
        }

        public override bool Update()
        {
            if (!base.Update())
                return false;

            if (m_needLayout)
                Relayout();

            if (m_label != null)
                m_label.Update();

            if (m_cursor != null)
            {
                m_cursor.Alpha = (int)(Math.Sin(WindowController.Instance.GetTime() / 1000.0f) * 255 + 128.5f); // blinks every 2 seconds
                m_cursor.Update();
            }

            return true;
        }

        protected override void DrawContents(object canvas)
        {
            base.DrawContents(canvas);

            if (m_label != null)
                m_label.Draw(canvas);

            if (m_focused)
                m_cursor.Draw(canvas);
        }
        
        public override bool Key(SpecialKey key, bool up, char character)
        {
            if (!m_focused)
                return false;

            if (up && key == SpecialKey.Back)
            {
                //SetFocused(false);
                if (OnTextEntered != null)
                    OnTextEntered(this, string.Empty);
                return true;
            }

            if (up && (key == SpecialKey.Enter))
            {
                //SetFocused(false);
                if (OnTextEntered != null)
                    OnTextEntered(this, m_text);
                return true;
            }

            if (up && key == SpecialKey.Tab)
            {
                if (WidgetManager.FocusNext(this) && OnFocusLost != null)
                    OnFocusLost(this, m_text);
                
                return true;
            }

            if ((key == SpecialKey.Letter || key == SpecialKey.Paste) && m_font.HaveSymbol(character))
            {
                m_text = m_text.Insert(m_cursorPosition, character.ToString());
                
                if (OnTextChanged != null)
                    OnTextChanged(this, m_text);

                UpdateCursor(1);
                return true;
            }

            if (!up)
            {
                switch (key)
                {
                    case SpecialKey.Backspace:
                        if (m_cursorPosition > m_preffix.Length)
                        {
                            m_text = m_text.Substring(0, m_cursorPosition - 1) + m_text.Substring(m_cursorPosition);
                    
                            if (OnTextChanged != null)
                                OnTextChanged(this, m_text);

                            UpdateCursor(-1);
                        }
                        break;
                    case SpecialKey.Delete:
                        if (m_cursorPosition <= m_text.Length - 1)
                        {
                            m_text = m_text.Substring(0, m_cursorPosition) + m_text.Substring(m_cursorPosition + 1);
                    
                            if (OnTextChanged != null)
                                OnTextChanged(this, m_text);

                            UpdateCursor(0);
                        }
                        break;
                    case SpecialKey.Left:
                        if (m_cursorPosition > m_preffix.Length)
                            UpdateCursor(-1);
                        break;
                    case SpecialKey.Right:
                        if (m_cursorPosition <= m_text.Length - 1)
                            UpdateCursor(1);
                        break;
                }
            }
            
            switch(key)
            {
                case SpecialKey.Up:
                case SpecialKey.Down:
                    return false;
            }
            


            return true;
        }

        private void UpdateCursor(int change)
        {
            if (m_needLayout)
                return;
            
            if (m_focused)
            {
                m_cursorPosition += change;

                if (m_cursorPosition < m_preffix.Length)
                    m_cursorPosition = m_preffix.Length;

                if (m_cursorPosition > m_text.Length)
                    m_cursorPosition = m_text.Length;

                m_label.Text = MaskText(m_text.Substring(0, m_cursorPosition) + '\0' + m_text.Substring(m_cursorPosition));

                System.Drawing.RectangleF frame = m_label.GetCharFrame(m_cursorPosition);
                float nsize = Size.X / m_fontSize;

                if (m_label.Size.X > nsize)
                {
                    float from = -m_label.Position.X / m_fontSize;
                    float to = from + nsize;

                    if (frame.X > from && frame.X < to)
                    {
                    } else
                    {
                        if (frame.X > from)
                        {
                            float nx = (nsize - frame.X) * m_fontSize - m_padding.Width;
                            if (nx > m_padding.Left)
                                nx = m_padding.Left;

                            m_label.Position = new Vector2(nx, m_padding.Top);
                        } else
                            if (frame.X < to)
                        {
                            float nx = -frame.X * m_fontSize;
                            if (nx > m_padding.Left)
                                nx = m_padding.Left;

                            m_label.Position = new Vector2(nx, m_padding.Top);
                        }
                    }
                } else
                    m_label.Position = m_padding.TopLeft;

                m_cursor.Position = m_label.Position + new Vector2(frame.X * m_fontSize - 2, 0);

            } else
            {
                m_label.Text = MaskText(m_text);
                m_label.Position = m_padding.TopLeft;
            }
        }

        private string MaskText(string text)
        {
            if (m_maskChar == 0)
                return text;

            char [] result = text.ToCharArray();
            for (int i = 0; i < result.Length; i++)
                if (!m_focused || i != m_cursorPosition)
                    result[i] = '*';

            return new string(result);
        }

        public void SetFocused(bool value)
        {
            if (m_focused == value)
                return;

            m_focused = value;

            if (!string.IsNullOrEmpty(m_focusImage))
                BackgroundTexture = value ? m_focusImage : Style.BackgroundTexture;

            //UpdateColor();
            m_cursorPosition = m_text.Length;
            UpdateCursor(0);
            
            UpdateColor();
            
            WidgetManager.UpdateFocus(this, value);
            //WidgetManager.UpdateFocus(this, true);
        }
        
        private void UpdateColor()
        {
            if (m_label != null)
                m_label.Color = m_focused ? m_focusedColor : m_textColor;
        }

        public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            if (HitTest(x, y))
            {
                if (press)
                {
                    bool wasFocused = m_focused;

                    SetFocused(true);

                    Vector2 local = (this.Transform.GetClientPoint(new Vector2(x, y)) - m_label.Position) / m_fontSize;
                    ISprite[] sprites = m_label.InternalGetSprites();

                    if (sprites.Length > 0)
                    {
                        bool found = false;

                        if (!found && wasFocused)
                        for (int i = 0; i < sprites.Length; i++)
                        {
                            if (local.X < sprites[i].Position.X + sprites[i].Size.X / 2)
                            {
                                if (i < m_cursorPosition || wasFocused)
                                    UpdateCursor(i - m_cursorPosition);
                                else
                                    if (i >= m_cursorPosition)
                                        UpdateCursor(i - m_cursorPosition - 1);
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            int last = m_text.Length - 1;
                            UpdateCursor(last - m_cursorPosition + 1);
                        }
                    }
                }
                return true;
            }

            return base.Touch(x, y, press, unpress, pointer);
        }
        
        public override void Remove()
        {
            WidgetManager.UpdateFocus(this, false);
            base.Remove();
        }
    }
}

