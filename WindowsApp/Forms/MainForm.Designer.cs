namespace Sandaab.WindowsApp.Forms
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.button1 = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnRemoteControl = new System.Windows.Forms.Button();
            this.lvDevices = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.mnuDevices = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mnuDevicesUnpair = new System.Windows.Forms.ToolStripMenuItem();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.lblConnected = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.mnuDevices.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(340, 87);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(83, 25);
            this.button1.TabIndex = 0;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.Btn1_Click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.lblConnected);
            this.panel1.Controls.Add(this.btnRemoteControl);
            this.panel1.Controls.Add(this.lvDevices);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(189, 339);
            this.panel1.TabIndex = 1;
            // 
            // btnRemoteControl
            // 
            this.btnRemoteControl.AutoSize = true;
            this.btnRemoteControl.Location = new System.Drawing.Point(27, 269);
            this.btnRemoteControl.Name = "btnRemoteControl";
            this.btnRemoteControl.Size = new System.Drawing.Size(125, 27);
            this.btnRemoteControl.TabIndex = 2;
            this.btnRemoteControl.Text = "btnRemoteControl";
            this.btnRemoteControl.UseVisualStyleBackColor = true;
            this.btnRemoteControl.Click += new System.EventHandler(this.btnRemoteControl_Click);
            // 
            // lvDevices
            // 
            this.lvDevices.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.lvDevices.ContextMenuStrip = this.mnuDevices;
            this.lvDevices.Dock = System.Windows.Forms.DockStyle.Top;
            this.lvDevices.FullRowSelect = true;
            this.lvDevices.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.lvDevices.Location = new System.Drawing.Point(0, 0);
            this.lvDevices.MultiSelect = false;
            this.lvDevices.Name = "lvDevices";
            this.lvDevices.Size = new System.Drawing.Size(189, 26);
            this.lvDevices.TabIndex = 1;
            this.lvDevices.UseCompatibleStateImageBehavior = false;
            this.lvDevices.View = System.Windows.Forms.View.Details;
            this.lvDevices.SelectedIndexChanged += new System.EventHandler(this.LvDevices_SelectedIndexChanged);
            this.lvDevices.Resize += new System.EventHandler(this.LvDevice_Resize);
            // 
            // mnuDevices
            // 
            this.mnuDevices.ImageScalingSize = new System.Drawing.Size(18, 18);
            this.mnuDevices.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuDevicesUnpair});
            this.mnuDevices.Name = "mnuDevices";
            this.mnuDevices.Size = new System.Drawing.Size(185, 26);
            // 
            // mnuDevicesUnpair
            // 
            this.mnuDevicesUnpair.Name = "mnuDevicesUnpair";
            this.mnuDevicesUnpair.Size = new System.Drawing.Size(184, 22);
            this.mnuDevicesUnpair.Text = "mnuDevicesUnpair";
            this.mnuDevicesUnpair.Click += new System.EventHandler(this.MnuDeviceUnpair_Click);
            // 
            // splitter1
            // 
            this.splitter1.Location = new System.Drawing.Point(189, 0);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(3, 339);
            this.splitter1.TabIndex = 2;
            this.splitter1.TabStop = false;
            // 
            // lblConnected
            // 
            this.lblConnected.AutoSize = true;
            this.lblConnected.Location = new System.Drawing.Point(36, 54);
            this.lblConnected.Name = "lblConnected";
            this.lblConnected.Size = new System.Drawing.Size(84, 17);
            this.lblConnected.TabIndex = 3;
            this.lblConnected.Text = "Unconnected";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(478, 339);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.button1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "MainForm";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.mnuDevices.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Button button1;
        private Panel panel1;
        private ListView lvDevices;
        private ColumnHeader columnHeader1;
        private Splitter splitter1;
        private ContextMenuStrip mnuDevices;
        private ToolStripMenuItem mnuDevicesUnpair;
        private Button btnRemoteControl;
        private Label lblConnected;
    }
}