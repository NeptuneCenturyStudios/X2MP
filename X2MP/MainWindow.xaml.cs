using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
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
using System.Windows.Interop;

namespace X2MP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("gdi32")]
        static extern int DeleteObject(IntPtr o);

        public static BitmapSource LoadBitmap(System.Drawing.Bitmap source)
        {
            IntPtr ip = source.GetHbitmap();
            BitmapSource bs = null;
            try
            {
                bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip,
                   IntPtr.Zero, Int32Rect.Empty,
                   System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(ip);
            }

            return bs;
        }

        public MainWindow()
        {
            InitializeComponent();
            //initialize our view model
            var model = new MainWindowViewModel(this);
            this.DataContext = model;

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

            
            var dc = dv.RenderOpen();


            if (dpiX == 0 || dpiY == 0)
            {
                PresentationSource src = PresentationSource.FromVisual(this);
                dpiX = 96 * src.CompositionTarget.TransformToDevice.M11;
                dpiY = 96 * src.CompositionTarget.TransformToDevice.M22;
            }

            var width = vis.ActualWidth;
            var height = vis.ActualHeight;

            int numPoints = 128;
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


        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //move window
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            //stop playback
            App.SoundEngine.Stop();
            this.Close();
            //hide the window
            //this.Hide();

            //stopTask.ContinueWith((t) =>
            //{
            //    //shutdown fmod
            //    App.SoundEngine.Dispose();

            //    //shutdown app
            //    //App.Current.Shutdown();
            //    Dispatcher.Invoke(() => { });

            //});

        }

        private void Slider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            //get the slider and value
            var slider = sender as Slider;
            var value = slider.Value;

            //clear the binding so that we do not update the slider while we are dragging it
            BindingOperations.ClearBinding(sender as Slider, Slider.ValueProperty);

            //reset slider value because removing the binding causes the value to become 0
            slider.Value = value;
        }

        private void Slider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            //get the slider and value
            var slider = sender as Slider;
            var value = slider.Value;

            //recreate the binding
            var binding = new Binding()
            {
                Path = new PropertyPath("Position"),
                Mode = BindingMode.TwoWay,
                NotifyOnTargetUpdated = true,
                NotifyOnSourceUpdated = true
            };

            //set the binding
            BindingOperations.SetBinding(slider, Slider.ValueProperty, binding);

            //set slider value
            slider.SetValue(Slider.ValueProperty, value);
        }



    }
}
