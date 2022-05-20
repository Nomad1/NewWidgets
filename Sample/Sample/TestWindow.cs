using System.Numerics;
using NewWidgets.UI;
using NewWidgets.Utility;
using NewWidgets.Widgets;

namespace NewWidgets.Sample
{
    /// <summary>
    /// Test window. Taken from Project Amalthea login dialog
    /// </summary>
    public class TestWindow : Window
    {
        private static readonly string DefaultLogin = "login";
        private static readonly string DefaultPassword = "password";

        private readonly WidgetTextEdit m_loginEdit;
        private readonly WidgetTextEdit m_passEdit;

        private readonly WidgetTextEdit m_localEdit;
        private readonly WidgetCheckBox m_localCheckBox;
        private readonly WidgetButton m_loginButton;

        private readonly WidgetLabel m_fpsLabel;

        static TestWindow()
        {
            ResourceLoader.Instance.Language = "en-en";
            ResourceLoader.Instance.RegisterString("login_title", "Connect to server");
            ResourceLoader.Instance.RegisterString("login_login", "Login");
            ResourceLoader.Instance.RegisterString("login_password", "Password");
            ResourceLoader.Instance.RegisterString("login_local", "Custom server");
            ResourceLoader.Instance.RegisterString("login_register", "Register new account");
            ResourceLoader.Instance.RegisterString("login_connect", "Connect");
            ResourceLoader.Instance.RegisterString("dialog_title", "Dialog");
            ResourceLoader.Instance.RegisterString("dialog_text", "Dialog text that could be very long,\nwith |caaaaaadifferent|r |c336699colors|r, languages ({0}) and may even contain |tsettings_icon:64:64|t images.");
            ResourceLoader.Instance.RegisterString("button_yes", "Yes");
            ResourceLoader.Instance.RegisterString("button_no", "Yes!");
            ResourceLoader.Instance.RegisterString("tooltip_connect", "Start connection");
        }

        public TestWindow()
            : base(WindowFlags.None)
        {
            Size = new Vector2(2048, 2048.0f * WindowController.Instance.ScreenHeight / WindowController.Instance.ScreenWidth);
            Scale = WindowController.Instance.ScreenHeight / Size.Y;

            WidgetPanel panel = new WidgetWindow();
            panel.Size = new Vector2(600, 760);
            panel.Scale = WindowController.Instance.UIScale;
            panel.Position = Size / 2 - panel.Size * panel.Scale / 2;
            AddChild(panel);

            WidgetPanel back = new WidgetPanel(WidgetManager.GetStyle("back_pattern"));
            back.Size = panel.Size;
            panel.AddChild(back);

            WidgetLabel title = new WidgetLabel();
            title.Text = ResourceLoader.Instance.GetString("login_title");
            title.Size = new Vector2(panel.Size.X, 60);
            title.Position = new Vector2(0, 50);
            title.FontSize = WidgetManager.FontScale * 1.5f;
            title.TextAlign = WidgetAlign.Top | WidgetAlign.HorizontalCenter;
            panel.AddChild(title);

            WidgetLabel loginLabel = new WidgetLabel();
            loginLabel.Text = ResourceLoader.Instance.GetString("login_login");
            loginLabel.Position = new Vector2(50, 160);
            loginLabel.FontSize = WidgetManager.FontScale * 1.25f;
            panel.AddChild(loginLabel);

            m_loginEdit = new WidgetTextEdit();
            m_loginEdit.Text = DefaultLogin;
            m_loginEdit.Size = new Vector2(500, 45);
            m_loginEdit.Position = new Vector2(50, 200);
            m_loginEdit.FontSize = WidgetManager.FontScale * 1.25f;
            m_loginEdit.OnTextEntered += HandleLoginEntered;
            panel.AddChild(m_loginEdit);
            m_loginEdit.SetFocused(true);

            WidgetLabel passLabel = new WidgetLabel();
            passLabel.Text = ResourceLoader.Instance.GetString("login_password");
            passLabel.Position = new Vector2(50, 260);
            passLabel.FontSize = WidgetManager.FontScale * 1.25f;
            panel.AddChild(passLabel);

            m_passEdit = new WidgetTextEdit();
            m_passEdit.Text = DefaultPassword;
            m_passEdit.Size = new Vector2(500, 45);
            m_passEdit.Position = new Vector2(50, 300);
            m_passEdit.FontSize = WidgetManager.FontScale * 1.25f;
            m_passEdit.MaskChar = "*";
            m_passEdit.OnTextEntered += HandlePassEntered;
            panel.AddChild(m_passEdit);
            m_passEdit.SetFocused(false);

            WidgetLabel localLabel = new WidgetLabel();
            localLabel.Text = ResourceLoader.Instance.GetString("login_local");
            localLabel.Position = new Vector2(90, 360);
            localLabel.Color = 0xcceeff;
            localLabel.FontSize = WidgetManager.FontScale * 1.0f;
            panel.AddChild(localLabel);

            m_localCheckBox = new WidgetCheckBox(true);
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
            m_localEdit.FontSize = WidgetManager.FontScale * 1.25f;
            m_localEdit.Visible = m_localCheckBox.Checked && m_localCheckBox.Visible;
            m_localEdit.OnTextEntered += delegate { HandleLoginPress(null); };
            m_localEdit.OnValidateInput += HandleValidateIpInput;
            panel.AddChild(m_localEdit);
            m_localEdit.SetFocused(false);


            WidgetButton webSiteButton = new WidgetButton(WidgetManager.GetStyle("text_button"), ResourceLoader.Instance.GetString("login_register"));
            webSiteButton.Position = new Vector2(50, 360 + (m_localCheckBox.Visible ? 40 : 0));
            webSiteButton.FontSize = WidgetManager.FontScale * 1.0f;
            webSiteButton.OnPress += delegate { HandleWebSitePress(null); };
            panel.AddChild(webSiteButton);

            m_loginButton = new WidgetButton(ResourceLoader.Instance.GetString("login_connect"));
            m_loginButton.Size = new Vector2(160, 48);
            m_loginButton.FontSize = WidgetManager.FontScale * 1.25f;
            m_loginButton.Position = new Vector2(panel.Size.X / 2 - m_loginButton.Size.X / 2, 460);
            m_loginButton.Enabled = false;
            m_loginButton.OnPress += delegate { HandleLoginPress(null); };
            m_loginButton.Tooltip = "@tooltip_connect";
            panel.AddChild(m_loginButton);

            WidgetImage logoImage = new WidgetImage(WidgetBackgroundStyle.ImageFit, "settings_icon");
            logoImage.Size = new Vector2(64, 64);
            logoImage.Position = new Vector2(20, 15);
            panel.AddChild(logoImage);


            WidgetTextField textTextField = new WidgetTextField();// WidgetManager.GetStyle("default_textedit"));
            textTextField.Size = new Vector2(500, 225);
            textTextField.Position = new Vector2(50, 520);
            textTextField.FontSize = WidgetManager.FontScale * 1.25f;
            textTextField.Visible = true;
            textTextField.Text = "WidgetTextField textTextField = new WidgetTextField(WidgetManager.GetStyle(\"default_textedit\"));\n            textTextField.Size = new Vector2(500, 225);\n            textTextField.Position = new Vector2(50, 520);\n            textTextField.FontSize = WidgetManager.FontScale * 1.25f;\n            textTextField.Visible = true";
            panel.AddChild(textTextField);

            m_fpsLabel = new WidgetLabel();
            m_fpsLabel.Text = "1";
            m_fpsLabel.Position = new Vector2(440, 20);
            m_fpsLabel.FontSize = WidgetManager.FontScale * 0.75f;
            panel.AddChild(m_fpsLabel);
        }

        private bool HandleValidateIpInput(string oldText, string input)
        {
            if (input.Length == 1 && !char.IsDigit(input[0]) && input[0] != '.')
                return false;
            return true;
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

        public void SetFpsValue(float updateFps, float drawFps)
        {
            m_fpsLabel.Text = string.Format("FPS: {0:F1}/{1:F1}", drawFps, updateFps);
        }
    }
}