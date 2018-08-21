using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace USBHelperLauncher
{
    public partial class CheckboxDialog : Form
    {
        private static readonly Dictionary<int, Icon> icons = new Dictionary<int, Icon>()
        {
            { 16, SystemIcons.Hand },
            { 32, SystemIcons.Question },
            { 48, SystemIcons.Exclamation },
            { 64, SystemIcons.Asterisk }
        };

        private static readonly Dictionary<int, SystemSound> sounds = new Dictionary<int, SystemSound>()
        {
            { 0, SystemSounds.Beep },
            { 16, SystemSounds.Hand },
            { 32, SystemSounds.Question },
            { 48, SystemSounds.Exclamation },
            { 64, SystemSounds.Asterisk }
        };

        private static readonly int[][] btnDists = new int[][]
        {
            new int[] { 0, 0, 1 }, new int[] { 0, 1, 2 }, new int[] { 3, 4, 5 },
            new int[] { 6, 7, 2 }, new int[] { 0, 6, 7 }, new int[] { 0, 4, 2 }
        };

        private SystemSound sound;

        public bool Checked
        {
            get
            {
                return checkBox.Checked;
            }
        }

        public CheckboxDialog(string text, string checkBoxText, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            InitializeComponent();

            var btns = new Button[] { button1, button2, button3 };
            int[] dist = btnDists[(int) buttons];
            for (int i = 0; i < 3; i++)
            {
                var result = (DialogResult)dist[i];
                if (result == DialogResult.None)
                {
                    btns[i].Visible = false;
                    continue;
                }
                btns[i].Text = Enum.GetName(typeof(DialogResult), result);
                btns[i].DialogResult = result;
            }
            if (icon == MessageBoxIcon.None)
            {
                this.icon.Visible = false;
                messageLabel.Location = new Point(messageLabel.Location.X - 32, messageLabel.Location.Y);
                messageLabel.MaximumSize = new Size(messageLabel.MaximumSize.Width + 32, messageLabel.MaximumSize.Height);
                checkBox.Location = new Point(checkBox.Location.X - 32, checkBox.Location.Y);
            }
            else
            {
                this.icon.Image = icons[(int) icon].ToBitmap();
            }
            Text = caption;
            sound = sounds[(int)icon];

            // Message box sizing shenanigans
            var startHeight = messageLabel.Height;
            messageLabel.Text = text;
            checkBox.Text = checkBoxText;
            var widest = Math.Max(messageLabel.Width, checkBox.Width); // Shrink the message box to the widest text
            var diff = new Size(widest - messageLabel.MaximumSize.Width, messageLabel.Height - startHeight); // Width and height adjustment
            messagePanel.Size += diff;
            Size += diff;
        }

        public CheckboxDialog(string text, string checkBoxText, string caption, MessageBoxButtons buttons) : this(text, checkBoxText, caption, buttons, MessageBoxIcon.None) { }

        public CheckboxDialog(string text, string checkBoxText, string caption) : this(text, checkBoxText, caption, MessageBoxButtons.OK, MessageBoxIcon.None) { }

        public CheckboxDialog(string text, string checkBoxText) : this(text, checkBoxText, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.None) { }

        private void CheckboxDialog_Load(object sender, EventArgs e)
        {
            sound.Play();
        }
    }
}
