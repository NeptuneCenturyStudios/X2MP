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
    /// Interaction logic for CollectionUserControl.xaml
    /// </summary>
    public partial class CollectionUserControl : UserControl
    {
        public CollectionUserControl()
        {
            InitializeComponent();
            var model = new CollectionViewModel();
            foreach(var entry in App.SoundEngine.NowPlaying){
                model.Collection.Add(entry);
            }
            this.DataContext = model;
        }

        protected void Grid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            //get the playlist item
            var entry = ((ListViewItem)sender).Content as PlayListEntry;

            //play the song
            App.SoundEngine.PlayOrPause(entry);
        }
    }
}
