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
using X2MP.Models;

namespace X2MP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //initialize our view model
            this.DataContext = new MainWindowViewModel(this);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //move window
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            //stop playback
            var stopTask = App.SoundEngine.StopAsync();

            //hide the window
            this.Hide();

            stopTask.ContinueWith((t) =>
            {
                //shutdown fmod
                App.SoundEngine.Dispose();

                //shutdown app
                //App.Current.Shutdown();
                Dispatcher.Invoke(() => { this.Close(); });

            });

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
