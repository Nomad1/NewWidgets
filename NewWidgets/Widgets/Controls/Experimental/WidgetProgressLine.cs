using System.Numerics;
using NewWidgets.Widgets;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    public class WidgetProgressLine : WidgetPanel // TODO: rename to WidgetProgress, apply styles and move to NewWidgets
    {
        private string m_text;
        private float m_progress;

        private readonly WidgetBackground m_progressLineBack;
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
            get { return m_progressLineBack.BackgroundColor; }
            set { m_progressLineBack.BackgroundColor = value; }
        }

        public float ProgressForegroundAlpha
        {
            get { return m_progressLine.Opacity;  }
            set { m_progressLine.Opacity = value; }
        }

        public float ProgressBackgroundAlpha
        {
            get { return m_progressLineBack.Opacity; }
            set { m_progressLineBack.Opacity = value; }
        }

        public float Progress
        {
            get { return m_progress; }
            set { SetProgress(value); }
        }

        public WidgetProgressLine(string text = "", string barStyle = "progress_bar")
            : base(Widget.DefaultStyle)
        {
            Size = new Vector2(300, 20);

            m_progressLineBack = new WidgetBackground(WidgetManager.GetStyle(barStyle));
            m_progressLineBack.Size = Size;
            m_progressLineBack.Position = new Vector2(0, 0);
            m_progressLineBack.Opacity = 0.5f;
            AddChild(m_progressLineBack);

            m_backText = new WidgetLabel("");
            m_backText.TextAlign = WidgetAlign.HorizontalCenter | WidgetAlign.VerticalCenter;
            m_backText.Size = Size;
            m_backText.Position = new Vector2(0, -3);
            m_backText.FontSize = WidgetManager.FontScale * 0.7f;
            m_backText.Color = 0x222222;
            AddChild(m_backText);

            m_progressLine = new WidgetBackground(WidgetManager.GetStyle(barStyle));
            m_progressLine.Size = m_progressLineBack.Size;
            m_progressLine.Position = m_progressLineBack.Position;
            m_progressLine.ClipContents = true;
            m_progressLine.BackgroundDepth = WidgetBackgroundDepth.BackClipped;
            AddChild(m_progressLine);

            m_frontText = new WidgetLabel(m_backText.Text);
            m_frontText.TextAlign = m_backText.TextAlign;
            m_frontText.Size = m_backText.Size;
            m_frontText.Position = m_backText.Position;
            m_frontText.FontSize = m_backText.FontSize;
            m_frontText.Color = 0x666666;
            m_frontText.ClipContents = true;
            AddChild(m_frontText);

            m_progress = 0;
            Text = text;
        }

        protected override void Resize(Vector2 size)
        {
            if (m_progressLineBack != null)
            {
                m_progressLineBack.Size = size;
                m_progressLine.Size = size;
                m_backText.Size = size;
                m_frontText.Size = size;
            }

            base.Resize(size);
        }

        public void SetProgress(float progress)
        {
            m_frontText.Text = m_backText.Text = string.Format(m_text, progress);
            m_frontText.ClipMargin = m_progressLine.ClipMargin = new Margin(0, 0, m_progressLine.Size.X * (1.0f - progress), 0);
            m_progress = progress;
        }
    }
}
