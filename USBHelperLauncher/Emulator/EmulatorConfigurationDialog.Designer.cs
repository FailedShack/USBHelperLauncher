namespace USBHelperLauncher.Emulator
{
    partial class EmulatorConfigurationDialog
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
            this.message = new System.Windows.Forms.Label();
            this.packages = new System.Windows.Forms.CheckedListBox();
            this.downloadButton = new System.Windows.Forms.Button();
            this.availableVersions = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // message
            // 
            this.message.AutoSize = true;
            this.message.Location = new System.Drawing.Point(12, 9);
            this.message.Name = "message";
            this.message.Size = new System.Drawing.Size(229, 13);
            this.message.TabIndex = 0;
            this.message.Text = "Select the options you would like to use for {0}.";
            // 
            // packages
            // 
            this.packages.FormattingEnabled = true;
            this.packages.Location = new System.Drawing.Point(15, 60);
            this.packages.Name = "packages";
            this.packages.Size = new System.Drawing.Size(294, 109);
            this.packages.TabIndex = 1;
            this.packages.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.packages_ItemCheck);
            // 
            // downloadButton
            // 
            this.downloadButton.Location = new System.Drawing.Point(197, 175);
            this.downloadButton.Name = "downloadButton";
            this.downloadButton.Size = new System.Drawing.Size(112, 30);
            this.downloadButton.TabIndex = 2;
            this.downloadButton.Text = "Download";
            this.downloadButton.UseVisualStyleBackColor = true;
            this.downloadButton.Click += new System.EventHandler(this.downloadButton_Click);
            // 
            // availableVersions
            // 
            this.availableVersions.FormattingEnabled = true;
            this.availableVersions.Location = new System.Drawing.Point(60, 33);
            this.availableVersions.Name = "availableVersions";
            this.availableVersions.Size = new System.Drawing.Size(100, 21);
            this.availableVersions.TabIndex = 3;
            this.availableVersions.SelectedIndexChanged += new System.EventHandler(this.availableVersions_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 36);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(42, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Version";
            // 
            // EmulatorConfigurationDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(324, 211);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.availableVersions);
            this.Controls.Add(this.downloadButton);
            this.Controls.Add(this.packages);
            this.Controls.Add(this.message);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "EmulatorConfigurationDialog";
            this.Text = "Emulator Configuration";
            this.Load += new System.EventHandler(this.EmulatorConfigurationDialog_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label message;
        private System.Windows.Forms.CheckedListBox packages;
        private System.Windows.Forms.Button downloadButton;
        private System.Windows.Forms.ComboBox availableVersions;
        private System.Windows.Forms.Label label2;
    }
}