using NewWidgets.UI;
using NewWidgets.Utility;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace NewWidgets.Widgets
{
    public class WidgetText : Widget
    {
        public static readonly new WidgetStyleSheet DefaultStyle = WidgetManager.GetStyle("default_text", true);

        private static char[] s_separatorChars = { ' ', '\t' };

        private LabelObject[] m_labels;

        private string m_text;

        private float m_maxWidth;

        private bool m_needLayout;

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

        public float LineSpacing
        {
            get { return GetProperty(WidgetParameterIndex.LineSpacing, 1.0f); }
            set { SetProperty(WidgetParameterIndex.LineSpacing, value); InivalidateLayout(); }
        }

        public float MaxWidth
        {
            get { return m_maxWidth; }
            set { m_maxWidth = value; InivalidateLayout(); }
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

        public bool RichText
        {
            get { return GetProperty(WidgetParameterIndex.RichText, true); }
            set { SetProperty(WidgetParameterIndex.RichText, value); InivalidateLayout(); }
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

        public WidgetText(string text)
            : this(default(WidgetStyleSheet), text)
        {
        }

        public WidgetText(WidgetStyleSheet style = default(WidgetStyleSheet), string text = "")
            : base(style.IsEmpty? DefaultStyle : style)
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
            string[] lines = string.IsNullOrEmpty(m_text) ? new string[0]: m_text.Split(new string[] { Environment.NewLine, "\r", "\n", "|n", "\\n" }, StringSplitOptions.None);

            float lineHeight = (Font.Height + LineSpacing) * FontSize; // TODO: spacing

            Vector2 maxSize = Vector2.Zero;
            Vector2[] sizes = new Vector2[lines.Length];
            LabelObject.TextSpan[][] colors = null;

            if (RichText)
                colors = new LabelObject.TextSpan[lines.Length][];

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (RichText)
                    line = LabelObject.ParseRichText(line, Color, out colors[i], (int)(Font.SpaceWidth + Font.Spacing));

                Vector2 size = Font.MeasureString(line);

                size = new Vector2(size.X * FontSize, lineHeight);
                maxSize = new Vector2(Math.Max(size.X, maxSize.X), size.Y + maxSize.Y);

                sizes[i] = size;
                lines[i] = line;
            }

            if (m_maxWidth <= 0 || m_maxWidth > maxSize.X)
            {
                Size = new Vector2((TextAlign & (WidgetAlign.HorizontalCenter | WidgetAlign.Right)) != 0 && m_maxWidth > 0 ? m_maxWidth : Math.Max(maxSize.X, Size.X), Math.Max(Size.Y, maxSize.Y));
            }
            else
            {
                // Perform text wrapping

                List<string> newLines = new List<string>();
                List<Vector2> newSizes = new List<Vector2>();
                List<ArraySegment<LabelObject.TextSpan>> newColors = new List<ArraySegment<LabelObject.TextSpan>>();

                for (int i = 0; i < lines.Length; i++)
                {
                    if (sizes[i].X > m_maxWidth)
                    {
                        float[] charSizes = Font.MeasureChars(lines[i]);

                        float width = 0;
                        int lastSeparator = -1;
                        int start = 0;

                        for (int j = 0; j < charSizes.Length; j++)
                        {
                            if (Array.IndexOf(s_separatorChars, lines[i][j]) != -1)
                                lastSeparator = j;

                            width += charSizes[j] * FontSize;

                            if (width > m_maxWidth)
                            {
                                if (lastSeparator == -1)
                                    lastSeparator = j;

                                string line = lines[i].Substring(start, lastSeparator - start);
                                newLines.Add(line);

                                width = 0;
                                for (int k = start; k < lastSeparator; k++)
                                    width += charSizes[k] * FontSize;

                                newSizes.Add(new Vector2(width, lineHeight));

                                if (RichText)
                                    newColors.Add(new ArraySegment<LabelObject.TextSpan>(colors[i], start, lastSeparator - start));

                                if (lines[i][lastSeparator] == ' ') // skip line start space
                                    lastSeparator++;

                                start = lastSeparator;
                                width = 0;

                                for (int k = lastSeparator; k <= j; k++)
                                    width += charSizes[k] * FontSize;

                                lastSeparator = -1;
                            }
                        }

                        if (start < charSizes.Length)
                        {
                            newLines.Add(lines[i].Substring(start));
                            newSizes.Add(new Vector2(width, lineHeight));

                            if (RichText)
                                newColors.Add(new ArraySegment<LabelObject.TextSpan>(colors[i], start, colors[i].Length - start));
                        }

                    }
                    else
                    {
                        newLines.Add(lines[i]);
                        newSizes.Add(sizes[i]);
                        if (RichText)
                            newColors.Add(new ArraySegment<LabelObject.TextSpan>(colors[i]));
                    }
                }
                lines = newLines.ToArray();
                sizes = newSizes.ToArray();

                if (RichText)
                {
                    LabelObject.TextSpan[][] newColorsArray = new LabelObject.TextSpan[newColors.Count][];
                    for (int i = 0; i < newColors.Count; i++)
                    {
                        ArraySegment<LabelObject.TextSpan> segment = newColors[i];
                        newColorsArray[i] = new LabelObject.TextSpan[segment.Count];
                        Array.Copy(segment.Array, segment.Offset, newColorsArray[i], 0, segment.Count);
                    }

                    colors = newColorsArray;
                }

                float height = sizes.Length * lineHeight;
                Size = new Vector2(Math.Max(Size.X, m_maxWidth), Math.Max(Size.Y, height));
            }

            m_labels = new LabelObject[lines.Length]; // TODO: reuse old labels?

            float y = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                LabelObject label = new LabelObject(this, Font, string.Empty, LabelAlign.Start, LabelAlign.Start, false);
                label.Color = Color;
                label.Scale = FontSize;
                label.Alpha = Alpha;
                label.Text = lines[i];

                if (RichText)
                    label.SetColors(colors[i]);

                float x = 0;

                if ((TextAlign & WidgetAlign.HorizontalCenter) == WidgetAlign.HorizontalCenter)
                    x = (Size.X - sizes[i].X) / 2;
                else if ((TextAlign & WidgetAlign.Right) == WidgetAlign.Right)
                    x = Size.X - sizes[i].X;

                label.Position = new Vector2(x, y);

                m_labels[i] = label;

                y += lineHeight;
            }

            m_needLayout = false;
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

            return true;
        }

        protected override void DrawContents()
        {
            base.DrawContents();

            if (m_labels != null)
                foreach (LabelObject label in m_labels)
                    label.Draw();
        }
    }
}

