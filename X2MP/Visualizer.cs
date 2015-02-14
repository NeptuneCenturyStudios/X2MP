using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace X2MP
{
    public partial class Visualizer : UserControl
    {
        private System.Drawing.Pen _pen;
        private float[] _data;

        public Visualizer()
        {
            InitializeComponent();

            _pen = new Pen(Brushes.Blue, 1);
        }

        public void SetData(float[] data)
        {
            _data = data;
            this.Invalidate();
        }



        protected override void OnPaint(PaintEventArgs e)
        {

            if (_data == null) return;
            var g = e.Graphics;
            int numPoints = 512;
            int skip = _data.Length / numPoints;

            double lineSpacing = (e.ClipRectangle.Width / (numPoints));
            double startX = 0;
            double startY = (e.ClipRectangle.Height / 2);

            //for now, we will only work with waveform
            for (var x = 0; x < numPoints; x += 1)
            {
                float data = _data[x * skip];


                double endX = startX + lineSpacing;
                double endY = startY + data;


                g.DrawLine(
                    _pen,
                    new System.Drawing.Point((int)startX, (int)startY),
                    new System.Drawing.Point((int)endX, (int)endY)
                    );

                //next start is last end
                startX = endX;
                startY = endY;
            }

            base.OnPaint(e);
        }
    }
}
