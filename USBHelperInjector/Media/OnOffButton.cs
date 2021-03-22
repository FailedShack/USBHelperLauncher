using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace USBHelperInjector.Media
{
    class OnOffButton : Button
    {
        public event EventHandler StateChanged;

        private bool _state;
        public bool State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
                Image = value ? OnImage : OffImage;
                Size = Image.Size;
                StateChanged?.Invoke(this, null);
            }
        }

        public Image OnImage { get; set; }
        public Image OffImage { get; set; }

        protected override void OnCreateControl()
        {
            StateChanged?.Invoke(this, null);
            Image = _state ? OnImage : OffImage;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            base.OnCreateControl();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            State = !State;
            base.OnMouseClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var path = new GraphicsPath();
            path.AddEllipse(0, 0, ClientSize.Width, ClientSize.Height);
            Region = new Region(path);
            base.OnPaint(e);
        }
    }
}
