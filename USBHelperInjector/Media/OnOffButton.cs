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
        public bool Hover { get; private set; }

        public Image OnImage { get; set; }
        public Image OffImage { get; set; }

        protected override void OnCreateControl()
        {
            StateChanged?.Invoke(this, null);
            Image = _state ? OnImage : OffImage;
            base.OnCreateControl();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            State = !State;
            base.OnMouseClick(e);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            Hover = true;
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            Hover = false;
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var rect = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
            ButtonRenderer.DrawParentBackground(g, rect, this);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (Hover) g.FillEllipse(Brushes.White, rect);
            g.DrawImage(Image, rect);
        }
    }
}
