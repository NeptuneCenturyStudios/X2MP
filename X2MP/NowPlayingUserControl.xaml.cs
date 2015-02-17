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
    /// Interaction logic for NowPlayingUserControl.xaml
    /// </summary>
    public partial class NowPlayingUserControl : UserControl
    {
        public NowPlayingUserControl()
        {
            InitializeComponent();

            //view model
            this.DataContext = new NowPlayingViewModel();
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            //get the model
            var model = this.DataContext as NowPlayingViewModel;

            //handle with model
            model.Drop(sender, e);

        }

        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;

        }
    }
}
