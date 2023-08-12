using System;
using System.Numerics;
using NewWidgets.UI;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    /// <summary>
    /// Draggable slider widget
    /// </summary>
    public class WidgetSlider : WidgetPanel
    {
        public new const string ElementType = "slider";

        public const string TrackerClass = "slider_track";
        public const string LabelClass = "slider_label";
        public const string LineClass = "slider_line";

        // TODO: make configurable
        private static readonly float s_inset = 15f;

        private readonly WidgetProgressLine m_progressLine;
        private readonly TrackingButton m_trackButton;
        private readonly WidgetLabel m_label;

        private float m_max;
        private float m_min;
        private float m_value;
        private string m_text;

        public float Max
        {
            get { return m_max; }
            set { m_max = value; }
        }

        public float Min
        {
            get { return m_min; }
            set { m_min = value; }
        }

        public float Value
        {
            get { return m_value; }
            set
            {
                SetValue(value);

                if (OnValueChanged != null)
                    OnValueChanged(this, value);
            }
        }

        public float Ratio
        {
            get
            {
                // Div by zero, return something meaningful
                if (m_max == m_min)
                    return 0f;
                return (Value - m_min) / (m_max - m_min);
            }
        }

        public string Text
        {
            get { return m_text; }
            set { m_text = value; UpdateText(); }
        }

        // TODO: update to NewWidgets common style of events
        public Action<WidgetSlider, float> OnValueChanged;

        /// <summary>
        /// Creates a slider
        /// </summary>
        /// <param name="style"></param>
        public WidgetSlider(WidgetStyle style = default(WidgetStyle))
            : this(ElementType, style, 0.0f, 1.0f)
        {
        }

        /// <summary>
        /// Creates a slider
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public WidgetSlider(float min, float max)
            : this(ElementType, default(WidgetStyle), min, max)
        {
        }

        public WidgetSlider(WidgetStyle style, float min, float max)
            : this(ElementType, style, min, max)
        {
        }

        /// <summary>
        /// Creates a slider
        /// </summary>
        /// <param name="style"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        protected WidgetSlider(string elementType, WidgetStyle style, float min, float max)
            : base(elementType, style)
        {
            m_max = Math.Max(max, min);
            m_min = Math.Min(min, max);
            m_text = "{0:0%}";

            m_progressLine = new WidgetProgressLine(new WidgetStyle(new[] { LineClass },""));
            AddChild(m_progressLine);

            m_trackButton = new TrackingButton(new WidgetStyle(new[] { TrackerClass }, ""));
            m_trackButton.Position = new Vector2(0, m_progressLine.Size.Y - s_inset);
            m_trackButton.OnDrag += HandleDrag;
            AddChild(m_trackButton);

            m_label = new WidgetLabel(new WidgetStyle(new[] { LabelClass }, ""));
            AddChild(m_label);

            SetValue(m_min);
        }

        protected override void UpdateLayout()
        {
            base.UpdateLayout();

            m_progressLine.Relayout();

            if (Size.X <= 0 || Size.Y <= 0)
                Size = m_progressLine.Size + m_progressLine.GetProperty(WidgetParameterIndex.Padding, Margin.Empty).Size;

            m_progressLine.Position = m_progressLine.GetProperty(WidgetParameterIndex.Padding, Margin.Empty).TopLeft;

            m_trackButton.Relayout();

            m_trackButton.Position = new Vector2(m_progressLine.Size.X * Ratio + m_trackButton.Scale * m_trackButton.Size.X, 0) + m_trackButton.GetProperty(WidgetParameterIndex.Padding, Margin.Empty).TopLeft;

            m_label.Relayout();

            UpdateText();
        }

        private void SetValue(float value)
        {
            m_value = value;
            m_progressLine.SetProgress(Ratio);

            m_trackButton.Position = new Vector2(m_progressLine.Size.X * Ratio + m_trackButton.Scale * m_trackButton.Size.X, 0) + m_trackButton.GetProperty(WidgetParameterIndex.Padding, Margin.Empty).TopLeft;

            // Manually update the text
            UpdateText();
        }

        private void UpdateText()
        {
            m_label.Text = string.Format(m_text, m_value);
            m_label.Relayout();
            m_label.Position =
                m_trackButton.Position +
                new Vector2(m_trackButton.Scale * m_trackButton.Size.X /2 - m_label.Size.X / 2, m_trackButton.Scale * m_trackButton.Size.Y - 10.0f)
                + m_label.GetProperty(WidgetParameterIndex.Padding, Margin.Empty).TopLeft;
        }

        private float ClickToProgress(float x)
        {
            if (x < m_progressLine.Position.X)
                return m_min;
            else if (x >= m_progressLine.Position.X + m_progressLine.Size.X)
                return m_max;
            else
                return (x - m_progressLine.Position.X) * (m_max - m_min) / m_progressLine.Size.X + m_min;
        }

        private void HandleDrag(TrackingButton btn, Vector2 pos)
        {
            Value = ClickToProgress(pos.X);
        }


        public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            bool res = base.Touch(x, y, press, unpress, pointer);

            // Non-dragging behavior, advance to where the pointer is
            if (!res && (unpress || WindowController.Instance.IsTouchScreen))
            {
                Vector2 trans = Transform.GetClientPoint(new Vector2(x, y));
                Value = ClickToProgress(trans.X);
            }

            return res;
        }

        private class TrackingButton : WidgetImage
        {
            private bool m_dragged;

            public event Action<TrackingButton, Vector2> OnDrag;

            public bool Dragged
            {
                get { return m_dragged; }
            }

            public TrackingButton(WidgetStyle style)
                : base(style)
            {
            }

            public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
            {
                if (Enabled && press && !m_dragged)
                {
                    m_dragged = true;
                    WindowController.Instance.OnTouch += HandleGlobalTouch;
                }

                return base.Touch(x, y, press, unpress, pointer);
            }

            private bool HandleGlobalTouch(float x, float y, bool press, bool unpress, int pointer)
            {
                if (OnDrag != null && press)
                    OnDrag(this, Parent.Transform.GetClientPoint(new Vector2(x, y)));

                if (unpress)
                {
                    m_dragged = false;
                    WindowController.Instance.OnTouch -= HandleGlobalTouch;
                }

                // Return true to avoid interacting with anything else basically
                return true;
            }
        }
    }
}
