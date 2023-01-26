namespace Sandaab.WindowsApp.Forms
{
    partial class RemoteControlForm
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
            this.pnlRemoteScreen = new System.Windows.Forms.Panel();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.SuspendLayout();
            // 
            // pnlRemoteScreen
            // 
            this.pnlRemoteScreen.AutoScroll = true;
            this.pnlRemoteScreen.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlRemoteScreen.Location = new System.Drawing.Point(0, 0);
            this.pnlRemoteScreen.Name = "pnlRemoteScreen";
            this.pnlRemoteScreen.Size = new System.Drawing.Size(220, 450);
            this.pnlRemoteScreen.TabIndex = 0;
            this.pnlRemoteScreen.Scroll += new System.Windows.Forms.ScrollEventHandler(this.PnlRemoteScreen_Scroll);
            this.pnlRemoteScreen.Resize += new System.EventHandler(this.PnlRemoteScreen_Resize);
            // 
            // RemoteControlForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(220, 450);
            this.Controls.Add(this.pnlRemoteScreen);
            this.Name = "RemoteControlForm";
            this.Text = "RemoveControlForm";
            this.ResumeLayout(false);

        }

        #endregion

        private Panel pnlRemoteScreen;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
    }
}