using System;
using System.Numerics;
using NewWidgets.UI;
using NewWidgets.Widgets;
using NewWidgets.Utility;

namespace NewWidgets.WinFormsSample
{
    /// <summary>
    /// Dialog window. Taken from Project Amalthea login dialog
    /// </summary>
    public class DialogWindow : Window
    {
        private static readonly int AppearDuration = 500;
        private static readonly float s_dragEpsilon = 10.0f;

        protected WidgetPanel m_panel;

        private WidgetLabel m_title;
        private WidgetText m_text;

        private WidgetButton m_closeButton;
        private WidgetButton m_yesButton;
        private WidgetButton m_noButton;

        //private WidgetImage m_image;

        private Vector2 m_dragShift;
        private Vector2 m_dragStart;
        private bool m_dragging;

        private bool m_fired;

        public event Action<DialogWindow, int> OnDialog;

        protected DialogWindow(string title, string text, string yesText, string noText)
            : base(WindowFlags.CustomAnim | WindowFlags.Blackout)
        {
            m_panel = new WidgetWindow(WidgetManager.GetStyle("dialog_window"));
            Vector2 size = m_panel.Size;

            m_text = new WidgetText();
            m_text.Text = text;
            m_text.Size = new Vector2(size.X, 0);
            m_text.MaxWidth = size.X - 40;
            m_text.FontSize *= 1.25f;
            m_text.TextAlign = WidgetAlign.VerticalCenter | WidgetAlign.HorizontalCenter;
            m_text.Relayout();
            size = m_panel.Size = m_panel.Size + new Vector2(0, m_text.Size.Y - 80);
            m_text.Position = new Vector2(0, size.Y / 2 - m_text.Size.Y / 2);
            m_panel.AddChild(m_text);

            Size = size;

            if (!string.IsNullOrEmpty(noText))
            {
                m_noButton = new WidgetButton(noText);
                m_noButton.Size = new Vector2(128, 48);

                m_noButton.Position = new Vector2(size.X * 2 / 3 - (m_noButton.Size.X) / 2, size.Y - m_noButton.Size.Y - 25);
                m_noButton.OnPress += delegate { HandleButtonPress(1); };
                m_panel.AddChild(m_noButton);
            }

            m_yesButton = new WidgetButton(yesText);
            m_yesButton.Size = new Vector2(128, 48);
            m_yesButton.Position = new Vector2(m_noButton == null ? size.X / 2 - (m_yesButton.Size.X) / 2 : size.X / 3 - m_yesButton.Size.X * 0.25f, size.Y - m_yesButton.Size.Y - 25);
            m_yesButton.OnPress += delegate { HandleButtonPress(0); };
            m_panel.AddChild(m_yesButton);

            m_closeButton = new WidgetButton(WidgetManager.GetStyle("image_button"), "");
            m_closeButton.Size = new Vector2(40, 40);
            m_closeButton.Image = "close_icon";
            m_closeButton.ImagePadding = new Margin(10, 10, 10, 10);
            m_closeButton.Position = new Vector2(size.X - m_closeButton.Size.X, 0);
            m_closeButton.OnPress += delegate { HandleButtonPress(2); };
            m_panel.AddChild(m_closeButton);

            m_title = new WidgetLabel();
            m_title.Text = title;
            m_title.Size = new Vector2(size.X, 50);
            m_title.Position = new Vector2(0, 20);
            m_title.FontSize *= 1.5f;
            m_title.TextAlign = WidgetAlign.Top | WidgetAlign.HorizontalCenter;
            m_panel.AddChild(m_title);

            AddChild(m_panel);

        }

        private void HandleButtonPress(int index)
        {
            if (OnDialog != null && !m_fired)
            {
                m_fired = true;
                OnDialog(this, index);
            }
        }

        public void Close()
        {
            Remove();
        }

        public override void Remove()
        {
            WindowController.Instance.ScheduleAction(base.Remove, AppearDuration);

            OnRemove();
        }

        public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            if (!Controlling)
                return false;

            if (base.Touch(x, y, press, unpress, pointer))
                return true;

            bool hit = this.HitTest(x, y);
            Vector2 local = new Vector2(x, y);//this.Sprite.Transform.GetClientPoint(new Vector2(x, y));

            if (!m_dragging && press && hit)
            {
                m_dragShift = local;
                m_dragStart = Position;
                m_dragging = true;
                return true;
            }

            if (m_dragging && (unpress || !hit))
            {
                m_dragging = false;
                if ((m_dragStart - m_dragShift).LengthSquared() > s_dragEpsilon)
                {
                    return true;
                }
            }

            if (m_dragging)
            {
                Vector2 move = local - m_dragShift;

                if (move.LengthSquared() > 0)
                    Position = m_dragStart + move;
                return true;
            }

            return false;
        }

        public static DialogWindow Show(string title, string text, string yesText, string noText = "")
        {
            DialogWindow dialog = new DialogWindow(title, text, yesText, noText);
            dialog.Scale = WindowController.Instance.ScreenScale * WindowController.Instance.ScreenWidth / 2048.0f;
            dialog.OnAppear();

            WindowController.Instance.ScheduleAction(() => WindowController.Instance.AddWindow(dialog), 1);

            return dialog;
        }

        protected void OnAppear()
        {
            Controlling = false;
            Vector2 targetPosition = (new Vector2(WindowController.Instance.ScreenWidth, WindowController.Instance.ScreenHeight) - Size * Scale) / 2;
            Position = new Vector2(targetPosition.X, -Size.Y / Scale);
            Move(targetPosition, AppearDuration / 2, () => Controlling = true);
        }

        protected void OnRemove()
        {
            HandleButtonPress(1);
            Move(new Vector2(WindowController.Instance.ScreenWidth, Position.Y), AppearDuration, null);
        }

        public override bool Key(SpecialKey key, bool up, string keyString)
        {
            if (key == SpecialKey.Back)
            {
                HandleButtonPress(1);
                return true;
            }
            else
                if (key == SpecialKey.Enter)
            {
                HandleButtonPress(0);
                return true;
            }

            return base.Key(key, up, keyString);
        }
    }
}

