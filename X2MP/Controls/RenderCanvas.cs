using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace X2MP.Controls
{
    class RenderCanvas : Canvas
    {

        private System.Windows.Media.Pen _pen;
        private float[] _data;


        public RenderCanvas()
        {
            _pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Blue, 1);
        }

        protected override void OnRender(System.Windows.Media.DrawingContext dc)
        {
            if (_data == null) return;

            int numPoints = 512;
            int skip = _data.Length / numPoints;

            double lineSpacing = (this.ActualWidth / (numPoints)); //256 points
            double startX = 0;
            double startY = (this.ActualHeight / 2);

            var pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Blue, 1);

            //for now, we will only work with waveform
            for (var x = 0; x < numPoints; x += 1)
            {
                float data = _data[x * skip];


                double endX = startX + lineSpacing;
                double endY = startY + data;


                dc.DrawLine(
                    pen,
                    new System.Windows.Point(startX, startY),
                    new System.Windows.Point(endX, endY)
                    );
                
                //next start is last end
                startX = endX;
                startY = endY;
            }

            base.OnRender(dc);
        }

        public void SetData(float[] data)
        {
            _data = data;
        }
    }
}
