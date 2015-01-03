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
    }
}
