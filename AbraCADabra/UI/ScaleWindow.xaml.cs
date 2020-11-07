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
using System.Windows.Shapes;

namespace AbraCADabra
{
    /// <summary>
    /// Interaction logic for ScaleWindow.xaml
    /// </summary>
    public partial class ScaleWindow : Window
    {
        public double ScaleFactor { get; set; } = 1;
        public ScaleWindow()
        {
            InitializeComponent();
        }

        private void ButtonOkClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void ButtonCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
