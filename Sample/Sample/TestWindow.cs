using System.IO;
using System.Numerics;
using NewWidgets.UI;
using NewWidgets.Utility;
using NewWidgets.Widgets;

namespace NewWidgets.Sample
{
    /// <summary>
    /// Test window. Taken from Project Amalthea login dialog.
    ///
    /// The dialog itself is <c>assets/login.xhtml</c> and its geometry is
    /// <c>assets/login.css</c>, which the document links. Nothing here builds a control or
    /// computes a position: the document is loaded, every control is found by the same
    /// <c>#id</c> the stylesheet names it by, and what is left is behaviour -- the event
    /// handlers -- plus the handful of things below that no CSS property can say yet.
    ///
    /// What could not leave code, and what each one waits on:
    ///
    /// 1. The panel's own centring and <c>Scale</c>. A Window is not a Widget, so this panel's
    ///    containing block is the screen rather than the 2048-unit box it is really placed in,
    ///    and Scale sits outside the box model. left/right plus auto margins cannot say this
    ///    until a Window is a containing block.
    /// 2. The six font sizes. <c>font-size</c> is a plain scale factor here, and every one of
    ///    these is a multiple of <see cref="WidgetManager.FontScale"/>, which is a runtime
    ///    number the host chooses -- 0.5 in the RunMobile sample, 1.0 under the test runner.
    ///    A literal in the stylesheet would pin the dialog to one host's scale.
    /// 3. The title's <c>TextAlign</c>. CSS <c>text-align: center</c> carries the Top and Bottom
    ///    bits of <see cref="WidgetAlign"/> as well, so it would centre the text vertically too
    ///    and there is no vertical-align property to undo that. See Test 37.
    /// 4. Focus, and <c>#local_edit</c> starting hidden. Both are runtime state rather than
    ///    document content, and the <c>visibility</c> property is parsed and dropped today.
    ///
    /// Everything else the dialog used to say in code is now in the document, the text content
    /// included: an HTML editor puts the words in the element, so that is where they live.
    /// </summary>
    public class TestWindow : Window
    {
        private const string DocumentName = "login.xhtml";
        private const string DefaultAssetPath = "assets";

        private readonly string m_assetPath;

        private readonly WidgetTextEdit m_loginEdit;
        private readonly WidgetTextEdit m_passEdit;

        private readonly WidgetTextEdit m_localEdit;
        private readonly WidgetCheckBox m_localCheckBox;
        private readonly WidgetButton m_loginButton;

        private readonly WidgetLabel m_fpsLabel;

        static TestWindow()
        {
            ResourceLoader.Instance.Language = "en-en";
            ResourceLoader.Instance.RegisterString("dialog_title", "Dialog");
            ResourceLoader.Instance.RegisterString("dialog_text", "Dialog text that could be very long,\nwith |caaaaaadifferent|r |c336699colors|r, languages ({0}) and may even contain |tsettings_icon:64:64|t images.");
            ResourceLoader.Instance.RegisterString("button_yes", "Yes");
            ResourceLoader.Instance.RegisterString("button_no", "Yes!");
            ResourceLoader.Instance.RegisterString("tooltip_connect", "Start connection");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Sample.TestWindow"/> class.
        /// </summary>
        /// <param name="assetPath">Folder holding login.xhtml and the stylesheets it links.
        /// Every sample runs from a folder with an <c>assets</c> beside the executable, which is
        /// the default; the test runner reaches the same files from its own working directory</param>
        public TestWindow(string assetPath = null)
            : base(WindowFlags.None)
        {
            m_assetPath = string.IsNullOrEmpty(assetPath) ? DefaultAssetPath : assetPath;

            Size = new Vector2(2048, 2048.0f * WindowController.Instance.ScreenHeight / WindowController.Instance.ScreenWidth);
            Scale = WindowController.Instance.ScreenHeight / Size.Y;

            WidgetManager.LoadXHTML(File.ReadAllText(Path.Combine(m_assetPath, DocumentName)), LoadStyleSheet, this);

            WidgetPanel panel;

            if (!WidgetPanel.TryFind(this, "login_window", out panel))
                throw new WidgetException(DocumentName + " has no #login_window, so there is no dialog to bind");

            panel.Scale = WindowController.Instance.UIScale;
            panel.Relayout(); // #login_window gives the 600x760 the centring below reads

            // Stays in code, see 1 above
            panel.Position = Size / 2 - panel.Size * panel.Scale / 2;

            WidgetLabel title = panel.Find<WidgetLabel>("login_title");
            title.FontSize = WidgetManager.FontScale * 1.5f;
            title.TextAlign = WidgetAlign.Top | WidgetAlign.HorizontalCenter; // stays in code, see 3 above

            panel.Find<WidgetLabel>("login_label").FontSize = WidgetManager.FontScale * 1.25f;
            panel.Find<WidgetLabel>("pass_label").FontSize = WidgetManager.FontScale * 1.25f;
            panel.Find<WidgetLabel>("local_label").FontSize = WidgetManager.FontScale * 1.0f;

            m_loginEdit = panel.Find<WidgetTextEdit>("login_edit");
            m_loginEdit.FontSize = WidgetManager.FontScale * 1.25f;
            m_loginEdit.OnTextEntered += HandleLoginEntered;
            m_loginEdit.SetFocused(true);

            m_passEdit = panel.Find<WidgetTextEdit>("pass_edit");
            m_passEdit.FontSize = WidgetManager.FontScale * 1.25f;
            m_passEdit.OnTextEntered += HandlePassEntered;
            m_passEdit.SetFocused(false);

            m_localCheckBox = panel.Find<WidgetCheckBox>("local_check");
            m_localCheckBox.OnChecked += HandleLocalChecked;

            m_localEdit = panel.Find<WidgetTextEdit>("local_edit");
            m_localEdit.FontSize = WidgetManager.FontScale * 1.25f;
            m_localEdit.Visible = m_localCheckBox.Checked && m_localCheckBox.Visible; // stays in code, see 4 above
            m_localEdit.OnTextEntered += HandleLocalEntered;
            m_localEdit.OnValidateInput += HandleValidateIpInput;
            m_localEdit.SetFocused(false);

            panel.Find<WidgetButton>("website_button").OnPress += HandleWebSitePress;

            m_loginButton = panel.Find<WidgetButton>("login_button");
            m_loginButton.OnPress += HandleLoginPress;

            panel.Find<WidgetTextField>("text_field").FontSize = WidgetManager.FontScale * 1.25f;

            m_fpsLabel = panel.Find<WidgetLabel>("fps_label");
            m_fpsLabel.FontSize = WidgetManager.FontScale * 0.75f;
        }

        /// <summary>
        /// Resolves a <c>&lt;link rel="stylesheet"&gt;</c> against the folder the document came
        /// from, which is where an HTML editor looks for it too
        /// </summary>
        private string LoadStyleSheet(string href)
        {
            string path = Path.Combine(m_assetPath, href);

            if (!File.Exists(path))
                return null; // logged by the loader rather than thrown, like every other miss

            return File.ReadAllText(path);
        }

        private void HandleLocalChecked(WidgetCheckBox checkBox)
        {
            m_localEdit.Visible = checkBox.Checked;
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

        private void HandleLocalEntered(WidgetTextEdit edit, string text)
        {
            HandleLoginPress(null);
        }

        private void HandleWebSitePress(WidgetButton sender)
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

        private void HandleLoginPress(WidgetButton sender)
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
