using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AffiliateSim.Services;

namespace AffiliateSim
{
    public partial class TransactionGraph : UserControl
    {
        readonly DateTimeOffset startTime = DateTimeOffset.UtcNow;
        System.Windows.Forms.Timer timer;

        internal Simulator? Simulator { get; set; }

        public TransactionGraph()
        {
            InitializeComponent();

            this.Paint += (s, e) => UpdateGraph(e.Graphics);
            this.Resize += (s, e) => UpdateGraph();
            this.ResizeRedraw = true;

            timer = new System.Windows.Forms.Timer()
            {
                Interval = 200
            };
            timer.Tick += (s, e) => UpdateGraph();
        }

        protected override void OnLoad(EventArgs e)
        {
            timer.Enabled = true;
            UpdateGraph();

            base.OnLoad(e);
        }

        Image? bgImage;
        private void UpdateGraph()
        {
            SyncBGImageSize();
            if (bgImage != null && this.IsHandleCreated)
            {
                using (var g = Graphics.FromImage(bgImage))
                {
                    UpdateGraph(g);
                }

                this.Invoke(() => this.Refresh());
            }
        }

        void SyncBGImageSize()
        {
            var w = this.Width;
            var h = this.Height;
            if (w > 0 && h > 0 && (bgImage?.Width != w || bgImage?.Width != h))
            {
                bgImage?.Dispose();
                bgImage = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            SyncBGImageSize();
            if (bgImage != null)
            {
                e.Graphics.DrawImage(bgImage, 0, 0);
            }

            base.OnPaint(e);
        }

        private void UpdateGraph(Graphics g)
        {
            g.Clear(Color.AliceBlue);

            if (this.Simulator == null) return;

            string makeKey(IEnumerable<AffiliateInstance> affiliateList)
            {
                return string.Join(',', affiliateList
                    .OrderBy(a => a.Descriptor.ID)
                    .Select(a => a.Descriptor.ID.ToString())
                );
            }

            var w = g.VisibleClipBounds.Width;
            var h = g.VisibleClipBounds.Height;
            var affiliateGroups = this.Simulator
                .Affiliates
                .Select(a => a.AllAffiliates.ToArray())
                .GroupBy(a => makeKey(a))
                .Select(g => g.First())
                .ToArray();

            var columnWidth = w / affiliateGroups.Length;
            var rect = new RectangleF(0, 0, columnWidth, h);
            for (int i = 0; i < affiliateGroups.Length; i++)
            {
                rect.X = columnWidth * i;
                RenderAffiliateGroup(g, affiliateGroups[i], rect);
            }
        }

        private void RenderAffiliateGroup(Graphics g, AffiliateInstance[] affiliates, RectangleF rect)
        {
            using var grayPen = new Pen(Brushes.LightGray, 2);
            using var blackPen = new Pen(Brushes.Black, 1);
            using var redPen = new Pen(Brushes.Red, 2);
            using var greenPen = new Pen(Brushes.Green, 2);

            const int MS_PER_PIXEL = 200;
            float getYOffset(DateTimeOffset dt)
            {
                var ts = dt - startTime;
                return (float)(ts.TotalMilliseconds / MS_PER_PIXEL);
            }

            var affiliateRect = new RectangleF(rect.X, rect.Y, rect.Width / affiliates.Length, rect.Height);
            var centerX = affiliateRect.Width / 2;
            for (int i = 0; i < affiliates.Length; i++)
            {
                foreach (var trn in affiliates[i].Ledger.Transactions)
                {
                    var y = getYOffset(trn.TimeStamp);
                    g.FillEllipse(Brushes.Gray, affiliateRect.X + centerX, y, 5, 5);
                }

                foreach (var trn in affiliates[i].FailedTransactions)
                {
                    var y = getYOffset(trn.TimeStamp);
                    g.FillEllipse(Brushes.Red, affiliateRect.X + centerX, y, 5, 5);
                }
                affiliateRect.X += affiliateRect.Width;
            }

            g.DrawLine(grayPen, rect.X + rect.Width, 0, rect.X + rect.Width, rect.Height);
        }
    }
}
