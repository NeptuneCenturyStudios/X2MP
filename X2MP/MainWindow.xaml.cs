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
using X2MP.Models;

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

        void model_VisualizationUpdated(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => {
                //if (this.Background != null)
                //{
                //    var img = this.Background as ImageBrush;
                    
                //}
                this.Background = new ImageBrush(LoadBitmap((Bitmap)(sender as MainWindowViewModel).Visualization));
            });
            
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
