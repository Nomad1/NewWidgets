using System;using System.Collections.Generic;using System.Drawing;using System.Windows.Forms;using System.IO;using System.ComponentModel;using System.Collections;
using System.Drawing.Imaging;
using System.Numerics;
using NewWidgets;
using NewWidgets.Widgets;

namespace NewWidgets.WinFormsSample{    public partial class TestForm : Form    {        private const int MaxWidth = 680;        private const int MaxHeight = 440;

        private Vector3 m_perspectiveViewShift;        private Vector3 m_perspectiveViewRotate;        private WinFormsController m_windowController;        public TestForm()        {            InitializeComponent();            this.perspectiveViewPictureBox.MouseWheel += perspectiveViewPictureBox_MouseWheel;            this.KeyPreview = true;
            zoomTrackBar.Visible = false;
            m_perspectiveViewRotate = Vector3.Zero;
            m_perspectiveViewShift = new Vector3(perspectiveViewPictureBox.Width / 2f, perspectiveViewPictureBox.Height / 2f, 1000);
            m_windowController = new WinFormsController(perspectiveViewPictureBox.Width, perspectiveViewPictureBox.Height, 1.0f, 1.0f, false, "assets");
            m_windowController.OnInit += HandleOnInit;            WidgetManager.Init(m_windowController.FontScale);            WidgetManager.LoadUI("assets/ui.xml");
        }

        private void HandleOnInit()
        {            m_windowController.Windows.Add(new TestWindow());
        }        protected override void OnKeyDown(KeyEventArgs e)        {            if (e.KeyCode == Keys.Q && (ModifierKeys & Keys.Alt) != 0)            {                Close();                return;            }            base.OnKeyDown(e);        }

        protected override void OnClosing(CancelEventArgs e)        {            base.OnClosing(e);        }
        #region Events

        private void zoomTrackBar_Scroll(object sender, EventArgs e)        {            perspectiveViewPictureBox.Invalidate();        }

        private void perspectiveViewPictureBox_MouseEnter(object sender, EventArgs e)
        {
            perspectiveViewPictureBox.Focus();
        }

        private void perspectiveViewPictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
            int zoom = zoomTrackBar.Value + e.Delta / 40;
            if (zoom < zoomTrackBar.Minimum)
                zoom = zoomTrackBar.Minimum;
            if (zoom > zoomTrackBar.Maximum)
                zoom = zoomTrackBar.Maximum;
            zoomTrackBar.Value = zoom;
            perspectiveViewPictureBox.Invalidate();
        }
        private void perspectiveView_Paint(object sender, PaintEventArgs e)        {            m_windowController.Draw(e.Graphics);            /*            Matrix4x4 transform = Matrix.Translation(m_perspectiveViewShift) * Matrix.Scale(zoomTrackBar.Value / 40f) *                                Matrix.RotationX(m_perspectiveViewRotate.X) * Matrix.RotationZ(m_perspectiveViewRotate.Z);            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;            e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;            DrawView(e.Graphics, transform, true);*/        }        private void perspectivePictureBox_MouseDown(object sender, MouseEventArgs e)        {
                   }
        private void perspectivePictureBox_MouseUp(object sender, MouseEventArgs e)        {                   }
        private void perspectivePictureBox_MouseMove(object sender, MouseEventArgs e)        {                }

        #endregion

    }}