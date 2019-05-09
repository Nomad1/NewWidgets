using System;
using System.Numerics;

using NewWidgets.UI;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    /// <summary>
    /// Single-line text edit with support for masking and non-editable preffix
    /// </summary>
    public class WidgetTextEdit : WidgetBackground, IFocusableWidget
    {
        public static readonly new WidgetStyleSheet DefaultStyle = WidgetManager.GetStyle("default_textedit", true);

        private int m_cursorPosition;

        private LabelObject m_label;
        private ImageObject m_cursor;

        private string m_preffix;
        private string m_text;

        private bool m_needLayout;

        public event Action<WidgetTextEdit, string> OnFocusLost;
        public event Action<WidgetTextEdit, string> OnTextEntered;
        public event Action<WidgetTextEdit, string> OnTextChanged;
        /// <summary>
        /// Occurs when key is pressed and nex text is to be added. First parameter is source string second in inout string
        /// returns true if string is valid and false if not
        /// </summary>
        public event Func<string, string, bool> OnValidateInput;

        public Font Font
        {
            get { return GetProperty(WidgetParameterIndex.Font, WidgetManager.MainFont); }
            set { SetProperty(WidgetParameterIndex.Font, value); InivalidateLayout(); }
        }

        public float FontSize
        {
            get { return GetProperty(WidgetParameterIndex.FontSize, 1.0f); }
            set { SetProperty(WidgetParameterIndex.FontSize, value); InivalidateLayout(); }
        }

        public WidgetAlign TextAlign
        {
            get { return GetProperty(WidgetParameterIndex.TextAlign, WidgetAlign.Left | WidgetAlign.Top); }
            set { SetProperty(WidgetParameterIndex.TextAlign, value); InivalidateLayout(); }
        }

        public Margin TextPadding
        {
            get { return GetProperty(WidgetParameterIndex.TextPadding, new Margin(0)); }
            set { SetProperty(WidgetParameterIndex.TextPadding, value); InivalidateLayout(); }
        }

        public int TextColor
        {
            get { return GetProperty(WidgetParameterIndex.TextColor, 0xffffff); }
            set
            {
                SetProperty(WidgetParameterIndex.TextColor, value);

                if (m_label != null) // try to avoid InivalidateLayout
                    m_label.Color = value;
            }
        }

        public int CursorColor
        {
            get { return GetProperty(WidgetParameterIndex.CursorColor, 0xffffff); }
            set
            {
                SetProperty(WidgetParameterIndex.CursorColor, value);

                if (m_cursor != null) // try to avoid InivalidateLayout
                    m_cursor.Sprite.Color = value;
            }
        }

        public string CursorChar
        {
            get { return GetProperty(WidgetParameterIndex.CursorChar, "|"); }
            set
            {
                SetProperty(WidgetParameterIndex.CursorChar, value);
                if (m_cursor != null)
                {
                    m_cursor.Remove();
                    m_cursor = null;
                }
                InivalidateLayout();
            }
        }

        public string Text
        {
            get { return m_text; }
            set
            {
                if (m_text == value)
                    return;
                m_text = value;
                m_cursorPosition = value.Length;
                InivalidateLayout();
            }
        }

        public string MaskChar
        {
            get { return GetProperty(WidgetParameterIndex.MaskChar, ""); }
            set
            {
                SetProperty(WidgetParameterIndex.MaskChar, value);
                InivalidateLayout();
            }
        }

        public bool IsFocused
        {
            get { return Selected; }
            private set { Selected = value; }
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
                InivalidateLayout();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetTextEdit"/> class.
        /// </summary>
        /// <param name="style">Style.</param>
        public WidgetTextEdit(WidgetStyleSheet style = default(WidgetStyleSheet))
            : base(style.IsEmpty ? DefaultStyle : style)
        {
            m_text = string.Empty;
            m_preffix = string.Empty;
            m_needLayout = true;
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

        private void Relayout()
        {
            if (m_label == null)
            {
                m_label = new LabelObject(this, Font, MaskText(m_text, MaskChar), LabelAlign.Start, LabelAlign.Start);
                m_label.Position = TextPadding.TopLeft;
            }
            else
                m_label.Text = MaskText(m_text, MaskChar);

            m_label.Scale = FontSize;

            if (m_cursor == null)
            {
                m_cursor = new ImageObject(this, Font.GetSprites(CursorChar, Vector2.Zero)[0]);
                m_cursor.Sprite.Color = CursorColor;
                m_cursor.Scale = FontSize;
                m_cursor.Transform.Parent = Transform;
            }

            // Nomad: I'm not sure it is normal to resize text edit depending on it's content. It looks like leftovers from WidgetLabel copy-paste
            Vector2 minSize = m_label.Size * FontSize + TextPadding.Size;
            if (minSize.X < Size.X)
                minSize.X = Size.X;
            if (minSize.Y < Size.Y)
                minSize.Y = Size.Y;

            Size = minSize;

            m_needLayout = false;
            
            UpdateCursor(0);
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
                m_cursor.Sprite.Alpha = (int)(Math.Sin(WindowController.Instance.GetTime() / 1000.0f) * 64 + 191); // blinks every 2 seconds from 127 to 255
                m_cursor.Update();
            }

            return true;
        }

        protected override void DrawContents(object canvas)
        {
            base.DrawContents(canvas);

            if (m_label != null)
                m_label.Draw(canvas);

            if (IsFocused)
                m_cursor.Draw(canvas);
        }
        
        public override bool Key(SpecialKey key, bool up, char character)
        {
            if (!IsFocused)
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


            if ((key == SpecialKey.Letter || key == SpecialKey.Paste) && Font.HaveSymbol(character))
            {
                string toAdd = character.ToString();

                // Input string validation. Only called on paste and input
                if (OnValidateInput != null && !OnValidateInput(m_text, toAdd))
                    return true;

                if (m_cursorPosition == m_text.Length)
                    m_text += toAdd;
                else
                    m_text = m_text.Insert(m_cursorPosition, toAdd);

                if (OnTextChanged != null)
                    OnTextChanged(this, m_text);

                m_cursorPosition += toAdd.Length;
                Relayout();
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

                            m_cursorPosition--;
                            Relayout();
                        }
                        break;
                    case SpecialKey.Delete:
                        if (m_cursorPosition <= m_text.Length - 1)
                        {
                            m_text = m_text.Substring(0, m_cursorPosition) + m_text.Substring(m_cursorPosition + 1);
                    
                            if (OnTextChanged != null)
                                OnTextChanged(this, m_text);

                            Relayout();
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
            
            if (IsFocused)
            {
                m_cursorPosition += change;

                if (m_cursorPosition < m_preffix.Length)
                    m_cursorPosition = m_preffix.Length;

                if (m_cursorPosition > m_text.Length)
                    m_cursorPosition = m_text.Length;

                float cursorX;
                var frame = m_label.GetCharFrame(m_cursorPosition == m_text.Length ? m_text.Length - 1 : m_cursorPosition);

                if (m_cursorPosition == m_text.Length)
                    cursorX = frame.X + frame.Width + Font.Spacing;
                else
                    cursorX = frame.X;

                float nsize = Size.X / FontSize;

                if (m_label.Size.X > nsize) // scrolling
                {
                    float from = -m_label.Position.X / FontSize;
                    float to = from + nsize;

                    if (cursorX > from && frame.X < to)
                    {
                    } else
                    {
                        if (cursorX > from)
                        {
                            float nx = (nsize - cursorX) * FontSize - TextPadding.Width;
                            if (nx > TextPadding.Left)
                                nx = TextPadding.Left;

                            m_label.Position = new Vector2(nx, TextPadding.Top);
                        } else
                            if (cursorX < to)
                        {
                            float nx = -frame.X * FontSize;
                            if (nx > TextPadding.Left)
                                nx = TextPadding.Left;

                            m_label.Position = new Vector2(nx, TextPadding.Top);
                        }
                    }
                } else
                    m_label.Position = TextPadding.TopLeft;

                m_cursor.Position = m_label.Position + new Vector2((cursorX - m_cursor.Sprite.FrameSize.X / 2) * FontSize, 0 - 5 * FontSize); // Nomad: 5 is magic constant. Don't like it at all 

                //m_cursor.Position = m_label.Position + new Vector2((cursorX - Font.SpaceWidth * 0.5f) * FontSize + 1, 0);

            } else
            {
                m_label.Position = TextPadding.TopLeft;
            }
        }

        private static string MaskText(string text, string maskChar)
        {
            if (string.IsNullOrEmpty(maskChar))
                return text;

            char [] result = text.ToCharArray();

            for (int i = 0; i < result.Length; i++)
                result[i] = maskChar[0];

            return new string(result);
        }

        public void SetFocused(bool value)
        {
            if (IsFocused == value)
                return;

            IsFocused = value;

            m_cursorPosition = m_text.Length;
            UpdateCursor(0);
            
            WidgetManager.UpdateFocus(this, value);
        }

        public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            if (press)
            {
                SetFocused(true);

                Vector2 local = (this.Transform.GetClientPoint(new Vector2(x, y)) - m_label.Position) / FontSize;
                ISprite[] sprites = m_label.InternalGetSprites();

                if (sprites.Length > 0)
                {
                    bool found = false;

                    for (int i = 0; i < sprites.Length; i++)
                    {
                        if (local.X < sprites[i].Position.X + sprites[i].FrameSize.X / 2)
                        {
                            UpdateCursor(i - m_cursorPosition);
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

            if (!press && !unpress && !Hovered)
            {
                Hovered = true;
                WindowController.Instance.OnTouch += UnHoverTouch;
            }
            return true;
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

        public override void Remove()
        {
            WidgetManager.UpdateFocus(this, false);
            base.Remove();
        }
    }
}

