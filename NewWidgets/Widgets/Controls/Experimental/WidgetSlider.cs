using System;
using System.Numerics;
using NewWidgets.Widgets;
using NewWidgets.UI;

namespace NewWidgets.Widgets
{
    /// <summary>
    /// Draggable slider widget
    /// </summary>
    public class WidgetSlider : WidgetPanel
    {
        const float s_height = 10f;
        // TODO: make configurable
        const float s_width = 250f;
        const float s_inset = 15f;

        private readonly WidgetProgressLine m_progressLine;
        private readonly DraggableButton m_lowerButton;
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

        public WidgetSlider(float max = 1.00f, float min = 0.0f)
            : base(Widget.DefaultStyle)
        {
            m_max = max;
            m_min = min;
            m_text = "{0:0%}";

            m_progressLine = new WidgetProgressLine();
            m_progressLine.Size = new Vector2(s_width, s_height);
            AddChild(m_progressLine);

            m_lowerButton = new DraggableButton(WidgetManager.GetStyle("left_button"));
            m_lowerButton.Rotation = 90;
            m_lowerButton.Scale = 0.75f;
            m_lowerButton.Position = new Vector2(0, m_progressLine.Size.Y - s_inset);
            m_lowerButton.OnDrag += HandleDrag;
            AddChild(m_lowerButton);

            m_label = new WidgetLabel("0");
            m_label.TextAlign = WidgetAlign.Top | WidgetAlign.VerticalCenter;
            m_label.FontSize = WidgetManager.FontScale * 0.7f;
            m_label.Color = 0xffffff;
            m_label.Relayout();
            AddChild(m_label);

            // Shift progress line to get room for the button to stick out a little
            m_progressLine.Position = new Vector2(m_lowerButton.Size.X * m_lowerButton.Scale / 2, 0);

            Size = new Vector2(m_progressLine.Size.X + m_lowerButton.Size.X * m_lowerButton.Scale,
                               m_progressLine.Size.Y + m_lowerButton.Size.Y * m_lowerButton.Scale + m_label.Size.Y - s_inset);

            SetValue(m_min);
        }

        private void SetValue(float value)
        {
            m_value = value;
            m_progressLine.SetProgress(Ratio);

            m_lowerButton.Position = new Vector2(m_progressLine.Size.X * Ratio + m_lowerButton.Scale * m_lowerButton.Size.X, m_lowerButton.Position.Y);

            // Manually update the text
            UpdateText();
        }

        private void UpdateText()
        {
            //m_progressLine.Text = string.Format(m_text, m_value);
            m_label.Text = string.Format(m_text, m_value);
            m_label.Relayout();
            m_label.Position = m_lowerButton.Position + new Vector2(m_lowerButton.Scale * m_lowerButton.Size.X /2 - m_label.Size.X / 2, m_lowerButton.Scale * m_lowerButton.Size.Y - 10.0f);
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

        private void HandleDrag(DraggableButton btn, Vector2 pos)
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

        private class DraggableButton : WidgetButton
        {
            private bool m_dragged;

            public event Action<DraggableButton, Vector2> OnDrag;

            public bool Dragged
            {
                get { return m_dragged; }
            }

            public DraggableButton(WidgetStyleSheet style) : base(style)
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
