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
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.zoomTrackBar = new System.Windows.Forms.TrackBar();
            this.perspectiveViewPictureBox = new System.Windows.Forms.PictureBox();
            this.updateTimer = new System.Windows.Forms.Timer(this.components);
            this.statusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.zoomTrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.perspectiveViewPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 574);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(793, 22);
            this.statusStrip.TabIndex = 2;
            this.statusStrip.Text = "statusStrip";
            // 
            // statusLabel
            // 
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // zoomTrackBar
            // 
            this.zoomTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.zoomTrackBar.AutoSize = false;
            this.zoomTrackBar.Location = new System.Drawing.Point(402, 574);
            this.zoomTrackBar.Maximum = 80;
            this.zoomTrackBar.Minimum = 1;
            this.zoomTrackBar.Name = "zoomTrackBar";
            this.zoomTrackBar.Size = new System.Drawing.Size(134, 21);
            this.zoomTrackBar.TabIndex = 5;
            this.zoomTrackBar.TickFrequency = 25;
            this.zoomTrackBar.Value = 10;
            this.zoomTrackBar.Scroll += new System.EventHandler(this.zoomTrackBar_Scroll);
            // 
            // perspectiveViewPictureBox
            // 
            this.perspectiveViewPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.perspectiveViewPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.perspectiveViewPictureBox.Location = new System.Drawing.Point(0, 0);
            this.perspectiveViewPictureBox.Margin = new System.Windows.Forms.Padding(0, 0, 256, 0);
            this.perspectiveViewPictureBox.Name = "perspectiveViewPictureBox";
            this.perspectiveViewPictureBox.Size = new System.Drawing.Size(793, 574);
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
            this.updateTimer.Interval = 16;
            this.updateTimer.Tick += new System.EventHandler(this.updateTimer_Tick);
            // 
            // TestForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(793, 596);
            this.Controls.Add(this.zoomTrackBar);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.perspectiveViewPictureBox);
            this.MinimumSize = new System.Drawing.Size(800, 500);
            this.Name = "TestForm";
            this.ShowIcon = false;
            this.Text = "NewWidgets Test Form";
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.zoomTrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.perspectiveViewPictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.TrackBar zoomTrackBar;
        private System.Windows.Forms.PictureBox perspectiveViewPictureBox;
        private Timer updateTimer;
    }
}

