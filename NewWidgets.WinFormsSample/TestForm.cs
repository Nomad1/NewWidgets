using System;using System.Windows.Forms;using System.Drawing;

using NewWidgets.Widgets;
using NewWidgets.WinForms;
using NewWidgets.UI;
using NewWidgets.Utility;
using NewWidgets.Sample;

namespace NewWidgets.WinFormsSample{    public partial class TestForm : Form    {        private readonly WinFormsController m_windowController;        private System.Threading.Timer m_updateTimer;        private readonly Delegate m_updateDelegate;        private TestWindow m_window;        private long m_lastFrameUpdate;
        private long m_lastFrameDraw;

        private float m_averageFrameUpdateTime;
        private float m_averageFrameTime;
        public TestForm()        {            InitializeComponent();            this.KeyPreview = true;
            this.perspectiveViewPictureBox.BackColor = Color.Black;
            perspectiveViewPictureBox.Paint += delegate { UpdateDrawFps(); };
            ResourceLoader.Instance.Language = "en-en";            ResourceLoader.Instance.RegisterString("login_title", "Connect to server");            ResourceLoader.Instance.RegisterString("login_login", "Login");
            ResourceLoader.Instance.RegisterString("login_password", "Password");
            ResourceLoader.Instance.RegisterString("login_local", "Custom server");
            ResourceLoader.Instance.RegisterString("login_register", "Register new account");
            ResourceLoader.Instance.RegisterString("login_connect", "Connect");            ResourceLoader.Instance.RegisterString("dialog_title", "Dialog");            ResourceLoader.Instance.RegisterString("dialog_text", "Dialog text that could be very long,\nwith |caaaaaadifferent|r |c336699colors|r, languages ({0}) and may even contain |tsettings_icon:64:64|t images.");            ResourceLoader.Instance.RegisterString("button_yes", "Yes");            ResourceLoader.Instance.RegisterString("button_no", "Yes!");            ResourceLoader.Instance.RegisterString("tooltip_connect", "Start connection");            m_windowController = new WinFormsController(perspectiveViewPictureBox.Width, perspectiveViewPictureBox.Height, 1.5f, 0.6f, false, "assets");
            m_windowController.OnInit += HandleOnInit;            m_windowController.RegisterSpriteAtlas("assets/font5.bin");            WidgetManager.LoadUI(System.IO.File.ReadAllText("assets/ui.xml"));

            this.perspectiveViewPictureBox.Init(m_windowController);
             
            m_updateDelegate = new Action(DoUpdate);

            const int targetFps = 60;
            m_averageFrameTime = m_averageFrameUpdateTime = 1000.0f / targetFps;
            m_lastFrameDraw = m_lastFrameUpdate = Environment.TickCount; // unprecise timer
        }

        protected override void OnHandleCreated(EventArgs e)
        {

            m_updateTimer = new System.Threading.Timer(delegate { BeginInvoke(m_updateDelegate); UpdateUpdateFps(); }, null, 500, (int)m_averageFrameTime);
            base.OnHandleCreated(e);
        }

        private void HandleOnInit()
        {            m_window = new TestWindow();            m_windowController.AddWindow(m_window);
        }        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            m_updateTimer.Dispose();
            Application.DoEvents(); // we need ti to make sure all timers has been finished
            base.OnFormClosing(e);
        }        protected override void OnKeyDown(KeyEventArgs e)        {            if (e.KeyCode == Keys.Q && (ModifierKeys & Keys.Alt) != 0)            {                Close();                return;            }
            e.SuppressKeyPress = ProcessKey(e.KeyCode, e.KeyValue, (ModifierKeys & Keys.Control) != 0, e.Shift, false);
            base.OnKeyDown(e);        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            e.SuppressKeyPress = ProcessKey(e.KeyCode, e.KeyValue, (ModifierKeys & Keys.Control) != 0, e.Shift, true);
            base.OnKeyUp(e);
        }

        private bool ProcessKey(Keys key, int value, bool control, bool shift, bool up)
        {
            if (m_windowController != null)
            {
                switch (key)
                {
                    case Keys.Left:
                        m_windowController.Key(SpecialKey.Left, up, "");
                        return true;
                    case Keys.Right:
                        m_windowController.Key(SpecialKey.Right, up, "");
                        return true;
                    case Keys.Up:
                        m_windowController.Key(SpecialKey.Up, up, "");
                        return true;
                    case Keys.Down:
                        m_windowController.Key(SpecialKey.Down, up, "");
                        return true;
                    case Keys.Space:
                        m_windowController.Key(SpecialKey.Select, up, " ");
                        return false;
                    case Keys.Enter:
                        m_windowController.Key(SpecialKey.Enter, up, "\n");
                        return true;
                    case Keys.Tab:
                        m_windowController.Key(SpecialKey.Tab, up, "\t");
                        return true;
                    case Keys.Delete:
                        m_windowController.Key(SpecialKey.Delete, up, "");
                        return true;
                    case Keys.Home:                        m_windowController.Key(SpecialKey.Home, up, "");                        return true;                    case Keys.End:                         m_windowController.Key(SpecialKey.End, up, "");
                        return true;
                    case Keys.Insert:                        if (shift)                             m_windowController.Key(SpecialKey.Paste, up, Clipboard.GetText());
                        return true;
                    case Keys.V: // never called. WinForms is a cruel beast (                        if (control)
                        {                            m_windowController.Key(SpecialKey.Paste, up, Clipboard.GetText());                            return true;
                         }                         break;
                    default:                        if (value == '\b')
                        {
                            if (control)                                m_windowController.Key(SpecialKey.EraseLine, up, "");                            else
                                m_windowController.Key(SpecialKey.Backspace, up, "");
                            return true;
                        }
                        break;
                }            }            return false;
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (m_windowController != null)
                m_windowController.Key(SpecialKey.Letter, true, e.KeyChar.ToString());

            base.OnKeyPress(e);
        }        private const int WM_PASTE = 0x0302;        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_PASTE)
            {
                if (m_windowController.Key(SpecialKey.Paste, false, Clipboard.GetText()))                    return;
            }

            base.WndProc(ref m);
        }

        private void DoUpdate()
        {
            if (m_windowController != null)
            {
                m_windowController.Update();
                m_window.SetFpsValue(1000.0f / m_averageFrameUpdateTime, 1000.0f / m_averageFrameTime);

                perspectiveViewPictureBox.Invalidate(); // total window repaint. Slow as hell in WinForms
            }
        }

        private void UpdateDrawFps()
        {
            long time = Environment.TickCount;

            int frameTime = (int)(time - m_lastFrameDraw);

            // calculate average frame time for last N frames
            int diminishing = 120 - 1;

            m_averageFrameTime = (m_averageFrameTime * diminishing + frameTime) / (float)(diminishing + 1);
            m_lastFrameDraw = time;
        }

        private void UpdateUpdateFps()
        {
            long time = Environment.TickCount;

            int frameTime = (int)(time - m_lastFrameUpdate);

            // calculate average frame time for last N frames
            int diminishing = 120 - 1;

            m_averageFrameUpdateTime = (m_averageFrameUpdateTime * diminishing + frameTime) / (float)(diminishing + 1);
            m_lastFrameUpdate = time;
        }

        class WindowControllerPaintBox : Control
        {
            private WinFormsController m_controller;

            public WindowControllerPaintBox()
            {
                this.DoubleBuffered = true;
            }

            public void Init(WinFormsController windowController)
            {
                m_controller = windowController;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                if (m_controller != null)
                    m_controller.Draw(e.Graphics);

                base.OnPaint(e);
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                if (m_controller != null)
                    m_controller.Touch(e.X, e.Y, true, false, (int)e.Button);
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                if (m_controller != null)
                    m_controller.Touch(e.X, e.Y, false, true, (int)e.Button);
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                if (m_controller != null)
                    m_controller.Touch(e.X, e.Y, false, false, (int)e.Button);
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                Focus();
            }

            protected override void OnMouseWheel(MouseEventArgs e)
            {
                if (m_controller != null)
                    m_controller.Zoom(e.X, e.Y, e.Delta);
            }

        }
    }    }