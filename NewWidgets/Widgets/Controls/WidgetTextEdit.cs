using System;
using System.Numerics;
using System.Text;
using NewWidgets.UI;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    /// <summary>
    /// Single-line text edit with support for masking and non-editable preffix
    /// </summary>
    public class WidgetTextEdit : WidgetBackground, IFocusable
    {
        public new const string ElementType = "textedit";
        //
        private int m_cursorPosition;
        private Vector2 m_contentOffset;

        private LabelObject m_label;
        private ImageObject m_cursor;

        private string m_preffix;
        private string m_text;

        private bool m_tabToValidate;

        public event Action<WidgetTextEdit, string> OnFocusLost;
        public event Action<WidgetTextEdit, string> OnTextEntered;
        public event Action<WidgetTextEdit, string> OnTextChanged;
        public event Func<WidgetTextEdit, SpecialKey, string, bool> OnKeyPressed;
        /// <summary>
        /// Occurs when key is pressed and nex text is to be added. First parameter is source string second is input string
        /// Delegate should return true if string is valid and false if not
        /// </summary>
        public event Func<string, string, bool> OnValidateInput;

        public Font Font
        {
            get { return GetProperty(WidgetParameterIndex.Font, WidgetManager.MainFont); }
            set { SetProperty(WidgetParameterIndex.Font, value); InvalidateLayout(); }
        }

        public float FontSize
        {
            get { return GetProperty(WidgetParameterIndex.FontSize, 1.0f); }
            set { SetProperty(WidgetParameterIndex.FontSize, value); InvalidateLayout(); }
        }

        public WidgetAlign TextAlign
        {
            get { return GetProperty(WidgetParameterIndex.TextAlign, WidgetAlign.Left | WidgetAlign.Top); }
            set { SetProperty(WidgetParameterIndex.TextAlign, value); InvalidateLayout(); }
        }

        public Margin TextPadding
        {
            get { return GetProperty(WidgetParameterIndex.Padding, Margin.Empty); }
            set { SetProperty(WidgetParameterIndex.Padding, value); InvalidateLayout(); }
        }

        public uint TextColor
        {
            get { return GetProperty(WidgetParameterIndex.TextColor, (uint)0xffffff); }
            set
            {
                SetProperty(WidgetParameterIndex.TextColor, value);

                if (m_label != null) // try to avoid InivalidateLayout
                    m_label.Color = value;
            }
        }

        //public uint FocusedTextColor
        //{
        //    get { return GetProperty(WidgetState.Selected, WidgetParameterIndex.TextColor, (uint)0xffffff); }
        //    set
        //    {
        //        SetProperty(WidgetState.Selected, WidgetParameterIndex.TextColor, value);

        //        if (CurrentState == WidgetState.Selected && m_label != null) // try to avoid InvalidateLayout
        //            m_label.Color = value;
        //    }
        //}

        public uint CursorColor
        {
            get { return GetProperty(WidgetParameterIndex.CursorColor, (uint)0xffffff); }
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
                InvalidateLayout();
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
                InvalidateLayout();
            }
        }

        public string MaskChar
        {
            get { return GetProperty(WidgetParameterIndex.MaskChar, ""); }
            set
            {
                SetProperty(WidgetParameterIndex.MaskChar, value);
                InvalidateLayout();
            }
        }

        public bool IsFocused
        {
            get { return Selected; }
            private set { Selected = value; }
        }

        public override bool Enabled
        {
            get
            {
                return base.Enabled;
            }
            set
            {
                if (value && IsFocused)
                    SetFocused(false);
                base.Enabled = value;
            }
        }

        public bool IsFocusable
        {
            get { return Enabled; }
        }

        public bool TabToValidate
        {
            get { return m_tabToValidate; }
            set { m_tabToValidate = value; }
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
                InvalidateLayout();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetTextEdit"/> class.
        /// </summary>
        /// <param name="style">Style.</param>
        public WidgetTextEdit(WidgetStyle style = default(WidgetStyle))
            : this(ElementType, style)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetTextEdit"/> class.
        /// </summary>
        /// <param name="elementType"></param>
        /// <param name="style"></param>
        protected WidgetTextEdit(string elementType, WidgetStyle style)
            : base(elementType, style)
        {
            m_text = string.Empty;
            m_preffix = string.Empty;
            ClipMargin = TextPadding;
        }

        protected override void UpdateLayout()
        {
            if (m_label == null)
            {
                m_label = new LabelObject(this, Font, MaskText(m_text, MaskChar));
                m_label.Position = TextPadding.TopLeft;
            }
            else
                m_label.Text = MaskText(m_text, MaskChar);

            m_label.Color = TextColor;
            m_label.Scale = FontSize;
            m_label.Opacity = Opacity;

            if (m_cursor == null)
            {
                m_cursor = new ImageObject(this, Font.GetSprites(CursorChar, Vector2.Zero)[0]);
                m_cursor.Sprite.Color = CursorColor;
                m_cursor.Scale = FontSize;
                m_cursor.Transform.Parent = Transform;
            }

            /* // Nomad: I'm not sure it is normal to resize text edit depending on it's content. It looks like leftovers from WidgetLabel copy-paste
             Vector2 minSize = m_label.Size * FontSize + TextPadding.Size;
             if (minSize.X < Size.X)
                 minSize.X = Size.X;
             if (minSize.Y < Size.Y)
                 minSize.Y = Size.Y;

             Size = minSize;*/            

            UpdateCursor(0);

            base.UpdateLayout();
        }

        public override bool Update()
        {
            if (!base.Update())
                return false;

            if (m_label != null)
                m_label.Update();

            if (m_cursor != null)
            {
                m_cursor.Sprite.Alpha = (byte)(Math.Sin(WindowController.Instance.GetTime() / 1000.0f) * 64 + 191); // blinks every 2 seconds from 127 to 255
                m_cursor.Update();
            }

            return true;
        }

        protected override void DrawContents()
        {
            base.DrawContents();

            if (m_label != null)
                m_label.Draw();

            if (IsFocused)
                m_cursor.Draw();
        }

        public override bool Key(SpecialKey key, bool up, string keyString)
        {
            if (!IsFocused)
                return false;

            if (!Enabled)
                return false;

            if (OnKeyPressed != null && OnKeyPressed(this, key, keyString))
            {
                return true;
            }

            if (up && key == SpecialKey.Back)
            {
                if (OnTextEntered != null)
                    OnTextEntered(this, string.Empty);
                return true;
            }

            if (up && (key == SpecialKey.Enter || key == SpecialKey.Joystick_Start))
            {
                if (OnTextEntered != null)
                    OnTextEntered(this, m_text);
                return true;
            }

            if (up && key == SpecialKey.Tab)
            {
                if (TabToValidate && OnTextEntered != null)
                    OnTextEntered(this, m_text);

                if (WidgetManager.FocusNext(this) && OnFocusLost != null)
                    OnFocusLost(this, m_text);

                return true;
            }

            if ((key == SpecialKey.Letter || key == SpecialKey.Paste)/* && Font.HaveSymbol(character)*/)
            {
                // filter non-printable chars

                StringBuilder stringToAdd = new StringBuilder(keyString.Length);

                for (int i = 0; i < keyString.Length; i++)
                    if (Font.HaveSymbol(keyString[i]))
                        stringToAdd.Append(keyString[i]);

                if (stringToAdd.Length > 0)
                {
                    string toAdd = stringToAdd.ToString();

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
                    UpdateLayout();
                    return true;
                }
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
                            UpdateLayout();
                        }
                        break;
                    case SpecialKey.Delete:
                        if (m_cursorPosition <= m_text.Length - 1)
                        {
                            m_text = m_text.Substring(0, m_cursorPosition) + m_text.Substring(m_cursorPosition + 1);

                            if (OnTextChanged != null)
                                OnTextChanged(this, m_text);

                            UpdateLayout();
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

            switch (key)
            {
                case SpecialKey.Up:
                case SpecialKey.Down:
                    return false;
            }



            return true;
        }

        private void UpdateCursor(int change)
        {
            if (NeedsLayout)
                return;

            if (IsFocused)
            {
                m_cursorPosition += change;

                if (m_cursorPosition < m_preffix.Length)
                    m_cursorPosition = m_preffix.Length;

                if (m_cursorPosition > m_text.Length)
                    m_cursorPosition = m_text.Length;

                float cursorY = 0;
                float cursorX;

                var frame = m_label.GetCharFrame(m_cursorPosition == m_text.Length ? m_text.Length - 1 : m_cursorPosition);

                if (m_cursorPosition == m_text.Length)
                    cursorX = frame.X + frame.Width;
                else
                    cursorX = frame.X;

                cursorX *= FontSize;

                if (cursorX + (frame.Width + Font.SpaceWidth) * FontSize + m_contentOffset.X > Size.X)
                    m_contentOffset.X = Size.X - cursorX - (frame.Width + Font.SpaceWidth) * FontSize;
                else
                    if (cursorX + m_contentOffset.X < 0)
                    m_contentOffset.X = -cursorX;

                m_cursor.Position = TextPadding.TopLeft + m_contentOffset +
                    new Vector2(cursorX - (m_cursor.Sprite.FrameSize.X / 2) * FontSize, cursorY - 4 * FontSize); // HACK: 4 is magic constant. Don't like it at all 

            }

            UpdateTextPosition();
        }

        private void UpdateTextPosition()
        {
            m_label.Position = TextPadding.TopLeft + m_contentOffset;
        }

        private static string MaskText(string text, string maskChar)
        {
            if (string.IsNullOrEmpty(maskChar))
                return text;

            char[] result = text.ToCharArray();

            for (int i = 0; i < result.Length; i++)
                result[i] = maskChar[0];

            return new string(result);
        }

        public void SetFocused(bool value)
        {
            if (IsFocused == value)
                return;

            if (!Enabled)
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
                        if (local.X < sprites[i].Transform.Position.X + sprites[i].FrameSize.X / 2)
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
                if (m_label != null)
                    m_label.Hovered = true;
                WindowController.Instance.OnTouch += UnHoverTouch;
            }
            return true;
        }

        private bool UnHoverTouch(float x, float y, bool press, bool unpress, int pointer)
        {
            if (Hovered && !HitTest(x, y))
            {
                Hovered = false;
                if (m_label != null)
                    m_label.Hovered = false;
                WindowController.Instance.OnTouch -= UnHoverTouch;
            }
            return false;
        }

        public override void Remove()
        {
            WidgetManager.UpdateFocus(this, false);
            base.Remove();
        }

        public void Press()
        {
        }
    }
}

