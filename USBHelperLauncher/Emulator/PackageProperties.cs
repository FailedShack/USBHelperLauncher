using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace USBHelperLauncher.Emulator
{
    public partial class PackageProperties : Form
    {
        private Package package;

        public PackageProperties(Package package)
        {
            this.package = package;
            InitializeComponent();
        }

        private void PackageDetails_Load(object sender, EventArgs e)
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            Text = String.Format(Text, package.GetName());
            version.Text = package.GetVersion();
            filename.Text = Path.GetFileName(package.GetURI().LocalPath);
            foreach (KeyValuePair<string, string> meta in package.GetMeta())
            {
                metadata.Items.Add(meta.Key + ": " + meta.Value);
            }
        }
    }
}
