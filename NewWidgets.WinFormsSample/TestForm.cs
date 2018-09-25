using System;using System.Windows.Forms;using System.Drawing;

using NewWidgets.Widgets;
using NewWidgets.WinForms;
using NewWidgets.UI;

namespace NewWidgets.WinFormsSample{    public partial class TestForm : Form    {        private WinFormsController m_windowController;        public TestForm()        {            InitializeComponent();            this.perspectiveViewPictureBox.MouseWheel += perspectiveViewPictureBox_MouseWheel;            this.KeyPreview = true;
            this.zoomTrackBar.Visible = false;            this.perspectiveViewPictureBox.BackColor = Color.Black;
            ResourceLoader loader = new ResourceLoader("en-en");            loader.RegisterString("login_title", "Connect to server");            loader.RegisterString("login_login", "Login");
            loader.RegisterString("login_password", "Password");
            loader.RegisterString("login_local", "Custom server");
            loader.RegisterString("login_register", "Register new account");
            loader.RegisterString("login_connect", "Connect");            loader.RegisterString("dialog_title", "Dialog");            loader.RegisterString("dialog_text", "Dialog text that could be very long,\nwith |caaaaaadifferent|r |c336699colors|r, languages ({0}) and may even contain |tsettings_icon:64:64|t images.");            loader.RegisterString("button_yes", "Yes");            loader.RegisterString("button_no", "Yes!");            m_windowController = new WinFormsController(perspectiveViewPictureBox.Width, perspectiveViewPictureBox.Height, 1.5f, 0.85f, false, "assets");
            m_windowController.OnInit += HandleOnInit;            m_windowController.RegisterSpriteAtlas("assets/font5.bin");            WidgetManager.LoadUI(System.IO.File.ReadAllText("assets/ui.xml"));

            updateTimer.Start();
        }
        
        private void HandleOnInit()
        {            m_windowController.Windows.Add(new TestWindow());
        }        protected override void OnKeyDown(KeyEventArgs e)        {            if (e.KeyCode == Keys.Q && (ModifierKeys & Keys.Alt) != 0)            {                Close();                return;            }

            ProcessKey(e.KeyCode, e.KeyValue, e.Control, false);
            base.OnKeyDown(e);        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            ProcessKey(e.KeyCode, e.KeyValue, e.Control, true);

            base.OnKeyUp(e);
        }

        private void ProcessKey(Keys key, int value, bool control, bool up)
        {
            if (m_windowController != null)
            {
                switch (key)
                {
                    case Keys.Left:
                        m_windowController.Key((int)SpecialKey.Left, up, '\0');
                        break;
                    case Keys.Right:
                        m_windowController.Key((int)SpecialKey.Right, up, '\0');
                        break;
                    case Keys.Up:
                        m_windowController.Key((int)SpecialKey.Up, up, '\0');
                        break;
                    case Keys.Down:
                        m_windowController.Key((int)SpecialKey.Down, up, '\0');
                        break;
                    case Keys.Space:
                        m_windowController.Key((int)SpecialKey.Select, up, ' ');
                        break;
                    case Keys.Enter:
                        m_windowController.Key((int)SpecialKey.Enter, up, '\n');
                        break;
                    case Keys.Tab:
                        m_windowController.Key((int)SpecialKey.Tab, up, '\t');
                        break;
                    case Keys.Delete:
                        m_windowController.Key((int)SpecialKey.Delete, up, '\0');
                        break;
                    default:

                        if (value == '\b')
                        {
                            m_windowController.Key((int)SpecialKey.Backspace, up, '\0');

                            if (control)
                                for (int i = 0; i < 31; i++)
                                    m_windowController.Key((int)SpecialKey.Backspace, up, '\0');
                            break;
                        }
                        break;
                }            }
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (m_windowController != null)
                m_windowController.Key((int)SpecialKey.Letter, true, (char)e.KeyChar);

            base.OnKeyPress(e);
        }

        #region Events

        private void zoomTrackBar_Scroll(object sender, EventArgs e)        {            perspectiveViewPictureBox.Invalidate();        }

        private void perspectiveViewPictureBox_MouseEnter(object sender, EventArgs e)
        {
            perspectiveViewPictureBox.Focus();
        }

        private void perspectiveViewPictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
           /* int zoom = zoomTrackBar.Value + e.Delta / 40;
            if (zoom < zoomTrackBar.Minimum)
                zoom = zoomTrackBar.Minimum;
            if (zoom > zoomTrackBar.Maximum)
                zoom = zoomTrackBar.Maximum;
            zoomTrackBar.Value = zoom;*/
            if (m_windowController != null)
                m_windowController.Zoom(e.X, e.Y, e.Delta);
        }
        private void perspectiveView_Paint(object sender, PaintEventArgs e)        {            Graphics g = e.Graphics;

            
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;

            if (m_windowController != null)
                m_windowController.Draw(g);        }        private void perspectivePictureBox_MouseDown(object sender, MouseEventArgs e)        {
            if (m_windowController != null)                m_windowController.Touch(e.X, e.Y, true, false, (int)e.Button);        }
        private void perspectivePictureBox_MouseUp(object sender, MouseEventArgs e)        {
            if (m_windowController != null)                m_windowController.Touch(e.X, e.Y, false, true, (int)e.Button);        }
        private void perspectivePictureBox_MouseMove(object sender, MouseEventArgs e)        {
            if (m_windowController != null)                m_windowController.Touch(e.X, e.Y, false, false, (int)e.Button);        }

        private void updateTimer_Tick(object sender, EventArgs e)
        {
            if (m_windowController != null)
            {
                m_windowController.Update();
                perspectiveViewPictureBox.Invalidate();
            }
        }

        #endregion
    }}