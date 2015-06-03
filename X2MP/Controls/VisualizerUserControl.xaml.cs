using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using X2MP.Core;
using X2MP.Models;

namespace X2MP
{
    /// <summary>
    /// Interaction logic for VisualizerUserControl.xaml
    /// </summary>
    public partial class VisualizerUserControl : UserControl, IPlayerComponent
    {
        public VisualizerUserControl(MainWindowViewModel model)
        {
            InitializeComponent();

            this.DataContext = model;
            
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //get the model
            var model = this.DataContext as MainWindowViewModel;
            //set event for visualization
            model.VisualizationUpdated += model_VisualizationUpdated;
        }


        double dpiX;
        double dpiY;
        //create pen
        System.Windows.Media.Pen pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Blue, 2);
        DrawingVisual dv = new DrawingVisual();
        RenderTargetBitmap bmp = null;

        void model_VisualizationUpdated(object sender, VisualizationUpdatedEventArgs e)
        {
            //Dispatcher.Invoke(() =>
            //{

            var dc = dv.RenderOpen();


            if (dpiX == 0 || dpiY == 0)
            {
                PresentationSource src = PresentationSource.FromVisual(this);
                dpiX = 96 * src.CompositionTarget.TransformToDevice.M11;
                dpiY = 96 * src.CompositionTarget.TransformToDevice.M22;
            }

            var width = vis.ActualWidth;
            var height = vis.ActualHeight;

            int numPoints = 64;
            int skip = e.SampleBuffer.Length / numPoints;

            double lineSpacing = (width / (numPoints)); //256 points
            double startX = 0;
            double startY = (height / 2);

            //dc.Draw

            //for now, we will only work with waveform
            for (var x = 0; x < numPoints; x++)
            {
                float scale = (1 / ((float)(height / 64)));
                float data1 = e.SampleBuffer[x * skip] / scale;
                float data2 = e.SampleBuffer[(x * skip) + 1] / scale;

                var data = (data1 + data2) / 2;//

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


            //get ready to display
            dc.Close();
            //if (bmp == null || bmp.Width != (int)width || bmp.Height != (int)height)
            {

                bmp = new RenderTargetBitmap((int)width, (int)height, dpiX, dpiY, PixelFormats.Pbgra32);
            }

            bmp.Render(dv);
            vimg.Source = bmp;

            //}, System.Windows.Threading.DispatcherPriority.Render);
        }



        bool _isDisposed = false;
        //TODO: make a base class for components that support disposing
        public void Dispose()
        {
            if (!_isDisposed)
            {
                //get the model
                var model = this.DataContext as MainWindowViewModel;
                //stop visual processing
                model.VisualizationUpdated -= model_VisualizationUpdated;
                //null the source
                vimg.Source = null;

                _isDisposed = true;
            }
        }
    }
}
