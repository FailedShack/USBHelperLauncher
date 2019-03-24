using System;
using System.Collections.Generic;
using System.Drawing;
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
            metadata.DrawMode = DrawMode.OwnerDrawVariable;
            metadata.MeasureItem += new MeasureItemEventHandler(metadata_MeasureItem);
            metadata.DrawItem += new DrawItemEventHandler(metadata_DrawItem);
        }

        private async void PackageDetails_Load(object sender, EventArgs e)
        {
            Enabled = false;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            Text = String.Format(Text, package.GetName());
            version.Text = package.GetVersion();
            filename.Text = await package.GetFileName();
            foreach (KeyValuePair<string, string> meta in package.GetMeta())
            {
                metadata.Items.Add(meta.Key + ": " + meta.Value);
            }
            Enabled = true;
        }

        private void metadata_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = (int)e.Graphics.MeasureString(metadata.Items[e.Index].ToString(), metadata.Font, metadata.Width).Height;
        }

        private void metadata_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index == -1)
                return;
            e.DrawBackground();
            e.DrawFocusRectangle();
            e.Graphics.DrawString(metadata.Items[e.Index].ToString(), e.Font, new SolidBrush(e.ForeColor), e.Bounds);
        }
    }
}
