using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    /// <summary>
    /// Progress line control
    /// </summary>
    public class WidgetProgressLine : WidgetPanel // TODO: rename to WidgetProgress
    {
        public new const string ElementType = "progress";
        public const string BackLabelId = "progress_back_label";
        public const string FrontLabelId = "progress_front_label";
        public const string LineId = "progress_line";
        //
        private string m_text;
        private float m_progress;

        //private readonly WidgetBackground m_progressLineBack;
        private readonly WidgetBackground m_progressLine;
        private readonly WidgetLabel m_backText;
        private readonly WidgetLabel m_frontText;

        public string Text
        {
            get { return m_text; }
            set { m_text = value; SetProgress(m_progress); }
        }

        public uint ProgressForegroundColor
        {
            get { return m_progressLine.BackgroundColor; }
            set { m_progressLine.BackgroundColor = value; }
        }

        public uint ProgressBackgroundColor
        {
            get { return BackgroundColor; }
            set { BackgroundColor = value; }
        }

        public float ProgressForegroundAlpha
        {
            get { return m_progressLine.Opacity;  }
            set { m_progressLine.Opacity = value; }
        }

        public float ProgressBackgroundAlpha
        {
            get { return Opacity; }
            set { Opacity = value; }
        }

        public float Progress
        {
            get { return m_progress; }
            set { SetProgress(value); }
        }

        /// <summary>
        /// Creates progress line control
        /// </summary>
        /// <param name="style"></param>
        public WidgetProgressLine(string text = "")
            : this(ElementType, default(WidgetStyle), text)
        {
        }

        /// <summary>
        /// Creates progress line control
        /// </summary>
        /// <param name="text"></param>
        public WidgetProgressLine(WidgetStyle style, string text = "")
           : this(ElementType, style, text)
        {
        }

        /// <summary>
        /// Creates progress line control
        /// </summary>
        /// <param name="style"></param>
        /// <param name="text"></param>
        protected WidgetProgressLine(string elementType, WidgetStyle style, string text)
            : base(elementType, style)
        {
            //m_progressLineBack = new WidgetBackground(WidgetManager.GetStyle(barStyle));
            //m_progressLineBack.Size = Size;
            //m_progressLineBack.Position = new Vector2(0, 0);
            //m_progressLineBack.Opacity = 0.5f;
            //AddChild(m_progressLineBack);

            m_backText = new WidgetLabel(new WidgetStyle(BackLabelId));
            AddChild(m_backText);

            m_progressLine = new WidgetBackground(new WidgetStyle(LineId));
            AddChild(m_progressLine);

            m_frontText = new WidgetLabel(new WidgetStyle(FrontLabelId));
            AddChild(m_frontText);

            m_progress = 0;
            Text = text;
        }

        protected override void UpdateLayout()
        {
            base.UpdateLayout();

            m_progressLine.Size = Size;
            m_backText.Size = Size;
            m_frontText.Size = Size;

            m_backText.Relayout();
            m_backText.Position = m_backText.GetProperty(WidgetParameterIndex.Padding, Margin.Empty).TopLeft;

            m_frontText.Relayout();
            m_frontText.Position = m_frontText.GetProperty(WidgetParameterIndex.Padding, Margin.Empty).TopLeft;
        }

        public void SetProgress(float progress)
        {
            m_frontText.Text = m_backText.Text = string.Format(m_text, progress);
            m_frontText.ClipMargin = m_progressLine.ClipMargin = new Margin(0, 0, Size.X * (1.0f - progress), 0);
            m_progress = progress;
        }
    }
}
