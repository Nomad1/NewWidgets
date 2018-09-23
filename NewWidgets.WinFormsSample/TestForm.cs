using System;using System.Windows.Forms;using System.ComponentModel;using System.Drawing;

using NewWidgets.Widgets;
using NewWidgets.WinForms;

namespace NewWidgets.WinFormsSample{    public partial class TestForm : Form    {        private const int MaxWidth = 680;        private const int MaxHeight = 440;

        private WinFormsController m_windowController;        public TestForm()        {            InitializeComponent();            this.perspectiveViewPictureBox.MouseWheel += perspectiveViewPictureBox_MouseWheel;            this.KeyPreview = true;
            this.zoomTrackBar.Visible = false;            this.perspectiveViewPictureBox.BackColor = Color.Black;
            ResourceLoader loader = new ResourceLoader("en-en");            loader.RegisterString("login_title", "Connect to server");            m_windowController = new WinFormsController(perspectiveViewPictureBox.Width, perspectiveViewPictureBox.Height, 0.75f, 0.75f, false, "assets");
            m_windowController.OnInit += HandleOnInit;            m_windowController.RegisterSpriteAtlas("assets/font5.bin");            WidgetManager.LoadUI(System.IO.File.ReadAllText("assets/ui.xml"));

            updateTimer.Start();
        }

        private void HandleOnInit()
        {            m_windowController.Windows.Add(new TestWindow());
        }        protected override void OnKeyDown(KeyEventArgs e)        {            if (e.KeyCode == Keys.Q && (ModifierKeys & Keys.Alt) != 0)            {                Close();                return;            }            if (m_windowController != null)                m_windowController.Key(0, false, (char)e.KeyValue);            base.OnKeyDown(e);        }

        protected override void OnClosing(CancelEventArgs e)        {            base.OnClosing(e);        }
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

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

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