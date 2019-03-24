using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace USBHelperLauncher
{
    public partial class ProgressDialog : Form
    {
        public ProgressDialog()
        {
            InitializeComponent();
        }

        public void Reset(int maximum)
        {
            progressBar.Step = 1;
            progressBar.Value = 0;
            progressBar.Maximum = maximum;
        }

        public BackgroundWorker GetWorker()
        {
            return backgroundWorker;
        }

        public ProgressBar GetProgressBar()
        {
            return progressBar;
        }

        public void SetHeader(string text)
        {
            header.Text = text;
        }

        public void SetProgress(int progress)
        {
            progressBar.Value = progress;
        }

        public void SetStyle(ProgressBarStyle style)
        {
            progressBar.Style = style;
        }

        private void ProgressDialog_Load(object sender, EventArgs e)
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
    }
}
