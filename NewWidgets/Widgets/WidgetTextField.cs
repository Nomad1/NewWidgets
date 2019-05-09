using System;
using System.Numerics;

using NewWidgets.UI;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    public class WidgetTextField : WidgetBackground, IFocusableWidget
    {
        public static readonly new WidgetStyleSheet DefaultStyle = WidgetManager.GetStyle("default_textedit", true);

        private int m_cursorPosition;
        private int m_cursorLine;
        private int m_cursorLinePosition;

        private LabelObject[] m_labels;
        private ImageObject m_cursor;

        private string m_text;
        private string[] m_lines;

        private Vector2 m_contentSize;
        private Vector2 m_contentPosition;
        private int m_lineHeight;

        private bool m_needLayout;

        public event Action<WidgetTextField, string> OnFocusLost;
        public event Action<WidgetTextField, string> OnTextEntered;
        public event Action<WidgetTextField, string> OnTextChanged;

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

        public Margin TextPadding
        {
            get { return GetProperty(WidgetParameterIndex.TextPadding, new Margin(0)); }
            set { SetProperty(WidgetParameterIndex.TextPadding, value); SetTextPosition(m_contentPosition); }
        }

        public float LineSpacing
        {
            get { return GetProperty(WidgetParameterIndex.LineSpacing, 5.0f); }
            set { SetProperty(WidgetParameterIndex.LineSpacing, value); SetTextPosition(m_contentPosition); }
        }

        public int CursorColor
        {
            get { return GetProperty(WidgetParameterIndex.CursorColor, 0xffffff); }
            set
            {
                SetProperty(WidgetParameterIndex.CursorColor, value);

                if (m_cursor != null) // try to avoid settings m_needLayout
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
                InivalidateLayout();
                m_cursorPosition = value.Length;
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

        public int Color
        {
            get { return GetProperty(WidgetParameterIndex.TextColor, 0xffffff); }
            set
            {
                SetProperty(WidgetParameterIndex.TextColor, value);

                if (m_labels != null)
                    foreach (LabelObject label in m_labels) // try to avoid settings m_needLayout
                        label.Color = value;
            }
        }

        public override float Alpha
        {
            get { return base.Alpha; }
            set
            {
                base.Alpha = value;

                if (m_labels != null)
                    foreach (LabelObject label in m_labels) // try to avoid settings m_needLayout
                        label.Alpha = value;
            }
        }

        public int LineCount
        {
            get { return m_labels == null ? 0 : m_labels.Length; }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetTextEdit"/> class.
        /// </summary>
        /// <param name="style">Style.</param>
        public WidgetTextField(WidgetStyleSheet style = default(WidgetStyleSheet))
            : base(style.IsEmpty ? DefaultStyle : style)
        {
            m_text = string.Empty;
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
            m_lines = m_text.Split(new string[] { "\r", "\n", "|n", "\\n" }, StringSplitOptions.None);

            if (m_lines.Length == 0)
                m_lines = new string[] { "" };
            else
                if (m_lines.Length > 1)
                    m_text = string.Join("\n", m_lines); // we need it to avoid multi-char line feeds

            m_lineHeight = (int)((Font.Height + LineSpacing) * FontSize + 0.5f);

            Vector2 maxSize = new Vector2(Size.X, 0);
            Vector2[] sizes = new Vector2[m_lines.Length];

            for (int i = 0; i < m_lines.Length; i++)
            {
                string line = m_lines[i];

                Vector2 size = Font.MeasureString(line);

                size = new Vector2(size.X * FontSize, m_lineHeight);
                maxSize = new Vector2(Math.Max(size.X, maxSize.X), size.Y + maxSize.Y);

                sizes[i] = size;
                m_lines[i] = line;
            }

            m_contentSize = maxSize + TextPadding.Size;

            if (m_labels == null || m_lines.Length != m_labels.Length)
                m_labels = new LabelObject[m_lines.Length];

            for (int i = 0; i < m_lines.Length; i++)
            {
                if (m_labels[i] == null)
                    m_labels[i] = new LabelObject(this, Font, string.Empty, LabelAlign.Start, LabelAlign.Start, false);

                m_labels[i].Color = Color;
                m_labels[i].Scale = FontSize;
                m_labels[i].Alpha = Alpha;
                m_labels[i].Text = m_lines[i];
                m_labels[i].Size = sizes[i] / FontSize;
            }

            if (m_cursor == null)
            {
                m_cursor = new ImageObject(this, Font.GetSprites(CursorChar, Vector2.Zero)[0]);
                m_cursor.Sprite.Color = CursorColor;
                m_cursor.Scale = FontSize;
                m_cursor.Transform.Parent = Transform;
            }

            m_needLayout = false;

            UpdateCursor(0, 0);
        }

        public override bool Update()
        {
            if (!base.Update())
                return false;

            if (m_needLayout)
                Relayout();

            if (m_labels != null)
                foreach (LabelObject label in m_labels)
                    label.Update();

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

            if (m_labels != null)
                foreach (LabelObject label in m_labels)
                    label.Draw(canvas);

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

                if (m_cursorPosition == m_text.Length)
                    m_text += toAdd;
                else
                    m_text = m_text.Insert(m_cursorPosition, toAdd);

                if (OnTextChanged != null)
                    OnTextChanged(this, m_text);

                m_cursorPosition += toAdd.Length;
                m_cursorLinePosition += toAdd.Length;
                Relayout();
                return true;
            }

            if (!up)
            {
                switch (key)
                {
                    case SpecialKey.Backspace:
                        if (m_cursorPosition > 0)
                        {
                            m_text = m_text.Substring(0, m_cursorPosition - 1) + m_text.Substring(m_cursorPosition);

                            if (OnTextChanged != null)
                                OnTextChanged(this, m_text);

                            m_cursorPosition--;
                            m_cursorLinePosition--;
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
                    case SpecialKey.Enter:
                        {
                            if (m_cursorPosition == m_text.Length)
                                m_text += "\n";
                            else
                                m_text = m_text.Insert(m_cursorPosition, "\n");

                            if (OnTextChanged != null)
                                OnTextChanged(this, m_text);

                            m_cursorPosition++;
                            m_cursorLine++;
                            m_cursorLinePosition = 0;

                            Relayout();
                        }
                        break;
                    case SpecialKey.Left:
                        if (m_cursorPosition > 0)
                            UpdateCursor(-1, 0);
                        break;
                    case SpecialKey.Right:
                        if (m_cursorPosition <= m_text.Length - 1)
                            UpdateCursor(1, 0);
                        break;
                    case SpecialKey.Up:
                        if (m_cursorLine > 0)
                            UpdateCursor(0, -1);
                        break;
                    case SpecialKey.Down:
                        if (m_cursorLine < m_lines.Length - 1)
                            UpdateCursor(0, 1);
                        break;
                }
            }

            switch (key)
            {
                case SpecialKey.Up:
                case SpecialKey.Down:
                    return false;
            }

            return true;
        }

        private void UpdateCursor(int symbolChange, int lineChange)
        {
            if (m_needLayout)
                return;

            if (IsFocused)
            {
                // changes

                if (lineChange != 0)
                {
                    m_cursorLine += lineChange;
                }
                else
                {
                    m_cursorLinePosition += symbolChange;

                    if (m_cursorLinePosition < 0)
                    {
                        m_cursorLine--;
                        m_cursorLinePosition = m_lines[m_cursorLine].Length;
                    }

                    if (m_cursorLinePosition > m_lines[m_cursorLine].Length)
                    {
                        m_cursorLinePosition = 0;
                        m_cursorLine++;
                    }
                }

                // validation

                if (m_cursorLine < 0)
                    m_cursorLine = 0;

                if (m_cursorLine > m_lines.Length - 1)
                    m_cursorLine = m_lines.Length - 1;

                if (m_cursorLinePosition > m_lines[m_cursorLine].Length)
                    m_cursorLinePosition = m_lines[m_cursorLine].Length;

                // calculate global text position

                int position = 0;

                for (int i = 0; i < m_cursorLine; i++)
                    position += m_lines[i].Length + 1;

                position += m_cursorLinePosition;

                m_cursorPosition = position;

                float cursorY = m_lineHeight * m_cursorLine;
                float cursorX;

                string line = m_lines[m_cursorLine];

                var frame = m_labels[m_cursorLine].GetCharFrame(m_cursorLinePosition == line.Length ? line.Length - 1 : m_cursorLinePosition);

                if (m_cursorLinePosition == line.Length)
                    cursorX = frame.X + frame.Width + Font.Spacing;
                else
                    cursorX = frame.X;

                // TODO: find cursor line and position in line

                /*

                var frame = m_label.GetCharFrame(m_cursorPosition);
                float nsize = Size.X / FontSize;

                if (m_label.Size.X > nsize)
                {
                    float from = -m_label.Position.X / FontSize;
                    float to = from + nsize;

                    if (frame.X > from && frame.X < to)
                    {
                    }
                    else
                    {
                        if (frame.X > from)
                        {
                            float nx = (nsize - frame.X) * FontSize - TextPadding.Width;
                            if (nx > TextPadding.Left)
                                nx = TextPadding.Left;

                            m_label.Position = new Vector2(nx, TextPadding.Top);
                        }
                        else
                            if (frame.X < to)
                        {
                            float nx = -frame.X * FontSize;
                            if (nx > TextPadding.Left)
                                nx = TextPadding.Left;

                            m_label.Position = new Vector2(nx, TextPadding.Top);
                        }
                    }
                }
                else
                    m_label.Position = TextPadding.TopLeft;*/

                SetTextPosition(Vector2.Zero);

                m_cursor.Position = m_contentPosition + new Vector2((cursorX - m_cursor.Sprite.FrameSize.X / 2) * FontSize, cursorY - 5 * FontSize); // Nomad: 5 is magic constant. Don't like it at all 
            } else
                SetTextPosition(Vector2.Zero);
        }

        private void SetTextPosition(Vector2 offset)
        {
            m_contentPosition = TextPadding.TopLeft + offset;

            float y = 0;

            for (int i = 0; i < m_labels.Length; i++)
            {
                LabelObject label = m_labels[i];

                float x;

                //if ((TextAlign & WidgetAlign.HorizontalCenter) == WidgetAlign.HorizontalCenter)
                    //x = (m_contentSize.X - label.Size.X) / 2;
                //else if ((TextAlign & WidgetAlign.Right) == WidgetAlign.Right)
                    //x = m_contentSize.X - label.Size.X;
                //else
                    x = 0;

                label.Position = m_contentPosition + new Vector2(x, y);

                m_labels[i] = label;

                y += m_lineHeight;
            }
        }

        public void SetFocused(bool value)
        {
            if (IsFocused == value)
                return;

            IsFocused = value;

            //UpdateColor();
            m_cursorPosition = m_text.Length;
            UpdateCursor(0, 0);

            WidgetManager.UpdateFocus(this, value);
            //WidgetManager.UpdateFocus(this, true);
        }

        public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            if (press)
            {
                bool wasFocused = IsFocused;

                SetFocused(true);

                LabelObject label = null;

                for (int i = 0; i < m_labels.Length; i++)
                {
                    if (m_labels[i].HitTest(x, y))
                    {
                        label = m_labels[i];
                        m_cursorLine = i;
                        break;
                    }
                }

                Vector2 local = Transform.GetClientPoint(new Vector2(x, y)) - m_contentPosition;

                if (label == null)
                {
                    m_cursorLine = (int)Math.Floor(local.Y / m_lineHeight);

                    if (m_cursorLine < 0)
                    {
                        m_cursorLine = 0;
                        m_cursorLinePosition = 0;
                    }
                    else
                    {
                        if (m_cursorLine > m_lines.Length - 1)
                            m_cursorLine = m_lines.Length - 1;
                        m_cursorLinePosition = m_lines[m_cursorLine].Length;
                    }

                    UpdateCursor(0, 0);
                }
                else
                {
                    bool spriteFound = false;

                    float lx = local.X / FontSize;

                    ISprite[] sprites = label.InternalGetSprites();
                    for (int i = 0; i < sprites.Length; i++)
                    {
                        if (lx < sprites[i].Position.X + sprites[i].FrameSize.X / 2)
                        {
                            m_cursorLinePosition = i;
                            spriteFound = true;
                            break;
                        }
                    }

                    if (!spriteFound)
                    {
                        if (local.X < label.Position.X)
                            m_cursorLinePosition = 0;
                        else
                            if (local.X > label.Position.X + label.Size.X)
                                m_cursorLinePosition = m_lines[m_cursorLine].Length;
                    }

                    UpdateCursor(0, 0);
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

