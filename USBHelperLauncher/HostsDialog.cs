using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace USBHelperLauncher
{
    public partial class HostsDialog : Form
    {
        private bool PassedValidation { get; set; }
        private bool Modified { get; set; }
        private Hosts Hosts { get; set; }

        private string originalTitle;
        private DataTable dt;

        public HostsDialog()
        {
            InitializeComponent();
            dataGridView.RowValidated += DataGridView_RowValidated;
            dataGridView.CellValidating += DataGridView_CellValidating;
            dataGridView.CellValueChanged += DataGridView_CellValueChanged;
            originalTitle = Text;
            dt = new DataTable();
            dt.Columns.Add("Hostname", typeof(string));
            dt.Columns.Add("IP Address", typeof(string));
            dataGridView.DataSource = dt;
        }

        private void DataGridView_RowValidated(object sender, DataGridViewCellEventArgs e)
        {
            PassedValidation = true;
        }

        private void DataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            Modified = true;
            Text = originalTitle + " (Unsaved)";
        }

        private void DataGridView_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            dataGridView.Rows[e.RowIndex].ErrorText = "";
            string value = (string)e.FormattedValue;
            switch (e.ColumnIndex)
            {
                case 0:
                    if (value == string.Empty || Regex.IsMatch(value, @"\s"))
                    {
                        e.Cancel = true;
                        dataGridView.Rows[e.RowIndex].ErrorText = "Invalid hostname";
                        PassedValidation = false;
                    }
                    else
                    {
                        foreach (DataGridViewRow row in dataGridView.Rows)
                        {
                            if (row.Index != e.RowIndex && string.Equals((string)row.Cells[0].FormattedValue, value, StringComparison.OrdinalIgnoreCase))
                            {
                                e.Cancel = true;
                                dataGridView.Rows[e.RowIndex].ErrorText = "Hostname already defined";
                                PassedValidation = false;
                                break;
                            }
                        }
                    }
                    break;
                case 1:
                    if (!ValidateIPv4(value))
                    {
                        e.Cancel = true;
                        dataGridView.Rows[e.RowIndex].ErrorText = "Invalid IP Address";
                        PassedValidation = false;
                    }
                    break;
            }
        }

        private void openButton_Click(object sender, EventArgs e)
        {
            openFileDialog.ShowDialog();
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog.ShowDialog();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CanSave())
            {
                SaveHosts(Hosts);
                Hosts.Save(Program.GetHostsFile());
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CanSave())
            {
                saveFileDialog.ShowDialog();
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (CanSave())
            {
                SaveHosts(Hosts);
                Hosts.Save(Program.GetHostsFile());
            }
        }

        private void saveFileDialog_FileOk(object sender, CancelEventArgs e)
        {
            Hosts hosts = new Hosts();
            SaveHosts(hosts);
            hosts.Save(saveFileDialog.FileName);
        }

        private void openFileDialog_FileOk(object sender, CancelEventArgs e)
        {
            Hosts hosts = Hosts.Load(openFileDialog.FileName);
            LoadHosts(hosts);
        }

        private void SaveHosts(Hosts hosts)
        {
            hosts.Clear();
            foreach (DataRow row in dt.Rows)
            {
                hosts.Add(row.Field<string>(0), IPAddress.Parse(row.Field<string>(1)));
            }
            Modified = false;
            Text = originalTitle;
        }

        private void LoadHosts(Hosts hosts)
        {
            PassedValidation = false;
            dt.Clear();
            foreach (string host in hosts.GetHosts())
            {
                dt.Rows.Add(host, hosts.Get(host).ToString());
            }
        }

        private bool CanSave()
        {
            if (!PassedValidation)
            {
                MessageBox.Show("You must properly fill in all fields before saving.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        private static bool ValidateIPv4(string ipString)
        {
            if (string.IsNullOrWhiteSpace(ipString))
            {
                return false;
            }

            string[] splitValues = ipString.Split('.');
            if (splitValues.Length != 4)
            {
                return false;
            }

            return splitValues.All(r => byte.TryParse(r, out _));
        }

        private void HostsDialog_Load(object sender, EventArgs e)
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            Hosts = Program.Hosts;
            LoadHosts(Hosts);
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            PassedValidation = false;
            dt.Clear();
        }
    }
}
