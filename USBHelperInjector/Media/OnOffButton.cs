using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace USBHelperInjector.Media
{
    class OnOffButton : Button
    {
        public event EventHandler StateChanged;

        private Bitmap bitmap;

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
                StateChanged?.Invoke(this, null);
            }
        }
        public bool Hover { get; private set; }

        public Image OnImage { get; set; }
        public Image OffImage { get; set; }

        public static Brush AlmostBlack = new SolidBrush(Color.FromArgb(25, 25, 25));
        public static Brush AlmostWhite = new SolidBrush(Color.FromArgb(230, 230, 230));

        protected override void OnCreateControl()
        {
            Debug.Assert(OnImage.Size == OffImage.Size);
            Size = OnImage.Size;
            StateChanged?.Invoke(this, null);
            Image = _state ? OnImage : OffImage;
            bitmap = new Bitmap(ClientSize.Width, ClientSize.Height);
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
            using (var g = Graphics.FromImage(bitmap))
            {
                var rect = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
                ButtonRenderer.DrawParentBackground(g, rect, this);
                var dark = bitmap.GetPixel(0, 0).GetBrightness() < 0.1;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                if (Hover) g.FillEllipse(dark ? AlmostBlack : AlmostWhite, rect);
                g.DrawImage(dark ? Invert(Image) : Image, rect);
                e.Graphics.DrawImageUnscaled(bitmap, 0, 0);
            }
        }

        protected override void Dispose(bool disposing)
        {
            bitmap.Dispose();
            AlmostBlack.Dispose();
            AlmostWhite.Dispose();
            base.Dispose(disposing);
        }

        private Image Invert(Image image)
        {
            var copy = new Bitmap(image);
            for (var i = 0; i < copy.Height; ++i)
            {
                for (var j = 0; j < copy.Width; ++j)
                {
                    var inv = copy.GetPixel(i, j);
                    inv = Color.FromArgb(inv.A, 255 - inv.R, 255 - inv.G, 255 - inv.B);
                    copy.SetPixel(i, j, inv);
                }
            }
            return copy;
        }
    }
}
