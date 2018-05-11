namespace USBHelperLauncher.Emulator
{
    partial class PackageProperties
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
            this.basicDetailsBox = new System.Windows.Forms.GroupBox();
            this.filename = new System.Windows.Forms.Label();
            this.version = new System.Windows.Forms.Label();
            this.filenameText = new System.Windows.Forms.Label();
            this.versionText = new System.Windows.Forms.Label();
            this.metadataBox = new System.Windows.Forms.GroupBox();
            this.metadata = new System.Windows.Forms.ListBox();
            this.basicDetailsBox.SuspendLayout();
            this.metadataBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // basicDetailsBox
            // 
            this.basicDetailsBox.Controls.Add(this.filename);
            this.basicDetailsBox.Controls.Add(this.version);
            this.basicDetailsBox.Controls.Add(this.filenameText);
            this.basicDetailsBox.Controls.Add(this.versionText);
            this.basicDetailsBox.Location = new System.Drawing.Point(12, 12);
            this.basicDetailsBox.Name = "basicDetailsBox";
            this.basicDetailsBox.Size = new System.Drawing.Size(300, 90);
            this.basicDetailsBox.TabIndex = 0;
            this.basicDetailsBox.TabStop = false;
            this.basicDetailsBox.Text = "Basic Details";
            // 
            // filename
            // 
            this.filename.AutoSize = true;
            this.filename.Location = new System.Drawing.Point(64, 50);
            this.filename.Name = "filename";
            this.filename.Size = new System.Drawing.Size(46, 13);
            this.filename.TabIndex = 3;
            this.filename.Text = "filename";
            // 
            // version
            // 
            this.version.AutoSize = true;
            this.version.Location = new System.Drawing.Point(64, 26);
            this.version.Name = "version";
            this.version.Size = new System.Drawing.Size(41, 13);
            this.version.TabIndex = 2;
            this.version.Text = "version";
            // 
            // filenameText
            // 
            this.filenameText.AutoSize = true;
            this.filenameText.Location = new System.Drawing.Point(6, 50);
            this.filenameText.Name = "filenameText";
            this.filenameText.Size = new System.Drawing.Size(52, 13);
            this.filenameText.TabIndex = 1;
            this.filenameText.Text = "Filename:";
            // 
            // versionText
            // 
            this.versionText.AutoSize = true;
            this.versionText.Location = new System.Drawing.Point(6, 26);
            this.versionText.Name = "versionText";
            this.versionText.Size = new System.Drawing.Size(45, 13);
            this.versionText.TabIndex = 0;
            this.versionText.Text = "Version:";
            // 
            // metadataBox
            // 
            this.metadataBox.Controls.Add(this.metadata);
            this.metadataBox.Location = new System.Drawing.Point(12, 108);
            this.metadataBox.Name = "metadataBox";
            this.metadataBox.Size = new System.Drawing.Size(300, 90);
            this.metadataBox.TabIndex = 1;
            this.metadataBox.TabStop = false;
            this.metadataBox.Text = "Metadata";
            // 
            // metadata
            // 
            this.metadata.Dock = System.Windows.Forms.DockStyle.Fill;
            this.metadata.FormattingEnabled = true;
            this.metadata.Location = new System.Drawing.Point(3, 16);
            this.metadata.Name = "metadata";
            this.metadata.Size = new System.Drawing.Size(294, 71);
            this.metadata.TabIndex = 0;
            // 
            // PackageProperties
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(324, 211);
            this.Controls.Add(this.basicDetailsBox);
            this.Controls.Add(this.metadataBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "PackageProperties";
            this.Text = "Package Properties: {0}";
            this.Load += new System.EventHandler(this.PackageDetails_Load);
            this.basicDetailsBox.ResumeLayout(false);
            this.basicDetailsBox.PerformLayout();
            this.metadataBox.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox basicDetailsBox;
        private System.Windows.Forms.Label filename;
        private System.Windows.Forms.Label version;
        private System.Windows.Forms.Label filenameText;
        private System.Windows.Forms.Label versionText;
        private System.Windows.Forms.GroupBox metadataBox;
        private System.Windows.Forms.ListBox metadata;
    }
}