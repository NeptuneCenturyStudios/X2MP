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
    /// Interaction logic for EqualizerUserControl.xaml
    /// </summary>
    public partial class EqualizerUserControl : UserControl
    {
        public EqualizerUserControl()
        {
            InitializeComponent();

            //set view model
            DataContext = new EqualizerViewModel();
        }
    }
}
