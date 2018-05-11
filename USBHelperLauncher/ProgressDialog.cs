using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace USBHelperLauncher
{
    public partial class ProgressDialog : Form
    {
        public ProgressDialog(bool reportProgress)
        {
            InitializeComponent();
            backgroundWorker.WorkerReportsProgress = reportProgress;
            backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.PerformStep();
        }

        public void Reset(int maximum)
        {
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
    }
}
