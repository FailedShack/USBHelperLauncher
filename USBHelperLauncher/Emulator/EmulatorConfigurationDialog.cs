﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using USBHelperLauncher.Utils;

namespace USBHelperLauncher.Emulator
{
    public partial class EmulatorConfigurationDialog : Form
    {
        private EmulatorConfiguration config;
        private Package selectedPackage;
        private readonly ContextMenuStrip contextMenuStrip;
        private Dictionary<string, Package> emulatorPackages = new Dictionary<string, Package>();
        private List<Package> extensionPackages = new List<Package>();
        private ProgressDialog dlDialog;
        private string version;
        private string tempFolder;

        public EmulatorConfigurationDialog(EmulatorConfiguration config)
        {
            this.config = config;
            InitializeComponent();
            var propertiesItem = new ToolStripMenuItem { Text = "Properties" };
            propertiesItem.Click += PropertiesItem_Click;
            contextMenuStrip = new ContextMenuStrip();
            contextMenuStrip.Items.Add(propertiesItem);
            packages.MouseDown += Packages_MouseDown;
            message.Text = String.Format(message.Text, config.GetName());
        }

        private void Packages_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            var index = packages.IndexFromPoint(e.Location);
            if (index != ListBox.NoMatches)
            {
                selectedPackage = (Package) packages.Items[index];
                contextMenuStrip.Show(Cursor.Position);
                contextMenuStrip.Visible = true;
            }
            else
            {
                contextMenuStrip.Visible = false;
            }
        }

        private void PropertiesItem_Click(object sender, EventArgs e)
        {
            new PackageProperties(selectedPackage).Show();
        }

        private void packages_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.CurrentValue == CheckState.Indeterminate)
            {
                e.NewValue = CheckState.Indeterminate;
            }
        }

        private void downloadButton_Click(object sender, EventArgs e)
        {
            if (availableVersions.SelectedIndex == -1)
            {
                MessageBox.Show("You must first select a version.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            dlDialog = new ProgressDialog();
            dlDialog.Show();
            dlDialog.SetHeader("Downloading...");
            Enabled = false;
            Download(dlDialog.GetWorker());
        }

        private async Task FetchPackages()
        {
            foreach (KeyValuePair<string, EmulatorConfiguration.GetPackage> entry in config.GetVersions())
            {
                emulatorPackages.Add(entry.Key, await entry.Value());
            }
            foreach (EmulatorConfiguration.GetPackage getter in config.GetExtensions())
            {
                extensionPackages.Add(await getter());
            }
        }

        private void UpdatePackages()
        {
            packages.Items.Clear();
            if (availableVersions.Items.Count == 0)
            {
                foreach (string version in emulatorPackages.Keys)
                {
                    availableVersions.Items.Add(version);
                }
            }
            else if (availableVersions.SelectedItem != null)
            {
                Package emulatorPackage = emulatorPackages[availableVersions.Text];
                packages.Items.Add(emulatorPackage, CheckState.Indeterminate);
            }
            foreach (Package package in extensionPackages)
            {
                packages.Items.Add(package);
            }
        }

        private async void Download(BackgroundWorker worker)
        {
            tempFolder = Path.GetTempFileName();
            File.Delete(tempFolder);
            Directory.CreateDirectory(tempFolder);
            string buildDir = Path.Combine(tempFolder, "build");
            using (WebClient client = new WebClient())
            {
                client.DownloadProgressChanged += Client_DownloadProgressChanged;
                foreach (Package package in packages.CheckedItems)
                {
                    try
                    {
                        await package.Download(client, tempFolder);
                        DirectoryInfo dir = await package.Unpack();
                        AddToBuild(Path.Combine(buildDir, package.GetInstallPath()), dir);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not download package " + package.GetName() +  ".\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        dlDialog.BeginInvoke(new Action(() => dlDialog.Close()));
                        Enabled = true;
                        return;
                    }
                }
            }
            string build = Path.Combine(tempFolder, "build.zip");
            ZipFile.CreateFromDirectory(buildDir, build);
            string emulatorsDir = "emulators";
            if (!Directory.Exists(emulatorsDir))
            {
                Directory.CreateDirectory(emulatorsDir);
            }
            File.Move(build, Path.Combine(emulatorsDir, config.GetName() + ".zip"));
            dlDialog.BeginInvoke(new Action(() => dlDialog.Close()));
            Close();
        }

        private void AddToBuild(string buildDir, DirectoryInfo dir)
        {
            // Handle archives that don't have their contents in the root
            DirectoryInfo[] dirs = dir.GetDirectories();
            if (dir.GetFiles().Length == 0 && dirs.Length == 1)
            {
                IOUtil.MoveDirectory(dirs[0].FullName, buildDir);
            }
            else
            {
                IOUtil.MoveDirectory(dir.FullName, buildDir);
            }
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            dlDialog.SetProgress(e.ProgressPercentage);
        }

        private void availableVersions_SelectedIndexChanged(object sender, EventArgs e)
        {
            version = availableVersions.Text;
            UpdatePackages();
        }

        private async void EmulatorConfigurationDialog_Load(object sender, EventArgs e)
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            Enabled = false;
            try
            {
                await FetchPackages();
            }
            catch (WebException ex)
            {
                MessageBox.Show("Could not fetch packages.\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Invoke(new Action(() => Close()));
                return;
            }
            Invoke(new Action(() =>
            {
                UpdatePackages();
                Enabled = true;
            }));
        }
    }
}
