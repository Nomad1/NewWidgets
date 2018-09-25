using System.Numerics;
using NewWidgets.UI;
using NewWidgets.Widgets;

namespace NewWidgets.WinFormsSample
{
    /// <summary>
    /// Test window. Taken from Project Amalthea login dialog
    /// </summary>
    public class TestWindow : Window
    {
        private static string DefaultLogin = "login";
        private static string DefaultPassword = "password";

        private WidgetTextEdit m_loginEdit;
        private WidgetTextEdit m_passEdit;

        private WidgetTextEdit m_localEdit;
        private WidgetCheckBox m_localCheckBox;
        private WidgetButton m_loginButton;

        public TestWindow()
            : base(WindowFlags.None)
        {
            Size = new Vector2(2048, 2048.0f * WindowController.Instance.ScreenHeight / WindowController.Instance.ScreenWidth);
            Scale = WindowController.Instance.ScreenHeight / Size.Y;

            WidgetPanel panel = new WidgetPanel(WidgetManager.DefaultWindowStyle);
            panel.Size = new Vector2(600, 560);
            panel.Scale = WindowController.Instance.ScreenScale;
            panel.Position = Size / 2 - panel.Size * panel.Scale / 2;
            AddChild(panel);

            WidgetPanel back = new WidgetPanel(WidgetManager.GetStyle("back_pattern"));
            back.Size = panel.Size;
            panel.AddChild(back);

            WidgetLabel title = new WidgetLabel();
            title.Text = ResourceLoader.Instance.GetString("login_title");
            title.Size = new Vector2(panel.Size.X, 60);
            title.Position = new Vector2(0, 50);
            title.FontSize = WidgetManager.DefaultLabelStyle.FontSize * 1.5f;
            title.TextAlign = WidgetAlign.Top | WidgetAlign.HorizontalCenter;
            panel.AddChild(title);

            WidgetLabel loginLabel = new WidgetLabel();
            loginLabel.Text = ResourceLoader.Instance.GetString("login_login");
            loginLabel.Position = new Vector2(50, 160);
            loginLabel.FontSize = WidgetManager.DefaultLabelStyle.FontSize * 1.25f;
            panel.AddChild(loginLabel);

            m_loginEdit = new WidgetTextEdit();
            m_loginEdit.Text = DefaultLogin;
            m_loginEdit.Size = new Vector2(500, 45);
            m_loginEdit.Position = new Vector2(50, 200);
            m_loginEdit.FontSize = WidgetManager.DefaultLabelStyle.FontSize * 1.25f;
            m_loginEdit.OnTextEntered += HandleLoginEntered;
            panel.AddChild(m_loginEdit);
            m_loginEdit.SetFocused(true);

            WidgetLabel passLabel = new WidgetLabel();
            passLabel.Text = ResourceLoader.Instance.GetString("login_password");
            passLabel.Position = new Vector2(50, 260);
            passLabel.FontSize = WidgetManager.DefaultLabelStyle.FontSize * 1.25f;
            panel.AddChild(passLabel);

            m_passEdit = new WidgetTextEdit();
            m_passEdit.Text = DefaultPassword;
            m_passEdit.Size = new Vector2(500, 45);
            m_passEdit.Position = new Vector2(50, 300);
            m_passEdit.FontSize = WidgetManager.DefaultLabelStyle.FontSize * 1.25f;
            m_passEdit.MaskChar = '*';
            m_passEdit.OnTextEntered += HandlePassEntered;
            panel.AddChild(m_passEdit);
            m_passEdit.SetFocused(false);

            WidgetLabel localLabel = new WidgetLabel();
            localLabel.Text = ResourceLoader.Instance.GetString("login_local");
            localLabel.Position = new Vector2(90, 360);
            localLabel.Color = 0xcceeff;
            localLabel.FontSize = WidgetManager.DefaultLabelStyle.FontSize * 1.0f;
            panel.AddChild(localLabel);

            m_localCheckBox = new WidgetCheckBox();
            //m_localCheckBox.Size = new Vector2(30, 30);
            m_localCheckBox.Position = new Vector2(50, 360);
            m_localCheckBox.Checked = false;
            localLabel.Visible = true;
            m_localCheckBox.OnChecked += delegate (WidgetCheckBox cb)
            {
                m_localEdit.Visible = cb.Checked;
            };

            panel.AddChild(m_localCheckBox);

            m_localCheckBox.LinkedLabel = localLabel;

            m_localEdit = new WidgetTextEdit();
            m_localEdit.Text = "127.0.0.1";
            m_localEdit.Size = new Vector2(500, 45);
            m_localEdit.Position = new Vector2(50, 100);
            m_localEdit.FontSize = WidgetManager.DefaultLabelStyle.FontSize * 1.25f;
            m_localEdit.Visible = m_localCheckBox.Checked && m_localCheckBox.Visible;
            m_localEdit.OnTextEntered += delegate { HandleLoginPress(null); };
            panel.AddChild(m_localEdit);
            m_localEdit.SetFocused(false);


            WidgetButton webSiteButton = new WidgetButton(WidgetManager.GetStyle("text_button"), ResourceLoader.Instance.GetString("login_register"));
            webSiteButton.Position = new Vector2(50, 360 + (m_localCheckBox.Visible ? 40 : 0));
            webSiteButton.FontSize = WidgetManager.DefaultLabelStyle.FontSize * 1.0f;
            webSiteButton.OnPress += delegate { HandleWebSitePress(null); };
            panel.AddChild(webSiteButton);

            m_loginButton = new WidgetButton(ResourceLoader.Instance.GetString("login_connect"));
            m_loginButton.Size = new Vector2(160, 48);
            m_loginButton.FontSize = WidgetManager.DefaultLabelStyle.FontSize * 1.25f;
            m_loginButton.Position = new Vector2(panel.Size.X / 2 - m_loginButton.Size.X / 2, 460);
            m_loginButton.OnPress += delegate { HandleLoginPress(null); };
            panel.AddChild(m_loginButton);

        }

        private void HandleLoginEntered(WidgetTextEdit edit, string text)
        {
            if (text == string.Empty)
            {
                edit.Text = "";
                return;
            }

            m_passEdit.SetFocused(true);
        }

        private void HandlePassEntered(WidgetTextEdit edit, string text)
        {
            if (text == string.Empty)
            {
                edit.Text = "";
                return;
            }

            m_passEdit.SetFocused(false);
            HandleLoginPress(null);
        }

        private void HandleWebSitePress(object t)
        {
            DialogWindow dialog = DialogWindow.Show("@dialog_title",
                ResourceLoader.Instance.GetString("dialog_text", "пример текста"), "@button_yes", "@button_no");

            dialog.OnDialog += delegate (DialogWindow d, int index)
            {
                dialog.Close();
                if (index == 0)
                {
                    // Do something
                }
            };
        }

        private void HandleLoginPress(object t)
        {
            m_loginButton.Enabled = false;
        }

        public void Init()
        {
            m_loginEdit.SetFocused(true);
            m_loginButton.Enabled = true;
        }
    }
}

