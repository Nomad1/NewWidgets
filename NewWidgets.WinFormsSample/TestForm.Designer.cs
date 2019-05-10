using System;
using System.Windows.Forms;

namespace NewWidgets.WinFormsSample
{
    partial class TestForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.perspectiveViewPictureBox = new System.Windows.Forms.PictureBox();
            this.updateTimer = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.perspectiveViewPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // perspectiveViewPictureBox
            // 
            this.perspectiveViewPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.perspectiveViewPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.perspectiveViewPictureBox.Location = new System.Drawing.Point(0, 0);
            this.perspectiveViewPictureBox.Name = "perspectiveViewPictureBox";
            this.perspectiveViewPictureBox.Size = new System.Drawing.Size(793, 597);
            this.perspectiveViewPictureBox.TabIndex = 0;
            this.perspectiveViewPictureBox.TabStop = false;
            this.perspectiveViewPictureBox.Paint += new System.Windows.Forms.PaintEventHandler(this.perspectiveView_Paint);
            this.perspectiveViewPictureBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.perspectivePictureBox_MouseDown);
            this.perspectiveViewPictureBox.MouseEnter += new System.EventHandler(this.perspectiveViewPictureBox_MouseEnter);
            this.perspectiveViewPictureBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.perspectivePictureBox_MouseMove);
            this.perspectiveViewPictureBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.perspectivePictureBox_MouseUp);
            // 
            // updateTimer
            // 
            this.updateTimer.Interval = 15;
            this.updateTimer.Tick += new System.EventHandler(this.updateTimer_Tick);
            // 
            // TestForm
            // 
            this.ClientSize = new System.Drawing.Size(793, 596);
            this.Controls.Add(this.perspectiveViewPictureBox);
            this.MinimumSize = new System.Drawing.Size(800, 500);
            this.Name = "TestForm";
            this.ShowIcon = false;
            this.Text = "NewWidgets Test Form";
            ((System.ComponentModel.ISupportInitialize)(this.perspectiveViewPictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.PictureBox perspectiveViewPictureBox;
        private Timer updateTimer;
    }
}

